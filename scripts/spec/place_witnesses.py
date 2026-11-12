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
    DRAFTED              a later pass wrote the witness itself (the triage of the drafts that did not match corrected a
                         suspect draft; the drafting pass wrote one for a rule the audit never drafted): the record names
                         its `pass` and carries the `draft` and its `redraft_run`, placed when that is MATCH

This script does the placement from the records, never by hand:

    python scripts/spec/place_witnesses.py --confirm DIR --state DIR --batch FILE [--summary FILE] [--cobol PATH]
                                           [--edition 1985] [--jobs 8] [--replace]
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
A draft's library texts (its `files`, audit_batch.py `draft_problem`) lie beside its test and its header declares them
(`files`, design §3.1), as the harness staged them when it judged the draft (kb/Work PB5067). A library text in a
library folder (`<library>/<file>`) is no suite test's: where a library-name's text is found is the implementor's, so
the generator admits the folder only in a WiseOwl test, and such a draft is skipped with that reason. A library text
whose name the folder already holds with other text (another test's) refuses the run before it writes anything. A
refusal the compiler reports with no error at the violating line is skipped too (a warning alone refuses nothing).

A rule is placed once, whichever pass's records place it. A run that places a rule whose test the suite already holds
leaves it alone when the case it would write is the one there, byte for byte (its program and the sidecars of its shape,
and no other); when it is not, the run is refused before it writes anything, naming the source the placed test was placed
from and the records file that places it again. `--replace` replaces it instead: every file of the old case (its program
in whatever clause folder, its `.out` files, its `wiseowl/diagnostics/` refusal) is removed before the new one is written.

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
import generate  # noqa: E402  (the suite's ONE reader of a test's header: the files a placed test declares)
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
#: A witness a later pass wrote and ran itself; its record names that pass.
DRAFTED = "DRAFTED"
#: A fixed-form comment line of a placed test: the indicator `*>` in columns 7-8, the text inside margin R (column 72).
COMMENT, MARGIN = "      *> ", 72


@dataclass(frozen=True)
class Witness:
    rule_id: str
    draft: dict[str, Any]
    #: what the draft is: the audit's own (CONFIRMED) or the confirmation pass's redraft
    source: str
    #: the records file whose record gives it
    origin: pathlib.Path


@dataclass
class Sorted:
    witnesses: list[Witness]
    #: rule id -> the question an UNDECIDED record asks
    undecided: dict[str, str]
    #: rule id -> why a record gives no witness (a MISMATCH redraft: a filed defect; a CONFIRMED rule with no draft)
    unplaced: dict[str, str]


def read_confirmations(directory: pathlib.Path) -> list[tuple[pathlib.Path, dict[str, Any]]]:
    """Every record of every `*.json` under `directory` (`{"records": [...]}`) with the file that holds it, in file order;
    a rule twice is refused."""
    records: list[tuple[pathlib.Path, dict[str, Any]]] = []
    for f in sorted(directory.glob("*.json")):
        records += [(f, r) for r in json.loads(f.read_text(encoding="utf-8"))["records"]]
    twice = [r for r, n in collections.Counter(r["rule-id"] for _, r in records).items() if n > 1]
    if twice:
        raise SystemExit(f"{TOOL}: {directory} decides {', '.join(sorted(twice)[:5])} more than once")
    return records


def sort_records(records: list[tuple[pathlib.Path, dict[str, Any]]], drafts: dict[str, dict[str, Any]]) -> Sorted:
    """The witnesses the records give, the UNDECIDED records' questions, and why each other record gives none."""
    out = Sorted([], {}, {})
    for origin, r in records:
        rid, decision = r["rule-id"], r["decision"]
        if decision == CONFIRMED:
            if rid in drafts:
                out.witnesses.append(Witness(rid, drafts[rid], "the audit's draft, confirmed", origin))
            else:
                out.unplaced[rid] = "CONFIRMED, but the audit run holds no draft of it"
        elif decision in REDRAFTED and r.get("redraft_run") == PLACEABLE_REDRAFT:
            out.witnesses.append(Witness(rid, r["draft"], f"the confirmation pass's redraft ({decision} original)",
                                         origin))
        elif decision == DRAFTED and r.get("redraft_run") == PLACEABLE_REDRAFT:
            out.witnesses.append(Witness(rid, r["draft"], f"written and run by {r['pass']}", origin))
        elif decision == UNDECIDED:
            out.undecided[rid] = r.get("question") or ""
        else:
            out.unplaced[rid] = f"{decision}, redraft {r.get('redraft_run')}: a filed defect, never a witness"
    return out


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
    files = sorted(w.draft.get("files", {}))
    header = [f"id: {test_id(w.rule_id)}", f"rules: [{w.rule_id}]", *([f"accept: [{spelling}]"] if accepted else []),
              *([f"files: [{', '.join(files)}]"] if files else []), "source-format: fixed", "portable: unverified"]
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
    #: the placed rules whose case the suite already held, byte for byte (nothing written)
    unchanged: list[str]


def declared(program: pathlib.Path) -> list[pathlib.Path]:
    """The files a placed test's header declares (`files`, read by the generator's own header reader), beside it."""
    header, _ = generate.read_header(program.read_text(encoding="utf-8"))
    return [program.parent / f for f in ((header.fields.get("files") or []) if header else [])]


def case_files(suite: pathlib.Path, tid: str) -> list[pathlib.Path]:
    """Every file of `suite` that is test `tid`'s case: its program in whatever clause folder, the expected-output files
    beside it (`<id>.out`, `<id>.<...>.out`: generate.py's names), the library texts it declares, and its refusal
    (`wiseowl/diagnostics/<id>.json`, `<id>.<step file>.json`)."""
    programs = sorted((suite / "cases").glob(f"*/{tid}.cob"))
    outs = [o for p in programs for o in sorted(p.parent.glob(f"{tid}.*out")) if o.name.endswith(".out")]
    libraries = [f for p in programs for f in declared(p) if f.is_file()]
    diagnostics = suite / "wiseowl" / "diagnostics"
    refusals = [f for f in (diagnostics / f"{tid}.json", *sorted(diagnostics.glob(f"{tid}.*.json"))) if f.is_file()]
    return programs + outs + libraries + refusals


def declared_by_others(program: pathlib.Path) -> set[pathlib.Path]:
    """The files another test of `program`'s folder declares (a library text two tests share is neither one's to
    remove)."""
    return {f for p in sorted(program.parent.glob("*.cob")) if p != program for f in declared(p)}


def placed_from(program: pathlib.Path) -> str:
    """The source a placed test names in its comment (`Placed from <source> by scripts/spec/place_witnesses.py`)."""
    text = " ".join(line[len(COMMENT):] for line in program.read_text(encoding="utf-8").splitlines()
                    if line.startswith(COMMENT))
    m = re.search(r"Placed from (.+?) by scripts/spec/place_witnesses\.py", text)
    return m.group(1) if m else "a source this script does not name (it did not place it)"


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
          judged: dict[str, tuple[str, str | None]], suite: pathlib.Path, replace: bool = False) -> Placement:
    """Write each witness that holds on this build into `suite`; the rest are skipped with their reason. A rule whose
    test the suite already holds is left alone when its case is the one this would write, byte for byte; otherwise the
    whole run is refused before anything is written, unless `replace`, when the old case's files are removed first."""
    out = Placement({}, {}, [])
    #: rule id -> the files of its case (path -> text)
    cases: dict[str, dict[pathlib.Path, str]] = {}
    #: rule id -> its library texts (path -> text), a part of its case
    library_texts: dict[str, dict[pathlib.Path, str]] = {}
    for w in witnesses:
        row = rows.get(w.rule_id)
        verdict, code = judged.get(w.rule_id, ("not judged", None))
        refused = w.draft["expected"]["outcome"] != "output"
        if row is None:
            out.skipped[w.rule_id] = f"no rule of the {ed.year} catalog"
        elif any("/" in name for name in w.draft.get("files", {})):
            out.skipped[w.rule_id] = ("its library texts lie in a library folder, which only a WiseOwl test may declare "
                                      "(where a library-name's text is found is the implementor's, design §3.1)")
        elif verdict != run_drafts.MATCH:
            out.skipped[w.rule_id] = verdict
        elif refused and code is None:
            out.skipped[w.rule_id] = (f"the compiler reports no error at its violating line "
                                      f"{w.draft['expected']['line']} (a warning refuses nothing)")
        else:
            clause = clause_of(row["section"])
            folder, tid = f"{clause}@{ed.year}-{names[clause]}", test_id(w.rule_id)
            where = suite / "cases" / folder
            case = {where / f"{tid}.cob": render(w, row, ed.spelling, code)}
            if refused:
                case[suite / "wiseowl" / "diagnostics" / f"{tid}.json"] = json.dumps(
                    {"refuse-at": [ed.spelling], "expect": code}) + "\n"
            else:
                case[where / f"{tid}.out"] = "\n".join(w.draft["expected"]["output"]) + "\n"
            libraries = {where / name: text for name, text in w.draft.get("files", {}).items()}
            case.update(libraries)
            cases[w.rule_id] = case
            library_texts[w.rule_id] = libraries
            out.placed[w.rule_id] = (folder, tid, code)
    by_rule = {w.rule_id: w for w in witnesses}
    held = {rid: case_files(suite, out.placed[rid][1]) for rid in cases}
    for rid, case in cases.items():
        if {f: f.read_bytes() for f in held[rid]} == {f: text.encode("utf-8") for f, text in case.items()}:
            out.unchanged.append(rid)
    # A library text is one file of its folder, named by the program's COPY: two cases of this run, or a case and another
    # test the folder already holds, that need one name with different text cannot both be placed.
    clashes, needed = [], {}
    for rid, libraries in library_texts.items():
        for f, text in libraries.items():
            first, wanted = needed.setdefault(f, (rid, text))
            if wanted != text:
                clashes.append(f"  {f.relative_to(suite).as_posix()}: {first} and {rid} need it with different text")
            elif f.is_file() and f not in held[rid] and f.read_bytes() != text.encode("utf-8"):
                clashes.append(f"  {f.relative_to(suite).as_posix()}: {rid} needs it, and the folder holds another test's "
                               f"text of that name")
    if clashes:
        raise SystemExit(f"{TOOL}: {len(clashes)} library text(s) clash (nothing was written):\n" + "\n".join(clashes))
    differing = [rid for rid in cases if held[rid] and rid not in out.unchanged]
    if differing and not replace:
        lines = [f"  {rid}: {held[rid][0].relative_to(suite).as_posix()} was placed from {placed_from(held[rid][0])}; "
                 f"{by_rule[rid].origin} places {by_rule[rid].source}, a different case" for rid in sorted(differing)]
        raise SystemExit(f"{TOOL}: {len(differing)} rule(s) already placed differently (--replace replaces them; nothing "
                         f"was written):\n" + "\n".join(lines))
    for rid, case in cases.items():
        if rid in out.unchanged:
            continue
        shared = {f for p in held[rid] if p.suffix == ".cob" for f in declared_by_others(p)}
        for f in held[rid]:
            if f not in shared:
                f.unlink()
        for f, text in case.items():
            f.parent.mkdir(parents=True, exist_ok=True)
            f.write_text(text, encoding="utf-8", newline="\n")
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
            "unchanged": len(placement.unchanged),
            "recorded": len(placement.placed) - len(chained) - len(unclassified), "chained": chained,
            "unclassified": unclassified,
            "skipped": dict(sorted(placement.skipped.items())), "unplaced": dict(sorted(s.unplaced.items())),
            "undecided": dict(sorted(s.undecided.items()))}


def run_placement(confirm: pathlib.Path, state: pathlib.Path, year: int, adapter: pathlib.Path, variables: dict[str, str],
                  jobs: int, suite: pathlib.Path, rows: list[dict[str, Any]] | None = None,
                  schema: inventory_schema.Schema | None = None, replace: bool = False
                  ) -> tuple[dict[str, Any], dict[str, Any]]:
    schema = schema or inventory_schema.load_schema()
    ed = schema.edition(year)
    rows = rows if rows is not None else inventory_view.inventory(year, schema)
    by_id = {r["rule-id"]: r for r in rows}
    s = sort_records(read_confirmations(confirm), answers_by_id(state, "drafts"))
    judged = judge_all(s.witnesses, adapter, variables, ed.spelling, jobs)
    placement = place(s.witnesses, by_id, clause_names(rows), ed, judged, suite, replace)
    batch, chained, unclassified = batch_of(placement, by_id, schema.next, f"place-witnesses-{year}")
    return batch, summary_of(s, placement, chained, unclassified)


# -- self-test ------------------------------------------------------------------------------------------------------------

def self_test() -> int:
    """Three planted records through the harness's stub compiler (run.py `_STUB`): a CONFIRMED output draft of a CHANGED
    rule, a WEAK record's MATCH redraft refusing at its line (a KEPT rule), and an UNDECIDED record; then a redraft that
    COPYs the library text it carries (placed with it), one whose text lies in a library folder (skipped), a refusal the
    compiler only warns about, a MISMATCH redraft, a MATCH record that mismatches on this build and a DRAFTED record its
    pass ran to MISMATCH; then the same records again (nothing written), a draft needing a placed library text's name with
    other text (refused), another pass's records that place two of the rules in the other shape (refused) and the same
    with `--replace`."""
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
        drafted, stale, drafted_filed = "1985-GR-VI.6.18.4-7", "1985-GR-VI.6.18.4-8", "1985-GR-VI.6.18.4-9"
        in_library, clashing = "1985-GR-VI.6.18.4-10", "1985-GR-VI.6.18.4-11"
        library = "       01  W PIC X.\n"
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
            # a redraft that COPYs a library text it carries (kb/Work PB5067): the stub's `need` fails the compile without
            # it, so it matches only when the judge stages it; and one whose text lies in a library folder
            {"rule-id": copying, "decision": "WRONG", "basis": "b", "redraft_run": "MATCH",
             "draft": {"program": "       COPY LIB1.\n*> STUB need LIB1.cpy\n*> STUB run out X",
                       "expected": {"outcome": "output", "output": ["X"]}, "files": {"LIB1.cpy": library}}},
            {"rule-id": in_library, "decision": "WRONG", "basis": "b", "redraft_run": "MATCH",
             "draft": {"program": "       COPY LIB2 OF LIBA.\n*> STUB need LIBA/LIB2.cpy\n*> STUB run out X",
                       "expected": {"outcome": "output", "output": ["X"]}, "files": {"LIBA/LIB2.cpy": library}}},
            {"rule-id": warned, "decision": "WEAK", "basis": "b", "redraft_run": "MATCH",
             "draft": {"program": warning, "expected": {"outcome": "refused", "line": 2, "control": "*> STUB run out ok"}}},
            {"rule-id": filed, "decision": "WEAK", "basis": "b", "redraft_run": "MISMATCH",
             "draft": {"program": "*> STUB run out Y", "expected": {"outcome": "output", "output": ["Z"]}}},
            {"rule-id": drafted, "decision": "DRAFTED", "pass": "the drafting pass", "redraft_run": "MATCH",
             "draft": {"program": "*> STUB run out W", "expected": {"outcome": "output", "output": ["W"]}}},
            # its record says MATCH, but on this build the program DISPLAYs another line: the re-judge skips it
            {"rule-id": stale, "decision": "WEAK", "basis": "b", "redraft_run": "MATCH",
             "draft": {"program": "*> STUB run out Y", "expected": {"outcome": "output", "output": ["Q"]}}},
            # a DRAFTED witness its pass ran to MISMATCH is a filed defect, even though this build would match it
            {"rule-id": drafted_filed, "decision": "DRAFTED", "pass": "the drafting pass", "redraft_run": "MISMATCH",
             "draft": {"program": "*> STUB run out V", "expected": {"outcome": "output", "output": ["V"]}}}]}),
            encoding="utf-8", newline="\n")

        def row(rid: str, kind: str | None) -> dict[str, Any]:
            section = rid.split("-")[2]
            return {"rule-id": rid, "section": section, "subject": "THE INSPECT STATEMENT",
                    "citation": f"§1985:{section} {rid.split('-')[1]}{rid.split('-')[3]}",
                    "next": {"kind": kind} if kind else {}}

        rows = [row(changed, "CHANGED"), row(kept, "KEPT"), row(undecided, "CHANGED"), row(copying, "REMOVED"),
                row(warned, "CHANGED"), row(filed, "CHANGED"), row(drafted, "REMOVED"), row(stale, "CHANGED"),
                row(drafted_filed, "CHANGED"), row(in_library, "CHANGED"), row(clashing, "CHANGED")]
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
        ok(sorted(batch["records"], key=lambda r: r["rule-id"])
           == [{"rule-id": rid, "test-ref": f"conformance:cases/{folder.name}/{test_id(rid)}"}
               for rid in sorted((changed, copying, drafted))]
           and summary["chained"] == [kept] and summary["unclassified"] == [],
           "the CHANGED and REMOVED rules' witnesses are witness-only records; the KEPT rule's is its chain's, placed and "
           "not recorded")
        copied = folder / f"{test_id(copying)}.cob"
        header, _ = generate.read_header(copied.read_text(encoding="utf-8")) if copied.is_file() else (None, [])
        ok(header is not None and header.fields.get("files") == ["LIB1.cpy"]
           and (folder / "LIB1.cpy").read_text(encoding="utf-8") == library
           and (folder / f"{test_id(copying)}.out").read_text(encoding="utf-8") == "X\n",
           "a draft that COPYs is placed with its library text beside it, its header declaring it (kb/Work PB5067)")
        ok("library folder" in summary["skipped"].get(in_library, "") and not (folder / "LIBA").exists(),
           "a draft whose library text lies in a library folder is skipped: only a WiseOwl test may declare one")
        ok((folder / f"{test_id(drafted)}.out").read_text(encoding="utf-8") == "W\n"
           and "written and run by the drafting pass" in (folder / f"{test_id(drafted)}.cob").read_text(encoding="utf-8"),
           "a DRAFTED record's MATCH draft is placed and recorded, its test naming the pass that wrote it")
        ok(summary["undecided"] == {undecided: "Is it testable?"} and filed in summary["unplaced"],
           "an UNDECIDED record is listed with its question; a MISMATCH redraft is a filed defect, never placed")
        ok("no error" in summary["skipped"].get(warned, "") and summary["placed"] == 4,
           "a refusal the compiler only warns about is skipped")
        ok("on this build" in summary["skipped"].get(stale, "") and not (folder / f"{test_id(stale)}.cob").exists()
           and stale not in summary["unplaced"],
           "a redraft whose record says MATCH but which no longer matches on this build is judged again and not placed")
        ok("MISMATCH" in summary["unplaced"].get(drafted_filed, "") and not (folder / f"{test_id(drafted_filed)}.cob").exists(),
           "a DRAFTED record whose pass ran it to MISMATCH is a filed defect, never placed, though this build matches it")

        def snapshot() -> dict[str, bytes]:
            return {f.relative_to(suite).as_posix(): f.read_bytes() for f in sorted(suite.rglob("*")) if f.is_file()}

        before = snapshot()
        _, again = run_placement(confirm, state, 1985, root / "adapters" / "stub.json", {}, 2, suite, rows, schema)
        ok(again["placed"] == 4 and again["unchanged"] == 4 and snapshot() == before,
           "placing the same records again finds every case already there, byte for byte, and writes nothing")
        # another rule's draft needs the placed test's library text's name with other text: the folder has one file of
        # that name, so the run is refused before it writes
        confirm3 = root / "confirm-3"
        confirm3.mkdir()
        (confirm3 / "clash.json").write_text(json.dumps({"records": [
            {"rule-id": clashing, "decision": "DRAFTED", "pass": "the triage pass", "redraft_run": "MATCH",
             "draft": {"program": "       COPY LIB1.\n*> STUB need LIB1.cpy\n*> STUB run out Y",
                       "expected": {"outcome": "output", "output": ["Y"]}, "files": {"LIB1.cpy": "       01  V PIC 9.\n"}}}]}),
            encoding="utf-8", newline="\n")
        try:
            run_placement(confirm3, state, 1985, root / "adapters" / "stub.json", {}, 2, suite, rows, schema)
            ok(False, "a library text another test of the folder holds with other text refuses the run")
        except SystemExit as e:
            ok("LIB1.cpy" in str(e) and clashing in str(e) and "another test" in str(e) and snapshot() == before,
               "a library text another test of the folder holds with other text refuses the run, which writes nothing")
        # the same name with the same text is one file two tests share; a --replace of one of them that no longer
        # needs it keeps it for the other, and the replace of the last one removes it
        (confirm3 / "clash.json").write_text(json.dumps({"records": [
            {"rule-id": clashing, "decision": "DRAFTED", "pass": "the triage pass", "redraft_run": "MATCH",
             "draft": {"program": "       COPY LIB1.\n*> STUB need LIB1.cpy\n*> STUB run out Y",
                       "expected": {"outcome": "output", "output": ["Y"]}, "files": {"LIB1.cpy": library}}}]}),
            encoding="utf-8", newline="\n")
        shared = run_placement(confirm3, state, 1985, root / "adapters" / "stub.json", {}, 2, suite, rows, schema)[1]
        plain = {"program": "*> STUB run out X", "expected": {"outcome": "output", "output": ["X"]}}
        for rid, name in ((copying, "first"), (clashing, "second")):
            replace_dir = root / f"replace-{name}"
            replace_dir.mkdir()
            (replace_dir / "r.json").write_text(json.dumps({"records": [
                {"rule-id": rid, "decision": "DRAFTED", "pass": "the triage pass", "redraft_run": "MATCH",
                 "draft": {**plain, "expected": {"outcome": "output", "output": ["X" if rid == copying else "Y"]},
                           "program": "*> STUB run out " + ("X" if rid == copying else "Y")}}]}),
                encoding="utf-8", newline="\n")
            run_placement(replace_dir, state, 1985, root / "adapters" / "stub.json", {}, 2, suite, rows, schema,
                          replace=True)
            if rid == copying:
                kept_shared = (folder / "LIB1.cpy").is_file()
        ok(shared["placed"] == 1 and shared["unchanged"] == 0 and kept_shared and not (folder / "LIB1.cpy").exists(),
           "a library text two tests share is placed once; a --replace keeps it while another test declares it and "
           "removes it with the last")
        before = snapshot()
        # another pass's records place two placed rules again in the other shape: the output test as a refusal, the
        # refusal as an output test
        confirm2 = root / "confirm-2"
        confirm2.mkdir()
        (confirm2 / "triage.json").write_text(json.dumps({"records": [
            {"rule-id": changed, "decision": "DRAFTED", "pass": "the triage pass", "redraft_run": "MATCH", "draft": refusal},
            {"rule-id": kept, "decision": "DRAFTED", "pass": "the triage pass", "redraft_run": "MATCH",
             "draft": {"program": "*> STUB run out K", "expected": {"outcome": "output", "output": ["K"]}}}]}),
            encoding="utf-8", newline="\n")
        try:
            run_placement(confirm2, state, 1985, root / "adapters" / "stub.json", {}, 2, suite, rows, schema)
            ok(False, "a rule placed again from another pass in a different case is refused, naming both sources")
        except SystemExit as e:
            ok(f"{changed}:" in str(e) and f"{kept}:" in str(e) and "the audit's draft, confirmed" in str(e)
               and "the confirmation pass's redraft (WEAK original)" in str(e) and str(confirm2 / "triage.json") in str(e)
               and "written and run by the triage pass" in str(e) and snapshot() == before,
               "a rule placed again from another pass in a different case is refused, naming both sources, and the "
               "run writes nothing")
        (folder / f"{test_id(changed)}.85.out").write_text("HELLO\n", encoding="utf-8", newline="\n")
        _, replaced = run_placement(confirm2, state, 1985, root / "adapters" / "stub.json", {}, 2, suite, rows, schema,
                                    replace=True)
        diagnostics = suite / "wiseowl" / "diagnostics"
        ok(replaced["placed"] == 2 and replaced["unchanged"] == 0
           and "written and run by the triage pass" in out_test.read_text(encoding="utf-8")
           and (diagnostics / f"{test_id(changed)}.json").is_file()
           and not list(folder.glob(f"{test_id(changed)}.*out"))
           and (folder / f"{test_id(kept)}.out").read_text(encoding="utf-8") == "K\n"
           and not (diagnostics / f"{test_id(kept)}.json").exists(),
           "--replace writes the new case and removes every file of the old shape (the .out files, the refusal)")
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
    ap.add_argument("--replace", action="store_true",
                    help="replace a rule's placed test that differs from the case this would write (its old files removed)")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    if a.confirm is None or a.state is None or a.batch is None:
        ap.error("--confirm, --state and --batch are required")
    if not a.cobol.is_file():
        raise SystemExit(f"{TOOL}: no compiler at {a.cobol} (dotnet build WiseOwl.Cobol.sln)")
    batch, summary = run_placement(a.confirm, a.state, a.edition, run_drafts.ADAPTER, {"cobol": str(a.cobol)}, a.jobs,
                                   SUITE, replace=a.replace)
    write_atomic(a.batch, json.dumps(batch, indent=1, ensure_ascii=False))
    if a.summary:
        write_atomic(a.summary, json.dumps(summary, indent=1, ensure_ascii=False))
    print(f"{TOOL}: placed {summary['placed']} {summary['placed-by-kind']} ({summary['unchanged']} already there), "
          f"{summary['recorded']} recorded in {a.batch}, "
          f"{len(summary['chained'])} their chain's, skipped {len(summary['skipped'])}, undecided "
          f"{len(summary['undecided'])}, unplaced {len(summary['unplaced'])}", flush=True)
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
