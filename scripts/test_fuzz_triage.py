#!/usr/bin/env python3
"""test_fuzz_triage.py — the self-test of scripts/fuzz_triage.py (kb/Work PB3972; docs/rearchitecture/DESIGN-differential-fuzz.md).

    python scripts/test_fuzz_triage.py          # no build, no git, no network; needs specs/ISO_COBOL.md

It drives the tool on a FABRICATED run directory (a results.jsonl, a report.md and a run.json in the shapes
scripts/differential_fuzz.py writes) and proves, arm by arm:
  RESCORE   an output difference that vanishes under the harness's normalization reads AGREE, an exit-only difference is
            EXIT_DIFFERS, the first four differing lines are kept;
  SLICE     the four slice kinds, the refusal-selection rule (3 rows or more, or a diagnostic code no larger signature
            carries), the rows report.md does not list (synthesized), and ALL of a signature's programs stated;
  BRIEFS    every template renders with no `<<NAME>>` left, an unknown placeholder is an error, no brief names a
            night-specific path, run id or date, the vocabulary / dispositions / bar sections in common.md equal the
            tool's, and the Workflow script names the briefs by the tool's own file names;
  VERDICTS  each rule of `verdict_problems` fires on its defect and stays silent on a valid line; a family claimed twice
            and an undecided signature are reported;
  REFUTE    the GNUCOBOL-SIDE records are sliced for the refuters and a refuted record is not sliced twice;
  FILING    a record is held when unrefuted, OVERTURNED, missing its corrected copy, its program, a checked citation, its
            Draft issue title or its cobc command, or when the register's own bar (`work.upstream_problems`) refuses the
            note; a RECLASSIFY applies the refuter's class; a family an existing note records, and a `witness` verdict,
            add the run's ids to that note instead of a second note; a refusal writes NOTHING; a second `file` is a no-op;
            the ids come from the allocator's answer, parsed.
"""
from __future__ import annotations

import contextlib
import io
import json
import pathlib
import re
import subprocess
import sys
import unittest.mock

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE / "spec"))
import fuzz_triage as ft  # noqa: E402
from scratch_repo import remove_tree, scratch_dir  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402

REPO = HERE.parent
FAILURES: list[str] = []
NL = "\n"


def check(name: str, ok: bool, detail: str = "") -> None:
    print(("PASS  " if ok else "FAIL  ") + name + ("" if ok else f"  {detail}"))
    if not ok:
        FAILURES.append(name)


def raises(fn, exc=Exception) -> bool:
    try:
        fn()
    except exc:
        return True
    return False


# ── the fabricated run ───────────────────────────────────────────────────────────────────────────────────────────────

def row(rid, verdict, clause="13.18.60", we_out="", they_out="", we_rc=0, they_rc=0, signature=None, **we):
    return {"id": rid, "clause": clause, "title": f"title of {clause}", "verdict": verdict, "signature": signature,
            "we": {"output": we_out, "run_rc": we_rc, "runner_error": "", **we}, "they": {"output": they_out, "run_rc": they_rc}}


ROWS = [
    row("fz00001", "OUTPUT_DIFFERS", "13.18.60", "A\nB", "A\nC", signature="OUTPUT_DIFFERS:13.18.60"),
    row("fz00002", "OUTPUT_DIFFERS", "8.5", "A  \nB\0", "A\nB ", signature="OUTPUT_DIFFERS:8.5"),
    row("fz00003", "OUTPUT_DIFFERS", "8.5", "X", "Y", signature="OUTPUT_DIFFERS:8.5"),
    row("fz00004", "EXIT_DIFFERS", "14.9", "same", "same", 1, 2, signature="EXIT_DIFFERS:14.9"),
    row("fz00005", "WE_REJECT", "15.78", signature="WE_REJECT:COBOL0898"),
    row("fz00006", "WE_REJECT", "15.78", signature="WE_REJECT:COBOL0898"),
    row("fz00007", "WE_REJECT", "15.78", signature="WE_REJECT:COBOL0898"),
    row("fz00008", "WE_REJECT", "15.79", signature="WE_REJECT:COBOL0898,COBOL0001"),
    row("fz00009", "WE_REJECT", "15.80", signature="WE_REJECT:COBOL0898-b"),
    row("fz00010", "THEY_REJECT", "15.66", signature="THEY_REJECT:15.66"),
    row("fz00011", "BOTH_HANG", "14.9"),
    row("fz00012", "RUNNER_ERROR", "14.9"),
    row("fz00013", "WE_HANG", "14.9", signature="WE_HANG:14.9"),
]
REPORT = """# Differential fuzz — triage report

## Survivors: 13 programs in 7 signatures

### `WE_REJECT:COBOL0898` × 3 — first: fz00005 (clause 15.78 REVERSE function)
- programs: fz00005, fz00006, fz00007
- source: `programs/fz00005.cob`
- WiseOwl: `fz00005.cob(6,1): error COBOL0898: x`

### `WE_REJECT:COBOL0898,COBOL0001` × 1 — first: fz00008 (clause 15.79 y)
- programs: fz00008
- source: `programs/fz00008.cob`

### `WE_REJECT:COBOL0898-b` × 1 — first: fz00009 (clause 15.80 z)
- programs: fz00009
- source: `programs/fz00009.cob`

### `THEY_REJECT:15.66` × 1 — first: fz00010 (clause 15.66 NATIONAL-OF function)
- programs: fz00010
- source: `programs/fz00010.cob`

### `WE_HANG:14.9` × 1 — first: fz00013 (clause 14.9 w)
- programs: fz00013
- source: `programs/fz00013.cob`

### `EXIT_DIFFERS:14.9` × 1 — first: fz00004 (clause 14.9 e)
- programs: fz00004
- source: `programs/fz00004.cob`

## Runner errors

- fz00012: the runner lost it
"""
RUN_JSON = {"model": "test-model", "cobol_exe": "judge/cobol.exe", "edition": "2014"}

PROGRAM = "IDENTIFICATION DIVISION.\nPROGRAM-ID. DEMO.\nPROCEDURE DIVISION.\n    STOP RUN.\n"
LONG = NL.join(f"       DISPLAY {n}." for n in range(70))
CITE = {"clause": "14.9.4.4", "text": "the EC-PROGRAM-RECURSIVE-CALL exception condition is set to exist"}


CITE2 = {"clause": "14.9.4.3", "text": "Literal-1 shall be an alphanumeric or national literal and shall not be a zero-length literal"}


def record_md(program=PROGRAM, title="**Title:** cobc 3.2 misbehaves on DEMO", env="`cobc -std=cobol2014 -x demo.cob`",
              expected="§14.9.4.4 GR 3) f): the exception is set to exist.") -> str:
    fence = "```"
    return NL.join([
        "# demo-family", "", "## Summary", "", "cobc 3.2 does something wrong.", "",
        "## Minimal test case", "", "Free reference format.", "", f"{fence}cobol", program.rstrip(), fence, "",
        "## Environment", "", env, "",
        "## Observed result", "", "```", "unknown (signal)", "```", "exit code 139", "",
        "## Expected result (per ISO)", "", expected, "",
        "## Our compiler", "", "Compiles; exit 0.", "",
        "## Fix site", "", "`cobc/typeck.c`, function `cb_check_conformance`.", "",
        "## Upstream state", "", "Unreported; tracker not yet searched.", "",
        "## Draft issue", "", title, "", "Steps and the program.", ""])


def verdict(sig="GNUCOBOL-SIDE:demo", family="demo-family", disposition="record", **over):
    up = {"disposition": disposition, "family": family, "class": "crash", "edition": "2014", "version": "3.2.0",
          "citations": [CITE], "record": f"records/{family}.md", "repro": f"records/{family}.cob",
          "draft_title": "cobc 3.2 misbehaves on DEMO", "programs": ["fz00020", "fz00021"]}
    up.update(over)
    return {"signature": sig, "count": 2, "verdict": "GNUCOBOL-SIDE", "harm": None, "rule": "§14.9.4.4 (checked)",
            "note": "n", "repro": up["repro"], "code_site": None, "upstream": up}


def put(path: pathlib.Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline=NL)


def jsonl(items) -> str:
    return "".join(json.dumps(i) + NL for i in items)


def make_run(root: pathlib.Path) -> pathlib.Path:
    run = root / "fuzz-20990101-000000"
    put(run / "results.jsonl", jsonl(ROWS))
    put(run / "report.md", REPORT)
    put(run / "run.json", json.dumps(RUN_JSON))
    return run


# ── arms ─────────────────────────────────────────────────────────────────────────────────────────────────────────────

def arm_rescore() -> None:
    resid = {x["id"]: x for x in ft.rescore(ROWS)}
    check("RESCORE: only the three rescored classes are kept", set(resid) == {"fz00001", "fz00002", "fz00003", "fz00004"})
    check("RESCORE: trailing blanks and NUL bytes do not make a difference", resid["fz00002"]["now"] == "AGREE")
    check("RESCORE: a real difference stays and keeps its first differing line",
          resid["fz00001"]["now"] == "OUTPUT_DIFFERS" and resid["fz00001"]["diffs"] == [(2, "B", "C")])
    check("RESCORE: the same output with another exit code is EXIT_DIFFERS", resid["fz00004"]["now"] == "EXIT_DIFFERS")


def arm_slice() -> list[ft.Slice]:
    plan = ft.plan_slices(ROWS, ft.rescore(ROWS), REPORT)
    by = {s.name: s for s in plan}
    check("SLICE: the slices are A1, B1, C1, D1, N1", sorted(by) == ["A1", "B1", "C1", "D1", "N1"], str(sorted(by)))
    check("SLICE: clause 8.x goes to A, the rest to B and AGREE rows are dropped",
          by["A1"].sigs == ["OUTPUT_DIFFERS:8.5"] and by["A1"].n_rows == 1
          and sorted(by["B1"].sigs) == ["EXIT_DIFFERS:14.9", "OUTPUT_DIFFERS:13.18.60"])
    check("SLICE: the refusal rule keeps 3 rows or more and a signature with a code no larger one carries",
          by["C1"].sigs == ["WE_REJECT:COBOL0898", "WE_REJECT:COBOL0898,COBOL0001"], str(by["C1"].sigs))
    check("SLICE: a BOTH_HANG row report.md lists nowhere is synthesized into D",
          "BOTH_HANG:fz00011" in by["D1"].sigs and "THEY_REJECT:15.66" in by["D1"].sigs, str(by["D1"].sigs))
    check("SLICE: N holds the crash classes, the unlisted RUNNER_ERROR row and the runner-error tail",
          "WE_HANG:14.9" in by["N1"].sigs and "RUNNER_ERROR:fz00012" in by["N1"].sigs and "Runner errors" in by["N1"].sigs
          and "the runner lost it" in by["N1"].text, str(by["N1"].sigs))
    many = [row(f"fz{n:05d}", "THEY_REJECT", signature="THEY_REJECT:15.66") for n in range(100, 120)]
    sec = "### `THEY_REJECT:15.66` × 20 — first: fz00100 (clause 15.66 t)\n- programs: " + ", ".join(
        r["id"] for r in many[:12]) + " …\n- source: `programs/fz00100.cob`\n"
    out = ft.plan_slices(many, [], sec)
    check("SLICE: all of a signature's programs are stated, not report.md's first dozen",
          "all 20 programs:" in out[0].text and "fz00119" in out[0].text)
    check("SLICE: the chunk size splits a slice", len(ft.plan_slices(ROWS, ft.rescore(ROWS), REPORT, 1)) > len(plan))
    return plan


def arm_briefs(root: pathlib.Path, run: pathlib.Path) -> ft.Triage:
    coord = root / "coord"
    t = ft.prepare(run, None, None, root / "triage", str(coord))
    check("BRIEFS: prepare reads the judge from run.json and the fresh judge defaults to it",
          t.judge == "judge/cobol.exe" and t.fresh_judge == t.judge and t.model == "test-model" and t.programs == len(ROWS))
    briefs = sorted((t.dir / "briefs").glob("*.md"))
    check("BRIEFS: a common brief and one per slice", len(briefs) == 1 + len(t.slices), str([b.name for b in briefs]))
    left = [b.name for b in briefs if ft.PLACEHOLDER.search(b.read_text(encoding="utf-8"))]
    check("BRIEFS: no <<NAME>> is left in a rendered brief", not left, str(left))
    check("BRIEFS: an unknown placeholder is an error", raises(lambda: ft.render_template("<<NOPE>>", {}), KeyError))
    texts = "".join(b.read_text(encoding="utf-8") for b in briefs)
    check("BRIEFS: the brief names the triage directory, the run, the WSL form and the run's stop files",
          ft.posix(t.dir) in texts and ft.posix(run) in texts and ft.wsl_path(t.dir) in texts
          and "STOP-fuzz-20990101-000000" in texts.replace("\\", "/") and "STOP" in texts)
    sources = {p: (BRIEFS_DIR / p).read_text(encoding="utf-8") for p in
               [q.name for q in BRIEFS_DIR.glob("*.md")]}
    sources["fuzz_triage.py"] = (HERE / "fuzz_triage.py").read_text(encoding="utf-8")
    sources["fuzz-triage.js"] = (REPO / ".claude" / "workflows" / "fuzz-triage.js").read_text(encoding="utf-8")
    hard = re.compile(r"scratchpad|Temp[\\/]claude|fuzz-2026\d{4}|\\E--COBOL|[A-Za-z]:\\\\Temp", re.IGNORECASE)
    check("BRIEFS: no template, the tool or the Workflow script names a night, a session scratchpad or a TEMP path",
          not [n for n, s in sources.items() if hard.search(s)], str([n for n, s in sources.items() if hard.search(s)]))
    common = (BRIEFS_DIR / "common.md").read_text(encoding="utf-8")
    vocab = common.split("## Verdict vocabulary")[1].split("## A GNUCOBOL-SIDE")[0]
    check("BRIEFS: common.md's verdict vocabulary equals the tool's",
          tuple(re.findall(r"^- `([A-Z-]+)`", vocab, re.MULTILINE)) == ft.VERDICTS, str(re.findall(r"^- `([A-Z-]+)`", vocab, re.MULTILINE)))
    bar_text = common.split("The record `")[1].split("\n- `Minimal")[0]
    names = [n.replace(" (per ISO)", "") for n in re.findall(r"`([A-Z][A-Za-z ()]+?)`", bar_text.split(":", 1)[1])]
    check("BRIEFS: common.md names the record's sections the tool and work.py require",
          all(any(n.startswith(b) for n in names) for b in ft.BAR), str(names))
    check("BRIEFS: common.md names each disposition and the refuter's outcomes are the tool's",
          all(f"`{d}`" in common for d in ft.DISPOSITIONS)
          and all(o in (BRIEFS_DIR / "refute.md").read_text(encoding="utf-8") for o in ft.OUTCOMES))
    js = sources["fuzz-triage.js"]
    adj_prefix, refute_prefix = ft.ADJ_BRIEF.split("{}")[0], ft.REFUTE_BRIEF.split("{}")[0]
    check("BRIEFS: the Workflow script names the briefs by the tool's own file names",
          adj_prefix in js and refute_prefix in js and ft.ADJ_BRIEF.split("{}")[1] in js)
    check("BRIEFS: the sibling skills the briefs name exist",
          all((REPO / p).exists() for p in (".claude/skills/workstream/templates/MANDATORY-PRACTICES.md",
                                            ".claude/skills/spec-lookup/SKILL.md")))
    check("BRIEFS: the wsl path of a drive path", ft.wsl_path("E:/a/b") == "/mnt/e/a/b" and ft.wsl_path("/x/y") == "/x/y")
    return t


BRIEFS_DIR = HERE / "fuzz_triage_briefs"


def arm_verdicts(t: ft.Triage) -> None:
    good = {"signature": "OUTPUT_DIFFERS:8.5", "count": 1, "verdict": "WISEOWL-CORRECT", "rule": "§8.5"}
    defect = {"signature": "s", "count": 1, "verdict": "WISEOWL-DEFECT", "harm": "wrong answer", "rule": "§8.5",
              "repro": "r.cob", "code_site": "a.cs:1"}
    check("VERDICTS: a valid line is silent", ft.verdict_problems(good) == [] and ft.verdict_problems(defect) == []
          and ft.verdict_problems(verdict()) == [])
    cases = {
        "a verdict outside the vocabulary": dict(good, verdict="MAYBE"),
        "no signature": {k: v for k, v in good.items() if k != "signature"},
        "no count": {k: v for k, v in good.items() if k != "count"},
        "a WISEOWL-DEFECT without its repro": {k: v for k, v in defect.items() if k != "repro"},
        "a WISEOWL-DEFECT without its code site": dict(defect, code_site=""),
        "a GNUCOBOL-SIDE without upstream": {k: v for k, v in verdict().items() if k != "upstream"},
        "upstream on another verdict": dict(good, upstream={"disposition": "record"}),
        "an unknown disposition": verdict(disposition="maybe"),
        "a record without citations": verdict(citations=[]),
        "a citation without text": verdict(citations=[{"clause": "14.9.4.4", "text": ""}]),
        "a bad class": verdict(**{"class": "slow"}),
        "a bad edition": verdict(edition="1999"),
        "a family that is no slug": verdict(family="Not A Slug"),
        "no program ids": verdict(programs=[]),
        "a witness with no note": verdict(disposition="witness", note="nope"),
        "a not-filed without a reason": verdict(disposition="not-filed"),
    }
    for name, v in cases.items():
        check(f"VERDICTS: fires on {name}", ft.verdict_problems(v) != [], str(v)[:80])
    check("VERDICTS: a witness and a not-filed are valid with their fields",
          ft.verdict_problems(verdict(disposition="witness", note="PB9001")) == []
          and ft.verdict_problems(verdict(disposition="not-filed", reason="no repro")) == [])
    put(t.dir / "adj/A1/verdicts-A1.jsonl", jsonl([good, verdict(), verdict(sig="GNUCOBOL-SIDE:dup", family="demo-family"),
                                                    dict(good, verdict="MAYBE")]) + "{not json\n")
    c = ft.collect(t)
    check("VERDICTS: collect keeps the valid lines and reports the bad ones, a duplicate family and a bad JSON line",
          len(c.verdicts) == 3 and any("MAYBE" in p for p in c.problems) and any("claimed by 2" in p for p in c.problems)
          and any("not JSON" in p for p in c.problems), str(c.problems))
    check("VERDICTS: collect names the undecided signatures of every slice",
          c.undecided.get("B1") and "OUTPUT_DIFFERS:8.5" not in c.undecided.get("A1", []), str(c.undecided))
    check("VERDICTS: the summary prints without error", bool(ft.summary(c)))
    put(t.dir / "adj/A1/verdicts-A1.jsonl", "")


def triage_with_records(t: ft.Triage, fams: dict[str, dict]) -> ft.Collected:
    """Write the verdict file, the records and the refutations of `fams` (family -> options) and collect them."""
    verdicts, refs = [], []
    for sub in ("records", "refuted"):          # a previous call's files must not satisfy this one
        for stale in (t.dir / sub).glob("*"):
            stale.unlink()
    for fam, o in fams.items():
        v = verdict(sig=f"GNUCOBOL-SIDE:{fam}", family=fam, **o.get("up", {}))
        verdicts.append(v)
        if "disposition" not in o.get("up", {}) or o["up"]["disposition"] == "record":
            put(t.dir / f"records/{fam}.md", record_md())
            put(t.dir / f"records/{fam}.cob", o.get("cob", PROGRAM))
        if o.get("refute", True):
            refs.append(dict({"family": fam, "outcome": "UPHELD", "citations": [CITE], "why": "re-ran"},
                             **o.get("refute_over", {})))
        if o.get("refuted_md", True) and (o.get("refute_over", {}).get("outcome", "UPHELD") != "OVERTURNED"):
            put(t.dir / f"refuted/{fam}.md", o.get("md") or record_md())
    put(t.dir / "adj/A1/verdicts-A1.jsonl", jsonl(verdicts))
    put(t.dir / "refute/R1/refute-R1.jsonl", jsonl(refs))
    return ft.collect(t)


def arm_refute_and_filing(root: pathlib.Path, t: ft.Triage) -> None:
    work = root / "work"
    work.mkdir()
    c = triage_with_records(t, {"fam-a": {"refute": False}, "fam-b": {}})
    plan = ft.refute_plan(t, c)
    check("REFUTE: only the unrefuted record is sliced", plan["records"] == 1 and plan["slices"] == ["R1"]
          and plan["already_refuted"] == 1, str(plan))
    brief = (t.dir / ft.REFUTE_BRIEF.format("R1")).read_text(encoding="utf-8")
    check("REFUTE: the brief names the slice's input and the corrected-copy directory",
          "refuted/<family>.md" in brief and "input-R1.jsonl" in brief and not ft.PLACEHOLDER.search(brief))
    check("REFUTE: the slice input holds the record's verdict", "fam-a" in (t.dir / "refute/R1/input-R1.jsonl").read_text(encoding="utf-8"))
    (t.dir / "refute/R1/refute-R1.jsonl").unlink()

    spec = ft.Spec()
    ids = iter(["PB9101", "PB9102", "PB9103", "PB9104"])

    def file_one(opts: dict) -> ft.Filing:
        c2 = triage_with_records(t, {"fam-x": opts})
        return ft.plan_filing(t, c2, work, spec)

    f = file_one({})
    check("FILING: a refuted, bar-meeting record is filed", len(f.drafts) == 1 and not f.held, str(f.held))
    d = f.drafts[0]
    note = ft.render_note(d, "PB9101", "2026-10-27")
    wk = ft._work()
    parsed, why = wk.read_note("PB9101.md", note)
    check("FILING: the note passes the register's own upstream bar", parsed is not None and wk.upstream_problems(parsed) == [],
          str(why or (wk.upstream_problems(parsed) if parsed else "")))
    check("FILING: the frontmatter carries the class flag, the spec refs and the campaign link",
          parsed["crashes"] is True and parsed["spec_refs"] == ["14.9.4.4"] and parsed["related"] == ["PB3838"]
          and parsed["process_only"] is True and parsed["upstream_status"] == "unreported")
    check("FILING: the minimal program is inline and the run's witnesses end the note",
          PROGRAM.strip() in note and note.rstrip().endswith("- fuzz-20990101-000000: fz00020 fz00021"))

    held = {
        "an unrefuted record": ({"refute": False}, "not refuted"),
        "an OVERTURNED record": ({"refute_over": {"outcome": "OVERTURNED", "why": "latitude"}}, "OVERTURNED"),
        "a refuter that wrote no corrected copy": ({"refuted_md": False}, "no corrected copy"),
        "a corrected copy whose program differs from the repro": ({"md": record_md(program=PROGRAM + "DISPLAY 1.")}, "equal to the repro"),
        "a record without a cobc command": ({"md": record_md(env="run it")}, "cobc command"),
        "a Draft issue without its title line": ({"md": record_md(title="no title here")}, "Title"),
        "a refuter citation that does not check":
            ({"refute_over": {"citations": [{"clause": "14.9.4.4", "text": "words the clause lacks"}]}}, "does not check"),
        "a refuter citation of a clause that does not exist":
            ({"refute_over": {"citations": [{"clause": "99.99", "text": "x"}]}}, "does not check"),
        "a refutation line with no citations": ({"refute_over": {"citations": []}}, "no `citations`"),
        # PB3972 review R1-2: the filed record is the REFUTER's copy, so its clauses are checked, not the adjudicator's
        "a corrected copy citing a clause the refuter did not list":
            ({"md": record_md(expected="§14.9.4.4 and §14.9.4.3 say so.")}, "§14.9.4.3"),
        "a refuter citation the corrected copy never names":
            ({"refute_over": {"citations": [CITE, CITE2]}}, "not named in the record's Expected result"),
        "a refutation outcome that is none": ({"refute_over": {"outcome": "MAYBE"}}, "outcome"),
        "edition 2023 as a crash": ({"up": {"edition": "2023"}}, "bar refuses"),
        "an unknown RECLASSIFY class": ({"refute_over": {"outcome": "RECLASSIFY", "class": "slow"}}, "class"),
        "a corrected copy with the unapplied-correction phrase":
            ({"md": record_md().replace("cobc 3.2 does something wrong.", "this paragraph governs")}, "bar refuses"),
        "a minimal program over the 60-line cap (the GPL guard)":
            ({"md": record_md(program=LONG), "cob": LONG}, "bar refuses"),
    }
    for name, (opts, word) in held.items():
        f2 = file_one(opts)
        check(f"FILING: holds {name}", not f2.drafts and len(f2.held) == 1 and word in f2.held[0][1], str(f2.held))

    f3 = file_one({"refute_over": {"outcome": "RECLASSIFY", "class": "wrong-output", "edition": "2002"}})
    check("FILING: a RECLASSIFY applies the refuter's class and edition",
          f3.drafts and f3.drafts[0].cls == "wrong-output" and f3.drafts[0].edition == "2002", str(f3.held))
    check("FILING: the class flag follows the refuter's class",
          "wrong_answer: true" in ft.render_note(f3.drafts[0], "PB9101", "d") and "crashes: false" in ft.render_note(f3.drafts[0], "PB9101", "d"))

    # the refuter found the more specific rule: the note's spec_refs are the refuter's checked clauses, not the adjudicator's
    f3b = file_one({"refute_over": {"citations": [CITE2]}, "md": record_md(expected="§14.9.4.3 SR 2): the literal is not empty.")})
    check("FILING: spec_refs come from the refutation line's citations of the corrected copy",
          f3b.drafts and f3b.drafts[0].spec_refs == ["14.9.4.3"] and not f3b.held, str(f3b.held))

    # write: the allocator's ids, the files, the witnesses
    f = file_one({})
    written = ft.write_filing(f, work, "2026-10-27", lambda n: [next(ids) for _ in range(n)], t.run_id)
    check("FILING: write puts the note under the allocator's id", written == ["PB9101.md"] and (work / "PB9101.md").is_file())
    check("FILING: the written note is read back as a valid upstream note",
          wk.upstream_problems(wk.read_note("PB9101.md", (work / "PB9101.md").read_text(encoding="utf-8"))[0]) == [])
    f4 = file_one({})
    check("FILING: a family an existing note records is a witness of it, never filed twice",
          not f4.drafts and f4.witnesses and f4.witnesses[0][0] == "PB9101", str((f4.drafts, f4.held, f4.witnesses)))
    before = (work / "PB9101.md").read_text(encoding="utf-8")
    ft.write_filing(f4, work, "2026-10-27", lambda n: [], t.run_id)
    check("FILING: a second file of the same run is a no-op", (work / "PB9101.md").read_text(encoding="utf-8") == before)
    f5 = ft.Filing(witnesses=[("PB9101", "fam", ["fz00099"])])
    ft.write_filing(f5, work, "2026-10-27", lambda n: [], "fuzz-20990202-000000")
    after = (work / "PB9101.md").read_text(encoding="utf-8")
    check("FILING: another run's ids are added to the Witnesses section, ids only",
          after.rstrip().endswith("- fuzz-20990202-000000: fz00099") and "- fuzz-20990101-000000: fz00020 fz00021" in after
          and wk.upstream_problems(wk.read_note("PB9101.md", after)[0]) == [])

    # PB3972 review R1-3: one mechanism seen under two signatures in ONE run gives all of both signatures' ids
    f5b = ft.Filing(witnesses=[("PB9101", "THEY_REJECT:15.66", ["fz00010", "fz00011"]),
                               ("PB9101", "THEY_FAIL_RUN:15.66", ["fz00020", "fz00011", "fz00030"])])
    ft.write_filing(f5b, work, "2026-10-27", lambda n: [], "fuzz-20990303-000000")
    after2 = (work / "PB9101.md").read_text(encoding="utf-8")
    check("FILING: two witnesses of one note in one run keep every id, once",
          "- fuzz-20990303-000000: fz00010 fz00011 fz00020 fz00030" in after2
          and after2.count("fuzz-20990303-000000") == 1, after2[-300:])
    ft.write_filing(f5b, work, "2026-10-27", lambda n: [], "fuzz-20990303-000000")
    check("FILING: filing the same witnesses again is a no-op", (work / "PB9101.md").read_text(encoding="utf-8") == after2)

    c3 = triage_with_records(t, {"fam-w": {"up": {"disposition": "witness", "note": "PB9101"}},
                                 "fam-v": {"up": {"disposition": "witness", "note": "PB1"}},
                                 "fam-n": {"up": {"disposition": "not-filed", "reason": "no repro"}}})
    f6 = ft.plan_filing(t, c3, work, spec)
    check("FILING: a witness verdict names an existing upstream note, an unknown one and a not-filed are held",
          [w[0] for w in f6.witnesses] == ["PB9101"] and len(f6.held) == 2 and not f6.drafts, str((f6.witnesses, f6.held)))

    # a refusal writes nothing
    bad = ft.Draft("bad-fam", t.run_id, "crash", "2014", "3.2.0", ["14.9.4.4"], "t", {"Summary": "s"}, ["fz00001"])
    snapshot = sorted(p.name for p in work.iterdir())
    check("FILING: a note the register's bar refuses stops the write before any file is written",
          raises(lambda: ft.write_filing(ft.Filing(drafts=[f.drafts[0], bad], witnesses=[("PB9101", "x", ["fz00005"])]),
                                         work, "2026-10-27", lambda n: ["PB9201", "PB9202"], t.run_id), SystemExit)
          and sorted(p.name for p in work.iterdir()) == snapshot
          and "fz00005" not in (work / "PB9101.md").read_text(encoding="utf-8"))

    out = ft.filing_report(t, f6, [], c3)
    check("FILING: the filing report lists what was filed, witnessed and held", "held: 2" in out and "WITNESS  PB9101" in out)


def arm_helpers() -> None:
    wk = ft._work()
    text = "a\n```cobol\nX\n  Y\n```\nb\n~~~\nZ\n~~~\n```\nopen"
    check("HELPERS: the register's fence tracker reads closed backtick and tilde blocks and ignores an unclosed one",
          wk.fenced_block_texts(text) == ["X\n  Y", "Z"], str(wk.fenced_block_texts(text)))
    # PB3972 review R1-4: a backtick run whose info string holds a backtick opens no fence (CommonMark 0.31 section 4.5);
    # the tool used to keep its own tracker that disagreed, and now asks work.py's, the one
    odd = "```a`b\n```cobol\nPROG\n```\n"
    check("HELPERS: a backtick info string with a backtick opens no fence, as work.py's tracker reads it",
          wk.fenced_block_texts(odd) == ["PROG"] and wk.fenced_blocks(odd)[1] == [(1, True)], str(wk.fenced_block_texts(odd)))
    check("HELPERS: the tool keeps no fence tracker or section lookup of its own",
          not hasattr(ft, "fence_blocks") and not hasattr(ft, "section") and not hasattr(ft, "draft_title"))
    check("HELPERS: the Draft issue title is work.py's",
          wk.draft_title("**Title:** one line\n\nbody") == "one line" and wk.draft_title("**Title:** one line\nbody") is None
          and wk.draft_title("preface\n**Title:** t\n\nb") is None)
    check("HELPERS: same_program ignores trailing blanks and surrounding blank lines", ft.same_program("A \nB\n\n", "\nA\nB"))
    check("HELPERS: same_program tells a different program", not ft.same_program("A", "B"))
    s = "## Witnesses\n\n- r1: fz1\n\n## Other\n\nx\n"
    t2 = ft.add_witness("---\nx: 1\n---\n# T\n\n" + s, "r2", ["fz2"])
    check("HELPERS: add_witness appends inside an existing Witnesses section before the next heading",
          t2.index("- r2: fz2") < t2.index("## Other") and t2.index("- r1: fz1") < t2.index("- r2: fz2"), t2)
    t3 = ft.add_witness(t2, "r1", ["fz1", "fz9"])
    check("HELPERS: add_witness appends the missing ids to the run's own bullet (R1-3) and nothing else",
          "- r1: fz1 fz9\n" in t3 and t3.count("- r1:") == 1 and ft.add_witness(t3, "r1", ["fz9", "fz1"]) == t3
          and "- r2: fz2" in t3, t3)
    check("HELPERS: add_witness creates the section when the note has none",
          ft.add_witness("# T\n\ntext\n", "r", ["fz1"]).rstrip().endswith("## Witnesses\n\n- r: fz1"))
    fake = unittest.mock.Mock(return_value=unittest.mock.Mock(stdout="PB10-PB12\n"))
    with unittest.mock.patch.object(ft.subprocess, "run", fake):
        check("HELPERS: alloc_ids parses the allocator's range", ft.alloc_ids(3, "") == ["PB10", "PB11", "PB12"])
    with unittest.mock.patch.object(ft.subprocess, "run", unittest.mock.Mock(return_value=unittest.mock.Mock(stdout="PB7\n"))):
        check("HELPERS: alloc_ids parses a single id", ft.alloc_ids(1, "") == ["PB7"])
        check("HELPERS: alloc_ids refuses an answer of the wrong size", raises(lambda: ft.alloc_ids(2, ""), SystemExit))
    cmd = ft.compile_command("j.exe", pathlib.Path("p.cob"), pathlib.Path("p.dll"), "2014")
    check("HELPERS: the verify compile command pins the edition and free format",
          cmd[:1] == ["j.exe"] and "--std" in cmd and "2014" in cmd and "--source-format" in cmd and "free" in cmd)
    spec = ft.Spec()
    ok, _first = spec.check(CITE["clause"], CITE["text"])
    bad, _first = spec.check(CITE["clause"], "words this clause does not have")
    check("HELPERS: the citation check is cite.py's own: right words pass, wrong words fail", ok and not bad)
    check("HELPERS: ordering of spec refs is numeric", sorted(["14.10", "14.9"], key=lambda x: [int(p) for p in x.split(".")]) == ["14.9", "14.10"])


def arm_cli(root: pathlib.Path, run: pathlib.Path) -> None:
    t2 = root / "cli"
    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):
        rc = ft.main(["prepare", "--run", str(run), "--triage", str(t2), "--coord", str(root / "coord")])
    summary = json.loads(buf.getvalue())
    check("CLI: prepare prints the slices and exits 0", rc == 0 and summary["slices"] and summary["signatures"] > 0)
    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):
        rc = ft.main(["collect", "--triage", str(t2)])
    check("CLI: collect exits 1 while signatures are undecided", rc == 1 and "UNDECIDED" in buf.getvalue())
    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):
        rc = ft.main(["refute-plan", "--triage", str(t2)])
    check("CLI: refute-plan with no GNUCOBOL-SIDE record prints no slices", rc == 0 and json.loads(buf.getvalue())["slices"] == [])
    work = root / "cli-work"
    work.mkdir()
    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):
        rc = ft.main(["file", "--triage", str(t2), "--work", str(work)])
    check("CLI: file without --write reports and writes nothing", rc == 0 and "dry run" in buf.getvalue()
          and list(work.iterdir()) == [] and (t2 / ft.FILING_REPORT).is_file())
    check("CLI: a triage directory that was never prepared is refused", raises(lambda: ft.load_triage(root / "nowhere"), SystemExit))
    with contextlib.redirect_stdout(io.StringIO()):
        rc = ft.main(["render", "--triage", str(t2)])
    check("CLI: render re-renders the briefs", rc == 0)

    # PB3972 review R1-5: verify reports a repro it could not re-run, and goes on to the next
    t = ft.load_triage(t2)
    defect = {"signature": "WE_REJECT:x", "count": 1, "verdict": "WISEOWL-DEFECT", "harm": "rejects legal source",
              "rule": "§14.9.4.4", "code_site": "a.cs:1"}
    put(t.dir / "probes-ok.cob", PROGRAM)
    put(t.dir / "adj/A1/verdicts-A1.jsonl", jsonl([dict(defect, repro=str(t.dir / "absent.cob")),
                                                    dict(defect, signature="WE_REJECT:y", repro=str(t.dir / "probes-ok.cob")),
                                                    dict(defect, signature="WE_REJECT:z", repro=str(t.dir / "probes-ok.cob"))]))
    calls = []

    def hang(cmd, **kw):
        calls.append(cmd)
        raise subprocess.TimeoutExpired(cmd, kw.get("timeout", 0))

    with unittest.mock.patch.object(ft.subprocess, "run", hang), contextlib.redirect_stdout(io.StringIO()):
        n, problems = ft.verify(t, ft.collect(t))
    check("CLI: verify counts every defect, reports the missing repro and the compile timeout, and runs the next repro",
          n == 3 and len(calls) == 2 and len(problems) == 3 and "does not exist" in problems[0]
          and all("outlived" in p for p in problems[1:]), str((n, calls, problems)))
    buf = io.StringIO()
    with unittest.mock.patch.object(ft.subprocess, "run", hang), contextlib.redirect_stdout(buf):
        rc = ft.main(["verify", "--triage", str(t2)])
    check("CLI: verify exits 1 when a repro could not be re-run", rc == 1 and "PROBLEM" in buf.getvalue())
    put(t.dir / "adj/A1/verdicts-A1.jsonl", "")


def arm_triage_directory(root: pathlib.Path, run: pathlib.Path) -> None:
    """PB3972 review R1-1: the read-only roles cannot write inside a git tree, and a run directory is inside one."""
    coord = root / "coord"
    d = ft.triage_directory(None, run, str(coord))
    check("TRIAGE DIR: the default is <coord>/scratch/fuzz-triage/<run id>, beside the fleet's other files",
          d == (coord / "scratch" / "fuzz-triage" / run.name).resolve(), str(d))
    in_repo_run = REPO / "conformance-suite" / "results" / "fuzz-20990101-000000"
    d2 = ft.triage_directory(None, in_repo_run, str(coord))
    check("TRIAGE DIR: a run directory inside the repository still gets a triage directory outside every checkout",
          not d2.is_relative_to(REPO) and d2.name == in_repo_run.name, str(d2))
    check("TRIAGE DIR: an explicit --triage inside the repository is refused",
          raises(lambda: ft.triage_directory(str(in_repo_run / "triage"), run, str(coord)), SystemExit))
    check("TRIAGE DIR: prepare refuses it before writing anything",
          raises(lambda: ft.prepare(run, None, None, in_repo_run / "triage", str(coord)), SystemExit)
          and not in_repo_run.exists())
    check("TRIAGE DIR: a relative --triage is refused (a drive-relative path names the wrong directory)",
          raises(lambda: ft.triage_directory("triage", run, str(coord)), SystemExit))
    # the hook that makes the rule: the real read-only guard refuses a write inside the repository, passes the default
    hook = HERE / "hooks" / "readonly_repo.py"
    refused = subprocess.run([sys.executable, str(hook)], capture_output=True, text=True, input=json.dumps(
        {"tool_input": {"file_path": str(in_repo_run / "triage" / "adj" / "C1" / "verdicts-C1.jsonl")}}))
    passed = subprocess.run([sys.executable, str(hook)], capture_output=True, text=True, input=json.dumps(
        {"tool_input": {"file_path": str(d2 / "adj" / "C1" / "verdicts-C1.jsonl")}}))
    check("TRIAGE DIR: readonly_repo.py refuses the old default and passes the new one",
          refused.returncode == 2 and passed.returncode == 0, f"{refused.returncode} {passed.returncode} {passed.stderr[:200]}")


def run() -> int:
    root = scratch_dir("fuzz-triage-test-")
    try:
        arm_rescore()
        arm_slice()
        arm_helpers()
        r = make_run(root)
        t = arm_briefs(root, r)
        arm_verdicts(t)
        arm_refute_and_filing(root, t)
        arm_cli(root, r)
        arm_triage_directory(root, r)
    finally:
        remove_tree(root)
    print("SELF-TEST: " + (f"FAIL {FAILURES}" if FAILURES else "PASS"))
    return 1 if FAILURES else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(run())
