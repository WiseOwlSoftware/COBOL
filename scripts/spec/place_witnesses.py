#!/usr/bin/env python3
"""place_witnesses.py - THE placement of an older edition's checked drafts in the conformance suite (kb/Work PB4995 item 3;
DESIGN-spec-conformance-review.md §10.2, DESIGN-conformance-suite.md §3.5).

The edition audit's drafts pass wrote, for each 1985 rule of its own, a program and the outcome the rule requires
(`audit_batch.py`, the `drafts` pass); `run_drafts.py` ran them; a confirmation pass then checked each MATCH against the
rule's text and wrote one record per rule (`{"records": [...]}` files under a directory):

    CONFIRMED            the original draft witnesses the rule: it is placed (read from the audit run's drafts answers)
    WEAK | WRONG         the draft does not; the record carries its own `draft` and `redraft_run`: a MATCH redraft is
                         placed, a MISMATCH one is a filed defect and is not
    UNDECIDED            the record asks a `question` the texts leave open: no witness, listed for the register

This script does the placement from the records, never by hand:

    python scripts/spec/place_witnesses.py --confirm DIR --state DIR --batch FILE [--summary FILE] [--cobol PATH]
                                           [--edition 1985] [--jobs 8]
    python scripts/spec/place_witnesses.py --self-test

Each witness is first judged again on THIS build through the harness (`run_drafts.harness_run`, `run_drafts.judge`: the
one judge of a draft); one that no longer matches is not placed. A placed witness is ONE test of the rule, in the folder
of the rule's own clause of its own edition (`cases/<clause>@<year>-<Name>/`, the generator's CLAUSE_FOLDER), its id the
rule id (`.` spelled `_`), `portable: unverified` until a clause pass clears its portability (design §6: a draft is a
model's, checked against its rule, never yet against the portability lint or the profiles' `requires`):
- an OUTPUT draft is a test accepted at the edition, its `.out` the draft's DISPLAYed lines;
- a REFUSED draft is a test WiseOwl must refuse, `wiseowl/diagnostics/<id>.json` naming the edition and the code the
  compiler reports AT the draft's violating line (read with the adapter's own diagnostic pattern). Its control proved the
  refusal the rule's when it was judged; the control's expected output is no draft's, so the flagged form (design §3.2)
  waits for the clause pass that writes it.
A draft that needs a COPY library is skipped (its library text is no draft's: kb/Work PB5067), and so is a refusal the
compiler reports with no error at the violating line (a warning alone refuses nothing).

The witness of a rule whose change record is its own (`next` not `chained`: CHANGED, REMOVED) is recorded as a
WITNESS-ONLY record (`{"rule-id", "test-ref"}`) in the batch this writes, for `record_verdicts.py --edition <year>`; its
verdict (CONFORMS needs the code read, `code-location`) is the adjudicator's, never this script's. A KEPT rule's verdict
and test are its chain's (design section 10.2: its store row holds none), and a rule with no change record yet has no adjudication a
witness can join, so both are placed and listed, never recorded.
"""
from __future__ import annotations

import argparse
import collections
import json
import pathlib
import re
import shutil
import sys
import tempfile
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass
from typing import Any

REPO = pathlib.Path(__file__).resolve().parents[2]
SUITE = REPO / "conformance-suite"
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.insert(0, str(SUITE / "harness"))
import cobol_source  # noqa: E402  (the harness's program-text facts: its COPY statements)
import inventory_schema  # noqa: E402
import inventory_view  # noqa: E402  (THE view of an edition's rules, kb/Work PB4992)
import run as harness  # noqa: E402
import run_drafts  # noqa: E402  (THE judge of a draft: its tests, its harness run, its verdict)
from audit_batch import answers_by_id  # noqa: E402
from message_batches import write_atomic  # noqa: E402
sys.path.append(str(REPO / "scripts"))
from standard_streams import ensure_utf8  # noqa: E402

TOOL = "place_witnesses"
#: The confirmation pass's decisions, and the redraft outcome that makes a WEAK or WRONG record a witness.
CONFIRMED, UNDECIDED, REDRAFTED, PLACEABLE_REDRAFT = "CONFIRMED", "UNDECIDED", ("WEAK", "WRONG"), run_drafts.MATCH
#: A fixed-form comment line of a placed test: the indicator `*>` in columns 7-8, the text inside margin R (column 72).
COMMENT, MARGIN = "      *> ", 72


@dataclass(frozen=True)
class Witness:
    rule_id: str
    draft: dict[str, Any]
    #: what the draft is: the audit's own (CONFIRMED) or the confirmation pass's redraft
    source: str


@dataclass
class Sorted:
    witnesses: list[Witness]
    #: rule id -> the question an UNDECIDED record asks
    undecided: dict[str, str]
    #: rule id -> why a record gives no witness (a MISMATCH redraft: a filed defect; a CONFIRMED rule with no draft)
    unplaced: dict[str, str]


def read_confirmations(directory: pathlib.Path) -> list[dict[str, Any]]:
    """Every record of every `*.json` under `directory` (`{"records": [...]}`), in file order; a rule twice is refused."""
    records: list[dict[str, Any]] = []
    for f in sorted(directory.glob("*.json")):
        records += json.loads(f.read_text(encoding="utf-8"))["records"]
    twice = [r for r, n in collections.Counter(r["rule-id"] for r in records).items() if n > 1]
    if twice:
        raise SystemExit(f"{TOOL}: {directory} decides {', '.join(sorted(twice)[:5])} more than once")
    return records


def sort_records(records: list[dict[str, Any]], drafts: dict[str, dict[str, Any]]) -> Sorted:
    """The witnesses the records give, the UNDECIDED records' questions, and why each other record gives none."""
    out = Sorted([], {}, {})
    for r in records:
        rid, decision = r["rule-id"], r["decision"]
        if decision == CONFIRMED:
            if rid in drafts:
                out.witnesses.append(Witness(rid, drafts[rid], "the audit's draft, confirmed"))
            else:
                out.unplaced[rid] = "CONFIRMED, but the audit run holds no draft of it"
        elif decision in REDRAFTED and r.get("redraft_run") == PLACEABLE_REDRAFT:
            out.witnesses.append(Witness(rid, r["draft"], f"the confirmation pass's redraft ({decision} original)"))
        elif decision == UNDECIDED:
            out.undecided[rid] = r.get("question") or ""
        else:
            out.unplaced[rid] = f"{decision}, redraft {r.get('redraft_run')}: a filed defect, never a witness"
    return out


def needs_library(program: str) -> bool:
    """Does the draft COPY a library text (which no draft carries, kb/Work PB5067)?"""
    return bool(cobol_source.copy_statements(cobol_source.tokenize(cobol_source.read_lines(program, "fixed"))))


def clause_of(section: str) -> str:
    """The clause that holds a rule's block: its section without the block's own subclause (`VI.6.18.4` -> `VI.6.18`)."""
    parts = section.split(".")
    return ".".join(parts[:-1]) if len(parts) > 1 else section


def clause_names(rows: list[dict[str, Any]]) -> dict[str, str]:
    """Each clause's folder name: the subject most of its rules carry (the first in the catalog's order on a tie), in
    the folder's letters (`THE INSPECT STATEMENT` -> `INSPECT-STATEMENT`)."""
    subjects: dict[str, collections.Counter[str]] = collections.defaultdict(collections.Counter)
    for row in rows:
        subjects[clause_of(row["section"])][row.get("subject") or ""] += 1
    names = {}
    for clause, counts in subjects.items():
        words = re.findall(r"[A-Za-z0-9]+", counts.most_common(1)[0][0])
        if words and words[0].upper() == "THE":
            words = words[1:]
        names[clause] = "-".join(words[:6]) or "Rules"
    return names


def test_id(rule_id: str) -> str:
    return rule_id.replace(".", "_")


def wrapped(text: str) -> list[str]:
    """`text` as fixed-form comment lines inside margin R."""
    lines, line = [], ""
    for word in text.split():
        if line and len(COMMENT) + len(line) + 1 + len(word) > MARGIN:
            lines.append(COMMENT + line)
            line = word
        else:
            line = f"{line} {word}" if line else word
    return lines + ([COMMENT + line] if line else [])


def render(w: Witness, row: dict[str, Any], spelling: str, code: str | None) -> str:
    """The test's text: its metadata block (README, "A test"), the comment naming the rule and why each leg can fail, then
    the draft's program."""
    accepted = w.draft["expected"]["outcome"] == "output"
    header = [f"id: {test_id(w.rule_id)}", f"rules: [{w.rule_id}]", *([f"accept: [{spelling}]"] if accepted else []),
              "source-format: fixed", "portable: unverified"]
    if accepted:
        why = (f"An output test: the program must translate and run at {spelling} and DISPLAY exactly the lines its .out "
               f"holds, the outcome the rule requires.")
    else:
        why = (f"A refusal: the rule forbids line {w.draft['expected']['line']} of the program below, and WiseOwl refuses "
               f"it at {spelling} with {code} there; a compiler that accepts it fails.")
    comment = wrapped(f"{row['citation']} ({row.get('subject') or row['section']}). {why} Placed from {w.source} by "
                      f"scripts/spec/place_witnesses.py (kb/Work PB4995).")
    lines = ["      *>--- conformance-case", *(COMMENT + h for h in header), "      *>---", *comment]
    return "\n".join(lines + [w.draft["program"].rstrip("\n")]) + "\n"


def refusal_code(run: harness.Run, edition: str, test: dict[str, Any]) -> str | None:
    """The code of the ERROR the compiler reports at the refused draft's violating line, read with the adapter's own
    diagnostic pattern (its `severity` and `code` groups), or None when it reports none there."""
    pattern = run.adapter.diagnostic
    if not {"severity", "code"} <= set(pattern.groupindex):
        raise SystemExit(f"{TOOL}: the {run.adapter.compiler} adapter's diagnostic pattern has no `severity` and `code` "
                         f"groups, so a refusal's code cannot be read")
    work = run.work / (test["id"] + "-code")
    try:
        source, program = run.stage(test, work)
        step = run.operation("compile", edition, test, work, source, program)
        for line in step.text().splitlines():
            m = pattern.search(line)
            if m and m.group("severity") == "error" and int(m.group("line")) == test["line"]:
                return m.group("code")
        return None
    finally:
        shutil.rmtree(work, ignore_errors=True)


@dataclass
class Placement:
    #: rule id -> (folder, test id, code or None)
    placed: dict[str, tuple[str, str, str | None]]
    #: rule id -> why it is not placed
    skipped: dict[str, str]


def judge_all(witnesses: list[Witness], adapter: pathlib.Path, variables: dict[str, str], edition: str,
              jobs: int) -> dict[str, tuple[str, str | None]]:
    """Each witness judged on this build: rule id -> (its verdict or the reason it fails, the refusal's code)."""
    with tempfile.TemporaryDirectory(prefix="witnesses-") as scratch:
        root = pathlib.Path(scratch)
        tests = {w.rule_id: run_drafts.tests_of(root, w.rule_id, w.draft) for w in witnesses}
        run = run_drafts.harness_run(root, adapter, variables, edition, tests)

        def one(rid: str) -> tuple[str, str | None]:
            test = tests[rid][0]
            got = run_drafts.judge(run, edition, test)
            if got["verdict"] != run_drafts.MATCH:
                return f"{got['verdict']} on this build: {got['reason']}", None
            return run_drafts.MATCH, refusal_code(run, edition, test) if test["expect"] == "flag" else None

        with ThreadPoolExecutor(max_workers=max(1, jobs)) as pool:
            return dict(zip(tests, pool.map(one, tests)))


def place(witnesses: list[Witness], rows: dict[str, dict[str, Any]], names: dict[str, str], ed: inventory_schema.Edition,
          judged: dict[str, tuple[str, str | None]], suite: pathlib.Path) -> Placement:
    """Write each witness that holds on this build into `suite`; the rest are skipped with their reason."""
    out = Placement({}, {})
    for w in witnesses:
        row = rows.get(w.rule_id)
        verdict, code = judged.get(w.rule_id, ("not judged", None))
        refused = w.draft["expected"]["outcome"] != "output"
        if row is None:
            out.skipped[w.rule_id] = f"no rule of the {ed.year} catalog"
        elif needs_library(w.draft["program"]):
            out.skipped[w.rule_id] = "it COPYs a library text no draft carries (kb/Work PB5067)"
        elif verdict != run_drafts.MATCH:
            out.skipped[w.rule_id] = verdict
        elif refused and code is None:
            out.skipped[w.rule_id] = (f"the compiler reports no error at its violating line "
                                      f"{w.draft['expected']['line']} (a warning refuses nothing)")
        else:
            clause = clause_of(row["section"])
            folder, tid = f"{clause}@{ed.year}-{names[clause]}", test_id(w.rule_id)
            where = suite / "cases" / folder
            where.mkdir(parents=True, exist_ok=True)
            (where / f"{tid}.cob").write_text(render(w, row, ed.spelling, code), encoding="utf-8", newline="\n")
            if refused:
                diagnostics = suite / "wiseowl" / "diagnostics"
                diagnostics.mkdir(parents=True, exist_ok=True)
                (diagnostics / f"{tid}.json").write_text(json.dumps({"refuse-at": [ed.spelling], "expect": code}) + "\n",
                                                         encoding="utf-8", newline="\n")
            else:
                (where / f"{tid}.out").write_text("\n".join(w.draft["expected"]["output"]) + "\n", encoding="utf-8",
                                                  newline="\n")
            out.placed[w.rule_id] = (folder, tid, code)
    return out


def batch_of(placement: Placement, rows: dict[str, dict[str, Any]], change: inventory_schema.ChangeRecord,
             label: str) -> tuple[dict[str, Any], list[str], list[str]]:
    """The witness-only records of every placed rule whose verdict is its own (its change record is not `chained`); the
    placed rules whose verdict is their chain's, which no record of their own edition may carry; and the placed rules
    with no change record yet, whose witness waits for the record that classifies them."""
    records, chained, unclassified = [], [], []
    for rid, (folder, tid, _) in sorted(placement.placed.items()):
        kind = change.kinds.get((rows[rid].get("next") or {}).get("kind", ""))
        if kind is None:
            unclassified.append(rid)
        elif kind.chained:
            chained.append(rid)
        else:
            records.append({"rule-id": rid, "test-ref": f"conformance:cases/{folder}/{tid}"})
    return {"batch": label, "records": records}, chained, unclassified


def summary_of(s: Sorted, placement: Placement, chained: list[str], unclassified: list[str]) -> dict[str, Any]:
    kinds = collections.Counter("refusal" if code else "output" for _, _, code in placement.placed.values())
    return {"placed": len(placement.placed), "placed-by-kind": dict(sorted(kinds.items())),
            "recorded": len(placement.placed) - len(chained) - len(unclassified), "chained": chained,
            "unclassified": unclassified,
            "skipped": dict(sorted(placement.skipped.items())), "unplaced": dict(sorted(s.unplaced.items())),
            "undecided": dict(sorted(s.undecided.items()))}


def run_placement(confirm: pathlib.Path, state: pathlib.Path, year: int, adapter: pathlib.Path, variables: dict[str, str],
                  jobs: int, suite: pathlib.Path, rows: list[dict[str, Any]] | None = None,
                  schema: inventory_schema.Schema | None = None) -> tuple[dict[str, Any], dict[str, Any]]:
    schema = schema or inventory_schema.load_schema()
    ed = schema.edition(year)
    rows = rows if rows is not None else inventory_view.inventory(year, schema)
    by_id = {r["rule-id"]: r for r in rows}
    s = sort_records(read_confirmations(confirm), answers_by_id(state, "drafts"))
    judged = judge_all(s.witnesses, adapter, variables, ed.spelling, jobs)
    placement = place(s.witnesses, by_id, clause_names(rows), ed, judged, suite)
    batch, chained, unclassified = batch_of(placement, by_id, schema.next, f"place-witnesses-{year}")
    return batch, summary_of(s, placement, chained, unclassified)


# -- self-test ------------------------------------------------------------------------------------------------------------

def self_test() -> int:
    """Three planted records through the harness's stub compiler (run.py `_STUB`): a CONFIRMED output draft of a CHANGED
    rule, a WEAK record's MATCH redraft refusing at its line (a KEPT rule), and an UNDECIDED record; then a redraft that
    COPYs, a refusal the compiler only warns about, and a MISMATCH redraft."""
    from audit_batch import Pass, custom_id, write_plan  # noqa: PLC0415
    fails: list[str] = []

    def ok(cond: bool, what: str) -> None:
        print(("  ok    " if cond else "  FAIL  ") + what)
        if not cond:
            fails.append(what)

    schema = inventory_schema.load_schema()
    with tempfile.TemporaryDirectory() as tmp:
        root = pathlib.Path(tmp)
        stub = root / "stubcc.py"
        stub.write_text(harness._STUB, encoding="utf-8", newline="\n")
        (root / "adapters").mkdir()
        (root / "profiles").mkdir()
        adapter = harness._stub_adapter(
            "stub", stub, diagnostic=r"^(?P<file>[^:\s]+):(?P<line>[0-9]+): (?P<severity>error|warning): (?P<code>\S+)")
        (root / "adapters" / "stub.json").write_text(json.dumps(adapter), encoding="utf-8", newline="\n")
        (root / "profiles" / "stub.json").write_text(json.dumps({"compiler": "stub", "editions": ["85"], "modules": {},
                                                                 "elements": {}, "implementor-defined": {}}),
                                                     encoding="utf-8", newline="\n")
        state, confirm, suite = root / "run", root / "confirm", root / "suite"
        changed, kept, undecided = "1985-GR-VI.6.18.4-1", "1985-SR-VI.6.18.3-2", "1985-GR-VI.6.18.4-3"
        copying, warned, filed = "1985-GR-VI.6.18.4-4", "1985-SR-VI.6.18.3-5", "1985-GR-VI.6.18.4-6"
        refusing = "\n".join(["*> STUB diag 2", "*> STUB refuse"])
        warning = "\n".join(["*> STUB diag 2", "*> the violating line"])
        p = Pass(state, "drafts")
        write_plan(p, [{"id": changed}])
        p.answers_dir.mkdir(parents=True)
        p.answer_file(custom_id("drafts", changed)).write_text(json.dumps(
            {"id": changed, "answer": {"program": "*> STUB run out HELLO", "basis": "planted",
                                       "expected": {"outcome": "output", "output": ["HELLO"]}}}),
            encoding="utf-8", newline="\n")
        refusal = {"program": refusing, "expected": {"outcome": "refused", "line": 2, "control": "*> STUB run out ok"}}
        confirm.mkdir()
        (confirm / "slice-01.json").write_text(json.dumps({"records": [
            {"rule-id": changed, "decision": "CONFIRMED", "basis": "b"},
            {"rule-id": kept, "decision": "WEAK", "basis": "b", "draft": refusal, "redraft_run": "MATCH"},
            {"rule-id": undecided, "decision": "UNDECIDED", "basis": "b", "question": "Is it testable?"}]}),
            encoding="utf-8", newline="\n")
        (confirm / "slice-02.json").write_text(json.dumps({"records": [
            {"rule-id": copying, "decision": "WRONG", "basis": "b", "redraft_run": "MATCH",
             "draft": {"program": "       COPY LIB1.\n*> STUB run out X", "expected": {"outcome": "output", "output": ["X"]}}},
            {"rule-id": warned, "decision": "WEAK", "basis": "b", "redraft_run": "MATCH",
             "draft": {"program": warning, "expected": {"outcome": "refused", "line": 2, "control": "*> STUB run out ok"}}},
            {"rule-id": filed, "decision": "WEAK", "basis": "b", "redraft_run": "MISMATCH",
             "draft": {"program": "*> STUB run out Y", "expected": {"outcome": "output", "output": ["Z"]}}}]}),
            encoding="utf-8", newline="\n")

        def row(rid: str, kind: str | None) -> dict[str, Any]:
            section = rid.split("-")[2]
            return {"rule-id": rid, "section": section, "subject": "THE INSPECT STATEMENT",
                    "citation": f"§1985:{section} {rid.split('-')[1]}{rid.split('-')[3]}",
                    "next": {"kind": kind} if kind else {}}

        rows = [row(changed, "CHANGED"), row(kept, "KEPT"), row(undecided, "CHANGED"), row(copying, "REMOVED"),
                row(warned, "CHANGED"), row(filed, "CHANGED")]
        batch, summary = run_placement(confirm, state, 1985, root / "adapters" / "stub.json", {}, 2, suite, rows, schema)
        folder = suite / "cases" / "VI.6.18@1985-INSPECT-STATEMENT"
        out_test = folder / f"{test_id(changed)}.cob"
        ok(out_test.is_file() and (folder / f"{test_id(changed)}.out").read_text(encoding="utf-8") == "HELLO\n",
           "a CONFIRMED output draft is a test in its rule's own clause of 1985, its .out the draft's lines")
        text = out_test.read_text(encoding="utf-8") if out_test.is_file() else ""
        ok(f"rules: [{changed}]" in text and "accept: [85]" in text and "portable: unverified" in text
           and text.rstrip().endswith("*> STUB run out HELLO") and all(len(x) <= MARGIN for x in text.splitlines()[:12]),
           "its metadata names the rule, accepts it at 85 alone, unverified, and the comment stays inside margin R")
        refused_test = folder / f"{test_id(kept)}.cob"
        diag = suite / "wiseowl" / "diagnostics" / f"{test_id(kept)}.json"
        ok(refused_test.is_file() and "accept:" not in refused_test.read_text(encoding="utf-8")
           and json.loads(diag.read_text(encoding="utf-8")) == {"refuse-at": ["85"], "expect": "refused"},
           "a MATCH redraft refusal is a refused test with the code the compiler reports at its violating line")
        ok(batch["records"] == [{"rule-id": changed, "test-ref": f"conformance:cases/{folder.name}/{test_id(changed)}"}]
           and summary["chained"] == [kept] and summary["unclassified"] == [],
           "the CHANGED rule's witness is a witness-only record; the KEPT rule's is its chain's, placed and not recorded")
        ok(summary["undecided"] == {undecided: "Is it testable?"} and filed in summary["unplaced"],
           "an UNDECIDED record is listed with its question; a MISMATCH redraft is a filed defect, never placed")
        ok("PB5067" in summary["skipped"].get(copying, "") and "no error" in summary["skipped"].get(warned, "")
           and summary["placed"] == 2,
           "a draft that COPYs is skipped, and so is a refusal the compiler only warns about")
        dup = confirm / "slice-03.json"
        dup.write_text(json.dumps({"records": [{"rule-id": changed, "decision": "CONFIRMED", "basis": "b"}]}),
                       encoding="utf-8", newline="\n")
        try:
            read_confirmations(confirm)
            ok(False, "a rule decided twice is refused")
        except SystemExit as e:
            ok("more than once" in str(e), "a rule decided twice is refused")
    ok(clause_of("VI.6.18.4") == "VI.6.18" and clause_names([{"section": "II.4.2", "subject": "TABLE HANDLING"}])
       == {"II.4": "TABLE-HANDLING"}, "a rule's clause is its block's parent; its name the subject's words")
    print(f"=== PLACE_WITNESSES SELF-TEST: {'PASS' if not fails else 'FAIL - ' + str(len(fails))} ===")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="Place an older edition's checked drafts in the conformance suite.")
    ap.add_argument("--self-test", action="store_true")
    ap.add_argument("--confirm", type=pathlib.Path, help="the confirmation pass's records (a directory of *.json)")
    ap.add_argument("--state", type=pathlib.Path, help="the audit run's directory (its drafts pass)")
    ap.add_argument("--batch", type=pathlib.Path, help="the record_verdicts batch to write (witness-only records)")
    ap.add_argument("--summary", type=pathlib.Path, help="where to write what was placed, skipped and left undecided")
    ap.add_argument("--cobol", type=pathlib.Path, default=run_drafts.DEFAULT_COBOL)
    ap.add_argument("--edition", type=int, default=1985, help="the held edition whose rules the drafts witness")
    ap.add_argument("--jobs", type=int, default=8)
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if a.confirm is None or a.state is None or a.batch is None:
        ap.error("--confirm, --state and --batch are required")
    if not a.cobol.is_file():
        raise SystemExit(f"{TOOL}: no compiler at {a.cobol} (dotnet build WiseOwl.Cobol.sln)")
    batch, summary = run_placement(a.confirm, a.state, a.edition, run_drafts.ADAPTER, {"cobol": str(a.cobol)}, a.jobs,
                                   SUITE)
    write_atomic(a.batch, json.dumps(batch, indent=1, ensure_ascii=False))
    if a.summary:
        write_atomic(a.summary, json.dumps(summary, indent=1, ensure_ascii=False))
    print(f"{TOOL}: placed {summary['placed']} {summary['placed-by-kind']}, {summary['recorded']} recorded in {a.batch}, "
          f"{len(summary['chained'])} their chain's, skipped {len(summary['skipped'])}, undecided "
          f"{len(summary['undecided'])}, unplaced {len(summary['unplaced'])}", flush=True)
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
