#!/usr/bin/env python3
"""fuzz_triage.py — THE ONE TOOL OF THE MORNING AFTER A DIFFERENTIAL-FUZZ NIGHT (kb/Work PB3972, PB3838 tier 5).

    python scripts/fuzz_triage.py prepare --run <run dir> [--judge <cobol.exe>] [--fresh-judge <cobol.exe>] [--triage <dir>]
    python scripts/fuzz_triage.py collect --triage <dir>        # tally and VALIDATE the adjudicators' verdict files
    python scripts/fuzz_triage.py cites   --triage <dir>        # every clause and quoted citation of the verdicts, checked
    python scripts/fuzz_triage.py verify  --triage <dir>        # re-run every WISEOWL-DEFECT repro on the fresh judge
    python scripts/fuzz_triage.py refute-plan --triage <dir>    # slice the GNUCOBOL-SIDE records for the independent refuter
    python scripts/fuzz_triage.py file    --triage <dir> [--write]   # file what the refuter upheld as `upstream` notes
    python scripts/test_fuzz_triage.py                           # the self-test (discovered by scripts/self_tests.py)

The design is docs/rearchitecture/DESIGN-differential-fuzz.md §3-§5; the Workflow that runs the adjudicators and the
refuters is .claude/workflows/fuzz-triage.js, and every decision it makes is made HERE. The tool takes the run directory
`scripts/differential_fuzz.py` wrote and the judge `scripts/fuzz_night.ps1` named (the judge defaults to the one run.json
records), and writes everything under `--triage` (default `<run dir>/triage`, inside the git-ignored run directory): no
night-specific path, run id or date is written in this file or in the briefs it renders (scripts/fuzz_triage_briefs/).

⛔ THE TOOL READS THE RUN AND FIXES NOTHING. The owner's standing decision (2026-10-24 ~17:00 PDT): "Do not begin fixing
any fuzz identified issues without my explicit approval". `file` writes `kind: upstream` kb/Work notes, which RECORD a
defect of another project (kb/Work PB3838); it changes no compiler, test or suite file, and posts nothing anywhere.

⛔ A GNUCOBOL-SIDE VERDICT IS FILED ONLY AFTER AN INDEPENDENT REFUTER AND ONLY TO THE EIGHT-ITEM BAR. `file` writes a note
for a record whose refutation line says UPHELD or RECLASSIFY, using the refuter's corrected copy; an unrefuted, an
OVERTURNED or a bar-failing record stays in `<triage>/filing.md` and is never filed half (PB3838 standing requirement 1).
Every note is checked with the register's own `work.upstream_problems` before any note is written, every quoted citation
with `cite.py`'s own check, and the minimal program inline in the note must equal the repro file. A family an existing
`upstream` note already records gains the run's program ids as WITNESSES of that note and is never filed twice. Ids come
from `scripts/orchestrator/alloc.py` only. No GnuCOBOL source, testsuite text or expected output is written (GPL).
"""
from __future__ import annotations

import argparse
import collections
import dataclasses
import datetime
import json
import pathlib
import re
import subprocess
import sys

from standard_streams import ensure_utf8

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parent
BRIEFS = HERE / "fuzz_triage_briefs"
NL = "\n"

#: The files of a triage directory (design §3.2). Every path the Workflow script names is one of these; the test holds
#: the script's literals equal to ADJ_BRIEF and REFUTE_BRIEF.
PARAMS_FILE = "triage.json"
SLICES_FILE = "slices.json"
REFUTE_SLICES_FILE = "refute-slices.json"
RESIDUE_FILE = "batch/residue.jsonl"
ADJ_BRIEF = "briefs/adj-{}.md"
REFUTE_BRIEF = "briefs/refute-{}.md"
COMMON_BRIEF = "briefs/common.md"
FILING_REPORT = "filing.md"

#: Output-difference classes rescored from the recorded outputs (design §3.3).
RESCORED_CLASSES = ("OUTPUT_DIFFERS", "EXIT_DIFFERS", "THEY_FAIL_RUN")
#: report.md section classes of each slice kind (design §3.4).
WE_REJECT_CLASS = "WE_REJECT"
THEY_CLASSES = ("THEY_REJECT", "THEY_FAIL_RUN", "THEY_HANG", "BOTH_HANG")
CRASH_CLASSES = ("WE_HANG", "WE_FAIL_RUN", "WE_CRASH", "EXIT_DIFFERS", "RUNNER_ERROR", "NO_PROGRAM")
#: Classes whose rows may have no report.md section; their rows are listed from results.jsonl (design §3.4).
SYNTHESIZED = ("BOTH_HANG", "RUNNER_ERROR", "NO_PROGRAM")
#: Output-difference signatures of this clause prefix go to slice A (the data model and expressions), the rest to B.
SPLIT_CLAUSE_PREFIX = "8."
CHUNK = 25
REFUTE_PER_SLICE = 6
PROGRAM_ID = re.compile(r"\bfz\d+\b")
SECTION_HEAD = re.compile(r"### `([^`]*)` × ([0-9]+)")

VERDICTS = ("WISEOWL-CORRECT", "GNUCOBOL-SIDE", "LATITUDE", "UNINITIALIZED", "NOT-COMPARABLE", "KNOWN", "FIXED-SINCE",
            "WISEOWL-DEFECT", "MODEL-ERROR", "UNDECIDED")
DISPOSITIONS = ("record", "witness", "not-filed")
OUTCOMES = ("UPHELD", "RECLASSIFY", "OVERTURNED")
FAMILY = re.compile(r"[a-z0-9][a-z0-9-]*")
NOTE_ID = re.compile(r"PB\d+")
CAMPAIGN = "PB3838"
#: class -> the harm flag that describes the GnuCOBOL user (work.py: "its harm flags describe a GnuCOBOL user").
CLASS_FLAGS = {"crash": "crashes", "wrong-output": "wrong_answer", "rejects-legal-source": "rejects_legal_source",
               "accepts-forbidden-source": "under_rejects"}
COBC_STD = {"1985": "-std=cobol85", "2002": "-std=cobol2002", "2014": "-std=cobol2014"}
#: The bar's `##` section names as the record writes them (work.UPSTREAM_SECTIONS matches by the words a heading opens with).
BAR = ("Minimal test case", "Environment", "Observed result", "Expected result", "Our compiler", "Fix site",
       "Upstream state", "Draft issue")


# ── parameters of one triage ─────────────────────────────────────────────────────────────────────────────────────────

@dataclasses.dataclass
class Triage:
    """One triage directory and what it was prepared from (`triage.json`)."""
    dir: pathlib.Path
    run: pathlib.Path
    run_id: str
    judge: str
    fresh_judge: str
    model: str
    programs: int
    edition: str
    coord: str

    @property
    def slices(self) -> dict:
        return read_json(self.dir / SLICES_FILE, {})


def read_json(path: pathlib.Path, default):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        return default


def load_triage(directory: str | pathlib.Path) -> Triage:
    d = pathlib.Path(directory).resolve()
    p = read_json(d / PARAMS_FILE, None)
    if p is None:
        raise SystemExit(f"⛔ {d} is not a prepared triage directory (no {PARAMS_FILE}): run `fuzz_triage.py prepare` first")
    return Triage(dir=d, run=pathlib.Path(p["run"]), run_id=p["run_id"], judge=p["judge"], fresh_judge=p["fresh_judge"],
                  model=p["model"], programs=p["programs"], edition=p["edition"], coord=p["coord"])


def posix(p: pathlib.Path | str) -> str:
    """A path as the briefs spell it: forward slashes, which every tool the agents run accepts on Windows too."""
    return pathlib.Path(p).as_posix()


def wsl_path(p: pathlib.Path | str) -> str:
    """The WSL spelling of a Windows drive path (`E:/a/b` -> `/mnt/e/a/b`); a POSIX path is unchanged."""
    s = posix(p)
    m = re.match(r"^([A-Za-z]):/(.*)$", s)
    return f"/mnt/{m.group(1).lower()}/{m.group(2)}" if m else s


# ── the run ──────────────────────────────────────────────────────────────────────────────────────────────────────────

def read_results(run: pathlib.Path) -> list[dict]:
    p = run / "results.jsonl"
    if not p.is_file():
        raise SystemExit(f"⛔ {run} has no results.jsonl: it is not a differential-fuzz run directory")
    return [json.loads(line) for line in p.read_text(encoding="utf-8").splitlines() if line.strip()]


def read_report(run: pathlib.Path) -> str:
    p = run / "report.md"
    if not p.is_file():
        raise SystemExit(f"⛔ {run} has no report.md (python scripts/differential_fuzz.py --report {posix(run)} renders it)")
    return p.read_bytes().decode("utf-8", "replace").replace(chr(0), "")


def normalize(s: str | None) -> list[str]:
    """The harness-like normalization of a recorded output: NUL bytes read as spaces, trailing blanks stripped per line."""
    return [line.rstrip() for line in (s or "").replace(chr(0), " ").splitlines()]


def rescore(rows: list[dict]) -> list[dict]:
    """The output-difference rows in the shape the slicer reads, rescored from the RECORDED outputs (no re-run: a program
    whose normalized outputs and exit codes now agree is `AGREE`) with the first four differing lines."""
    out = []
    for r in rows:
        if r.get("verdict") not in RESCORED_CLASSES:
            continue
        we, they = r.get("we") or {}, r.get("they") or {}
        a, b = normalize(we.get("output")), normalize(they.get("output"))
        x = {"id": r["id"], "clause": r.get("clause"), "title": r.get("title"), "rc": we.get("run_rc"),
             "they_rc": they.get("run_rc"), "we_lines": len(a), "they_lines": len(b),
             "we_stderr": we.get("runner_error") or ""}
        if a == b:
            x["now"] = "AGREE" if we.get("run_rc") == they.get("run_rc") else "EXIT_DIFFERS"
        else:
            x["now"] = "THEY_FAIL_RUN" if r["verdict"] == "THEY_FAIL_RUN" else "OUTPUT_DIFFERS"
            diffs = []
            for i in range(max(len(a), len(b))):
                p = a[i] if i < len(a) else "<none>"
                q = b[i] if i < len(b) else "<none>"
                if p != q:
                    diffs.append((i + 1, p[:160], q[:160]))
                if len(diffs) == 4:
                    break
            x["diffs"] = diffs
        out.append(x)
    return out


def report_sections(text: str) -> list[tuple[str, int, str]]:
    """`(signature, rows, text)` of every `### `SIGNATURE` × N` survivor section of report.md."""
    out = []
    for part in re.split(r"(?m)^(?=### `)", text)[1:]:
        part = re.split(r"(?m)^## ", part)[0]
        m = SECTION_HEAD.match(part)
        if m:
            out.append((m.group(1), int(m.group(2)), part.rstrip() + NL))
    return out


# ── slicing ──────────────────────────────────────────────────────────────────────────────────────────────────────────

@dataclasses.dataclass
class Slice:
    name: str
    cls: str            # AB · C · D · N: which method and description the brief carries
    title: str
    input_name: str
    text: str
    sigs: list[str]
    n_rows: int


def chunks(items: list, n: int) -> list[list]:
    return [items[i:i + n] for i in range(0, len(items), n)]


def codes_of(sig: str) -> set[str]:
    return set(re.findall(r"COBOL[0-9]+", sig))


def residue_section(key: tuple[str, str], group: list[dict]) -> str:
    out = [f"## {key[0]}:{key[1]} x {len(group)} — {group[0].get('title') or ''}",
           "programs: " + " ".join(x["id"] for x in group)]
    for x in group[:4]:
        out.append(f"- {x['id']} rc {x.get('rc')}/{x.get('they_rc')} lines {x.get('we_lines')}/{x.get('they_lines')}")
        for ln, a, b in x.get("diffs", []):
            out.append(f"  - line {ln}: `{a}` | `{b}`")
        if x.get("we_stderr"):
            first = x["we_stderr"].splitlines()
            out.append("  - WiseOwl stderr: " + (first[0][:200] if first else "(empty)"))
    return NL.join(out) + NL


def synthesized_section(verdict: str, r: dict) -> str:
    we, they = r.get("we") or {}, r.get("they") or {}
    return (f"### `{verdict}:{r['id']}` × 1 — first: {r['id']} (clause {r.get('clause')} {r.get('title')}){NL}"
            f"- programs: {r['id']}{NL}- source: `programs/{r['id']}.cob`{NL}"
            f"- note: no report.md section lists this row; WiseOwl run_rc={we.get('run_rc')} hung={we.get('hung')} "
            f"crashed={we.get('crashed')} runner_error={(we.get('runner_error') or '')[:200]} reason={r.get('reason')}; "
            f"GnuCOBOL run_rc={they.get('run_rc')} hung={they.get('hung')}{NL}")


def with_all_programs(section: str, sig: str, ids_by_sig: dict[str, list[str]]) -> str:
    """report.md lists a signature's first dozen programs and an ellipsis; the slice states ALL of them, from the rows."""
    ids = ids_by_sig.get(sig, [])
    listed = set(PROGRAM_ID.findall(section.split("- source:")[0]))
    if len(ids) <= len(listed):
        return section
    return section.rstrip() + f"{NL}- all {len(ids)} programs: {' '.join(ids)}{NL}"


def plan_slices(rows: list[dict], resid: list[dict], report: str, chunk: int = CHUNK) -> list[Slice]:
    """The adjudicators' slices (design §3.4): A/B output differences by clause, C refusals of WiseOwl, D refusals and
    failures of GnuCOBOL, N crashes, hangs, exits and runner errors."""
    out: list[Slice] = []
    counts = collections.Counter(x["now"] for x in resid)

    # A, B: the output differences as the run recorded them
    groups: dict[tuple[str, str], list[dict]] = collections.defaultdict(list)
    for x in resid:
        if x["now"] != "AGREE":
            groups[(x["now"], x.get("clause") or "?")].append(x)
    order = sorted(groups, key=lambda k: (-len(groups[k]), k[0], k[1]))
    for pre, keys, lab in (("A", [k for k in order if k[1].startswith(SPLIT_CLAUSE_PREFIX)],
                            f"A (clauses {SPLIT_CLAUSE_PREFIX}x)"),
                           ("B", [k for k in order if not k[1].startswith(SPLIT_CLAUSE_PREFIX)],
                            "B (other clauses: 13.x, 14.x, 15.x ...)")):
        for i, ch in enumerate(chunks(keys, chunk), 1):
            name = f"{pre}{i}"
            head = (f"# Residue {name} {lab} as the run recorded them ({dict(counts)}){NL}{NL}Each row: WiseOwl recorded "
                    f"output | GnuCOBOL 3.2 recorded output; per-line trailing spaces stripped, NUL bytes read as spaces "
                    f"on both sides; only the first 4 differing lines.{NL}{NL}")
            out.append(Slice(name, "AB", "output-difference residue, " + lab, f"residue-{name}.md",
                             head + NL.join(residue_section(k, groups[k]) for k in ch),
                             [f"{k[0]}:{k[1]}" for k in ch], sum(len(groups[k]) for k in ch)))

    secs = report_sections(report)
    ids_by_sig: dict[str, list[str]] = collections.defaultdict(list)
    for r in rows:
        if r.get("signature"):
            ids_by_sig[r["signature"]].append(r["id"])
    listed_by_class: dict[str, set[str]] = collections.defaultdict(set)
    for sig, _n, text in secs:
        listed_by_class[sig.split(":")[0]] |= set(PROGRAM_ID.findall(text.split("- source:")[0]))
        listed_by_class[sig.split(":")[0]] |= set(ids_by_sig.get(sig, []))

    def synth(verdict: str) -> list[tuple[str, str]]:
        return [(f"{verdict}:{r['id']}", synthesized_section(verdict, r)) for r in rows
                if r.get("verdict") == verdict and r["id"] not in listed_by_class[verdict]]

    # C: WiseOwl's refusals worth an adjudicator: every signature of 3 rows or more, and the smaller ones with a
    # diagnostic code no larger signature carries
    refusals = [s for s in secs if s[0].startswith(WE_REJECT_CLASS + ":")]
    chosen = []
    for s in refusals:
        if s[1] >= 3:
            chosen.append(s)
        else:
            larger: set[str] = set()
            for t in refusals:
                if t[1] > s[1]:
                    larger |= codes_of(t[0])
            if codes_of(s[0]) - larger:
                chosen.append(s)
    chosen.sort(key=lambda s: -s[1])
    for i, ch in enumerate(chunks(chosen, chunk), 1):
        name = f"C{i}"
        out.append(Slice(name, "C", "refusal rows, WE_REJECT", f"input-{name}.md",
                         f"# Input {name} — WE_REJECT signatures, verbatim from report.md{NL}{NL}"
                         + NL.join(with_all_programs(s[2], s[0], ids_by_sig) for s in ch),
                         [s[0] for s in ch], sum(s[1] for s in ch)))

    # D: GnuCOBOL's refusals, failed runs and hangs
    theirs = [s for s in secs if s[0].split(":")[0] in THEY_CLASSES]
    theirs.sort(key=lambda s: -s[1])
    items = [(s[0], with_all_programs(s[2], s[0], ids_by_sig), s[1]) for s in theirs]
    items += [(sig, text, 1) for sig, text in synth("BOTH_HANG")]
    for i, ch in enumerate(chunks(items, chunk), 1):
        name = f"D{i}"
        out.append(Slice(name, "D", "refusal rows, GnuCOBOL-side", f"input-{name}.md",
                         f"# Input {name} — THEY_REJECT / THEY_FAIL_RUN / THEY_HANG / BOTH_HANG, verbatim{NL}{NL}"
                         + NL.join(t for _s, t, _n in ch), [s for s, _t, _n in ch], sum(n for _s, _t, n in ch)))

    # N: crashes, hangs, exits and runner errors, one slice
    nsecs = [(s[0], with_all_programs(s[2], s[0], ids_by_sig), s[1]) for s in secs if s[0].split(":")[0] in CRASH_CLASSES]
    nowof = {x["id"]: x for x in resid}
    ntext = []
    for sig, text, _n in nsecs:
        extra = "".join(f"- re-judge now for {pid}: {nowof[pid]['now'] if pid in nowof else '(none)'}{NL}"
                        for pid in PROGRAM_ID.findall(text.split("- source:")[0]) if sig.startswith("EXIT_DIFFERS"))
        ntext.append((sig, text + extra, 1))
    for verdict in ("RUNNER_ERROR", "NO_PROGRAM"):
        ntext += [(sig, text, 1) for sig, text in synth(verdict)]
    tail = report[report.find("## Runner errors"):].rstrip() + NL if "## Runner errors" in report else ""
    if tail:
        ntext.append(("Runner errors", tail, 0))
    if ntext:
        nrows = sum(1 for r in rows if r.get("verdict") in CRASH_CLASSES)
        out.append(Slice("N1", "N", "crash, hang, exit and runner class", "input-N1.md",
                         f"# Input N1 — crash/hang/exit/runner/no-program class, verbatim{NL}{NL}"
                         + NL.join(t for _s, t, _n in ntext), [s for s, _t, _n in ntext], nrows))
    return out


# ── briefs ───────────────────────────────────────────────────────────────────────────────────────────────────────────

PLACEHOLDER = re.compile(r"<<([A-Z_]+)>>")

DESCRIPTIONS = {
    "AB": ("Every OUTPUT_DIFFERS / EXIT_DIFFERS / THEY_FAIL_RUN row of the run in this slice that differs (recorded "
           "outputs). Each signature lists its programs and, for the first four, their first differing lines (WiseOwl | "
           "GnuCOBOL); `results.jsonl` holds the full GnuCOBOL output (`they.output`) (compile the program yourself on the "
           "fresh judge for WiseOwl's full output)."),
    "C": ("The `WE_REJECT:` sections of `report.md` (WiseOwl refused, GnuCOBOL compiled), verbatim: programs, first "
          "program's source path, WiseOwl's first diagnostic."),
    "D": ("The `THEY_REJECT:`, `THEY_FAIL_RUN:`, `THEY_HANG:` and `BOTH_HANG:` sections of `report.md` (GnuCOBOL refused "
          "or failed, or both hung), verbatim."),
    "N": ("The WE_HANG, WE_FAIL_RUN, WE_CRASH, EXIT_DIFFERS, RUNNER_ERROR and NO_PROGRAM sections verbatim, each "
          "EXIT_DIFFERS program followed by its rescored `now` value from `batch/residue.jsonl` when it has one."),
}


def render_template(text: str, values: dict[str, str]) -> str:
    """Fill every `<<NAME>>` of a template. An unknown name is an error, never left in a brief an agent reads."""
    def fill(m: re.Match) -> str:
        if m.group(1) not in values:
            raise KeyError(f"template placeholder <<{m.group(1)}>> has no value")
        return values[m.group(1)]
    return PLACEHOLDER.sub(fill, text)


def template(name: str) -> str:
    return (BRIEFS / name).read_text(encoding="utf-8")


def prior_records(repo: pathlib.Path) -> str:
    found = sorted((repo / "docs/rearchitecture/evidence/fleet-optimization").glob("*differential-fuzz*.md"))
    return NL.join(f"  `{p.relative_to(repo).as_posix()}`" for p in found) or "  (none recorded yet)"


def stop_files(t: Triage) -> tuple[pathlib.Path, pathlib.Path]:
    sys.path.insert(0, str(HERE / "orchestrator"))
    import coord  # noqa: PLC0415  (this directory's sibling: the one home of the stop-file names)
    scratch = coord.coord_path(t.coord) / "scratch"
    return coord.global_stop(t.coord), coord.fleet_stop(scratch / "op", t.run_id)


def turn_cap(role: str) -> int:
    sys.path.insert(0, str(HERE / "orchestrator"))
    import coord  # noqa: PLC0415
    return coord.agent_turn_cap(role)


def common_values(t: Triage) -> dict[str, str]:
    global_stop, fleet = stop_files(t)
    return {"RUN_ID": t.run_id, "RUN": posix(t.run), "JUDGE": t.judge, "FRESH_JUDGE": t.fresh_judge, "MODEL": t.model,
            "PROGRAMS": str(t.programs), "EDITION": t.edition, "TRIAGE": posix(t.dir), "WSL_TRIAGE": wsl_path(t.dir),
            "REPO": posix(REPO), "PRIOR_RECORDS": prior_records(REPO), "STOP_GLOBAL": posix(global_stop),
            "STOP_FLEET": posix(fleet), "COBC_STD": COBC_STD.get(t.edition, "-std=default"),
            "TURN_CAP": str(turn_cap("cobol-adjudicator"))}


def render_briefs(t: Triage, slices: dict) -> list[str]:
    """Write briefs/common.md and one brief per slice. Returns the files written, relative to the triage directory."""
    base = common_values(t)
    (t.dir / "briefs").mkdir(parents=True, exist_ok=True)
    written = []

    def put(rel: str, text: str) -> None:
        (t.dir / rel).write_text(text, encoding="utf-8", newline=NL)
        written.append(rel)

    put(COMMON_BRIEF, render_template(template("common.md"), base))
    for name, s in slices.items():
        values = dict(base, SLICE=name, TITLE=s["title"], INPUT=posix(t.dir / "adj" / name / s["input"]),
                      N_SIGS=str(s["n_sigs"]), N_ROWS=str(s["n_rows"]))
        values["INPUT_DESCRIPTION"] = DESCRIPTIONS[s["class"]]
        values["METHOD"] = render_template(template(f"method-{s['class']}.md"), values).rstrip()
        put(ADJ_BRIEF.format(name), render_template(template("slice.md"), values))
    return written


# ── prepare ──────────────────────────────────────────────────────────────────────────────────────────────────────────

def prepare(run: pathlib.Path, judge: str | None, fresh_judge: str | None, triage: pathlib.Path | None,
            coord_override: str | None, chunk: int = CHUNK) -> Triage:
    run = run.resolve()
    meta = read_json(run / "run.json", {})
    rows = read_results(run)
    judge = judge or meta.get("cobol_exe")
    if not judge:
        raise SystemExit("⛔ no judge: pass --judge, or the run's run.json records none (`cobol_exe`)")
    t = Triage(dir=(triage or run / "triage").resolve(), run=run, run_id=run.name, judge=str(judge),
               fresh_judge=str(fresh_judge or judge), model=str(meta.get("model", "?")), programs=len(rows),
               edition=str(meta.get("edition", "2014")), coord=coord_override or "")
    resid = rescore(rows)
    plan = plan_slices(rows, resid, read_report(run), chunk)
    t.dir.mkdir(parents=True, exist_ok=True)
    (t.dir / "batch").mkdir(exist_ok=True)
    (t.dir / RESIDUE_FILE).write_text("".join(json.dumps(x) + NL for x in resid), encoding="utf-8", newline=NL)
    for d in ("records", "refuted", "refute"):
        (t.dir / d).mkdir(exist_ok=True)
    slices = {}
    for s in plan:
        d = t.dir / "adj" / s.name
        (d / "probes").mkdir(parents=True, exist_ok=True)
        (d / s.input_name).write_text(s.text, encoding="utf-8", newline=NL)
        slices[s.name] = {"class": s.cls, "title": s.title, "input": s.input_name, "n_sigs": len(s.sigs),
                          "n_rows": s.n_rows, "sigs": s.sigs, "brief": ADJ_BRIEF.format(s.name)}
    write_json(t.dir / SLICES_FILE, slices)
    write_json(t.dir / PARAMS_FILE, {"run": posix(run), "run_id": t.run_id, "judge": t.judge,
                                     "fresh_judge": t.fresh_judge, "model": t.model, "programs": t.programs,
                                     "edition": t.edition, "coord": t.coord})
    render_briefs(t, slices)
    return t


def write_json(path: pathlib.Path, value) -> None:
    path.write_text(json.dumps(value, indent=1, ensure_ascii=False) + NL, encoding="utf-8", newline=NL)


# ── verdicts ─────────────────────────────────────────────────────────────────────────────────────────────────────────

def read_jsonl(path: pathlib.Path) -> tuple[list[dict], list[str]]:
    rows, bad = [], []
    for n, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
        if not line.strip():
            continue
        try:
            v = json.loads(line)
        except json.JSONDecodeError as e:
            bad.append(f"{path.name}:{n}: not JSON ({e.msg})")
            continue
        if isinstance(v, dict):
            rows.append(v)
        else:
            bad.append(f"{path.name}:{n}: not a JSON object")
    return rows, bad


def nonempty(v, key: str) -> bool:
    x = v.get(key)
    return isinstance(x, str) and bool(x.strip())


def upstream_shape(up) -> list[str]:
    """What is wrong with the `upstream` object of a GNUCOBOL-SIDE verdict (design §5.1)."""
    if not isinstance(up, dict):
        return ["a GNUCOBOL-SIDE verdict has no `upstream` object (disposition record · witness · not-filed)"]
    d = up.get("disposition")
    if d not in DISPOSITIONS:
        return [f"upstream.disposition {d!r} is none of {list(DISPOSITIONS)}"]
    bad = []
    if d == "not-filed":
        return [] if nonempty(up, "reason") else ["upstream.not-filed carries no `reason`"]
    progs = up.get("programs")
    if not (isinstance(progs, list) and progs and all(isinstance(p, str) and PROGRAM_ID.fullmatch(p) for p in progs)):
        bad.append("upstream.programs is not a non-empty list of the run's program ids")
    if d == "witness":
        if not (isinstance(up.get("note"), str) and NOTE_ID.fullmatch(up["note"])):
            bad.append("upstream.witness names no existing note (`PB####`)")
        return bad
    work = _work()
    if not (isinstance(up.get("family"), str) and FAMILY.fullmatch(up["family"])):
        bad.append("upstream.family is not a lowercase slug [a-z0-9-]+")
    if up.get("class") not in work.UPSTREAM_CLASSES:
        bad.append(f"upstream.class {up.get('class')!r} is none of {list(work.UPSTREAM_CLASSES)}")
    if up.get("edition") not in work.UPSTREAM_EDITIONS:
        bad.append(f"upstream.edition {up.get('edition')!r} is none of {list(work.UPSTREAM_EDITIONS)}")
    for k in ("version", "record", "repro", "draft_title"):
        if not nonempty(up, k):
            bad.append(f"upstream.{k} is empty")
    cites = up.get("citations")
    if not (isinstance(cites, list) and cites and all(
            isinstance(c, dict) and nonempty(c, "clause") and nonempty(c, "text") for c in cites)):
        bad.append("upstream.citations is not a non-empty list of {clause, text}: every clause the expected result rests "
                   "on, with the quoted words `cite.py --check` accepted")
    return bad


def verdict_problems(v: dict) -> list[str]:
    """Why one verdict line is not a verdict (design §4.1). The vocabulary is common.md's, held equal by the test."""
    bad = []
    if not nonempty(v, "signature"):
        bad.append("has no `signature`")
    if v.get("verdict") not in VERDICTS:
        bad.append(f"verdict {v.get('verdict')!r} is not in the vocabulary")
        return bad
    if not isinstance(v.get("count"), int) or v["count"] < 0:
        bad.append("has no integer `count`")
    if v["verdict"] == "WISEOWL-DEFECT":
        for k in ("repro", "harm", "rule", "code_site"):
            if not nonempty(v, k):
                bad.append(f"a WISEOWL-DEFECT has no `{k}`")
    if v["verdict"] == "GNUCOBOL-SIDE":
        bad += upstream_shape(v.get("upstream"))
    elif "upstream" in v:
        bad.append("carries `upstream`, which only a GNUCOBOL-SIDE verdict has")
    return bad


@dataclasses.dataclass
class Collected:
    verdicts: list[tuple[str, dict]]       # (slice, verdict line)
    problems: list[str]
    undecided: dict[str, list[str]]        # slice -> signatures with no verdict line


def collect(t: Triage) -> Collected:
    verdicts, problems = [], []
    seen = collections.defaultdict(set)
    slices = t.slices
    for f in sorted((t.dir / "adj").glob("*/verdicts-*.jsonl")):
        name = f.parent.name
        rows, bad = read_jsonl(f)
        problems += [f"{name}: {b}" for b in bad]
        for v in rows:
            why = verdict_problems(v)
            problems += [f"{name}: {v.get('signature')!r}: {w}" for w in why]
            if not why:
                verdicts.append((name, v))
                seen[name].add(v["signature"])
    for fam, ups in group_by_family(verdicts).items():
        if len(ups) > 1:
            problems.append(f"family {fam!r} is claimed by {len(ups)} verdicts: one record per family, a second "
                            f"mechanism is `<family>--<suffix>`")
    undecided = {n: [s for s in sl["sigs"] if s not in seen[n]] for n, sl in slices.items()}
    return Collected(verdicts, problems, {n: u for n, u in undecided.items() if u})


def group_by_family(verdicts: list[tuple[str, dict]]) -> dict[str, list[dict]]:
    out = collections.defaultdict(list)
    for _s, v in verdicts:
        up = v.get("upstream")
        if v.get("verdict") == "GNUCOBOL-SIDE" and up.get("disposition") == "record":
            out[up["family"]].append(v)
    return out


def summary(c: Collected) -> list[str]:
    tally, rows = collections.Counter(), collections.Counter()
    for _s, v in c.verdicts:
        tally[v["verdict"]] += 1
        rows[v["verdict"]] += v["count"]
    out = ["signatures by verdict: " + json.dumps(dict(tally)), "programs by verdict:   " + json.dumps(dict(rows))]
    disp = collections.Counter(v["upstream"]["disposition"] for _s, v in c.verdicts if v["verdict"] == "GNUCOBOL-SIDE")
    out.append("GNUCOBOL-SIDE dispositions: " + json.dumps(dict(disp)))
    for s, v in c.verdicts:
        if v["verdict"] in ("WISEOWL-DEFECT", "UNDECIDED", "FIXED-SINCE", "KNOWN"):
            out += ["", f"[{s}] {v['signature']} x{v['count']} {v['verdict']} harm={v.get('harm')}",
                    f"  rule: {v.get('rule')}", f"  note: {v.get('note')}",
                    f"  repro: {v.get('repro')} | site: {v.get('code_site')}"]
    for n, sigs in c.undecided.items():
        out.append(f"UNDECIDED in {n}: {len(sigs)} signature(s): " + ", ".join(sigs[:8]) + (" ..." if len(sigs) > 8 else ""))
    out += [f"PROBLEM {p}" for p in c.problems]
    return out


# ── citations ────────────────────────────────────────────────────────────────────────────────────────────────────────

def _work():
    sys.path.insert(0, str(HERE / "spec"))
    import work  # noqa: PLC0415  (the register's one reader and the upstream bar)
    return work


class Spec:
    """specs/ISO_COBOL.md loaded once, for cite.py's own clause check."""

    def __init__(self) -> None:
        sys.path.insert(0, str(HERE / "spec"))
        import cite  # noqa: PLC0415
        self.cite = cite
        self.lines, self.clauses = cite.load()
        self.known = {c[0] for c in self.clauses}

    def check(self, clause: str, text: str) -> tuple[bool, str]:
        rc, out = self.cite.check(self.lines, self.clauses, clause, text)
        return rc == 0, out[0].splitlines()[0]


CLAUSE_IN_TEXT = re.compile(r"§\s*([0-9A-F]+(?:\.[0-9]+)*)")


def citation_lines(c: Collected, spec: Spec) -> list[tuple[bool, str]]:
    out = []
    for s, v in c.verdicts:
        for clause in sorted(set(CLAUSE_IN_TEXT.findall(v.get("rule") or ""))):
            out.append((clause in spec.known, f"[{s}] {v['signature']}: §{clause} " + ("exists" if clause in spec.known
                                                                                          else "is NOT a clause of the transcription")))
        up = v.get("upstream") or {}
        for cit in up.get("citations", []) if up.get("disposition") == "record" else []:
            ok, first = spec.check(cit["clause"], cit["text"])
            out.append((ok, f"[{s}] {up['family']}: {first}"))
    return out


# ── verify ───────────────────────────────────────────────────────────────────────────────────────────────────────────

def compile_command(judge: str, src: pathlib.Path, dll: pathlib.Path, edition: str) -> list[str]:
    return [judge, str(src), "--std", edition, "--source-format", "free", "-o", str(dll)]


def verify(t: Triage, c: Collected) -> int:
    n = 0
    for s, v in c.verdicts:
        if v["verdict"] != "WISEOWL-DEFECT":
            continue
        n += 1
        src = pathlib.Path(v["repro"])
        src = src if src.is_absolute() else t.dir / src
        d = t.dir / "verify" / f"{n:02d}-{re.sub(r'[^A-Za-z0-9]+', '-', v['signature']).strip('-')[:40]}"
        d.mkdir(parents=True, exist_ok=True)
        print("=" * 20, f"[{s}] {v['signature']}", src.name, "exists" if src.is_file() else "MISSING")
        if not src.is_file():
            continue
        print(src.read_text(encoding="utf-8", errors="replace")[:1400])
        dll = d / "p.dll"
        cp = subprocess.run(compile_command(t.fresh_judge, src, dll, t.edition), capture_output=True, text=True,
                            encoding="utf-8", errors="replace", timeout=120, cwd=d)
        print("-" * 8, "compile rc", cp.returncode)
        print((cp.stdout + cp.stderr).strip()[:900])
        if dll.is_file():
            try:
                rp = subprocess.run(["dotnet", str(dll)], capture_output=True, text=True, encoding="utf-8",
                                    errors="replace", timeout=30, cwd=d, stdin=subprocess.DEVNULL)
                print("-" * 8, "run rc", rp.returncode)
                print(rp.stdout[:800].replace(chr(0), "<NUL>"))
                if rp.stderr.strip():
                    print("stderr:", rp.stderr.strip()[:300])
            except subprocess.TimeoutExpired:
                print("TIMEOUT")
    return n


# ── refutation ───────────────────────────────────────────────────────────────────────────────────────────────────────

def refutations(t: Triage) -> dict[str, dict]:
    """family -> its refutation line (the last one wins: a replacement refuter may re-decide)."""
    out = {}
    for f in sorted((t.dir / "refute").glob("*/refute-*.jsonl")):
        rows, _bad = read_jsonl(f)
        for r in rows:
            if isinstance(r.get("family"), str):
                out[r["family"]] = r
    return out


def refute_plan(t: Triage, c: Collected, per: int = REFUTE_PER_SLICE) -> dict:
    """Slice the GNUCOBOL-SIDE records not yet refuted into the refuters' slices and render their briefs. A record with a
    refutation line is skipped, so a replacement run resumes."""
    done = refutations(t)
    todo = [(s, v) for s, v in c.verdicts if v["verdict"] == "GNUCOBOL-SIDE" and v["upstream"]["disposition"] == "record"
            and v["upstream"]["family"] not in done]
    base = common_values(t)
    base["TURN_CAP"] = str(turn_cap("cobol-refuter"))
    slices = {}
    for i, ch in enumerate(chunks(todo, per), 1):
        name = f"R{i}"
        d = t.dir / "refute" / name
        d.mkdir(parents=True, exist_ok=True)
        (d / f"input-{name}.jsonl").write_text("".join(json.dumps(v, ensure_ascii=False) + NL for _s, v in ch),
                                               encoding="utf-8", newline=NL)
        values = dict(base, SLICE=name, INPUT=posix(d / f"input-{name}.jsonl"), N_SIGS=str(len(ch)))
        (t.dir / "briefs").mkdir(parents=True, exist_ok=True)
        (t.dir / REFUTE_BRIEF.format(name)).write_text(render_template(template("refute.md"), values), encoding="utf-8",
                                                       newline=NL)
        slices[name] = {"families": [v["upstream"]["family"] for _s, v in ch], "brief": REFUTE_BRIEF.format(name)}
    write_json(t.dir / REFUTE_SLICES_FILE, slices)
    return {"slices": list(slices), "records": len(todo), "already_refuted": len(done)}


# ── filing ───────────────────────────────────────────────────────────────────────────────────────────────────────────

def fence_blocks(text: str) -> list[str]:
    """The contents of the closed fenced blocks of a text (a run of three or more backticks or tildes opens one)."""
    blocks, cur, run = [], None, None
    for line in text.splitlines():
        m = re.match(r"^[ \t]*(`{3,}|~{3,})(.*)$", line)
        if cur is None:
            if m:
                cur, run = [], m.group(1)
        elif m and m.group(1)[0] == run[0] and len(m.group(1)) >= len(run) and not m.group(2).strip():
            blocks.append(NL.join(cur))
            cur = None
        else:
            cur.append(line)
    return blocks


def same_program(a: str, b: str) -> bool:
    return [x.rstrip() for x in a.strip().splitlines()] == [x.rstrip() for x in b.strip().splitlines()]


def record_sections(text: str) -> dict[str, str]:
    return _work().body_sections(text)


def section(sections: dict[str, str], name: str) -> str | None:
    return next((t for h, t in sections.items() if h.lower().startswith(name.lower())), None)


def draft_title(sections: dict[str, str]) -> str | None:
    m = re.match(r"\s*\*\*Title:\*\*[ \t]+(\S[^\n]*)", section(sections, "Draft issue") or "")
    return m.group(1).strip() if m else None


def resolve(t: Triage, p: str) -> pathlib.Path:
    q = pathlib.Path(p)
    return q if q.is_absolute() else t.dir / q


@dataclasses.dataclass
class Draft:
    """A record that passed every check short of its id: what `file` will write."""
    family: str
    run_id: str
    cls: str
    edition: str
    version: str
    spec_refs: list[str]
    title: str
    sections: dict[str, str]
    programs: list[str]


@dataclasses.dataclass
class Filing:
    drafts: list[Draft] = dataclasses.field(default_factory=list)
    witnesses: list[tuple[str, str, list[str]]] = dataclasses.field(default_factory=list)   # (note id, family, programs)
    held: list[tuple[str, str]] = dataclasses.field(default_factory=list)                    # (family, reason)

    def lines(self) -> list[str]:
        out = [f"filed: {len(self.drafts)} · witnessed: {len(self.witnesses)} · held: {len(self.held)}"]
        out += [f"FILE  {d.family} ({d.cls}, {d.edition}): {d.title}" for d in self.drafts]
        out += [f"WITNESS  {n} <- {fam}: {' '.join(p)}" for n, fam, p in self.witnesses]
        out += [f"HELD  {fam}: {why}" for fam, why in self.held]
        return out


def existing_families(work_dir: pathlib.Path) -> dict[str, str]:
    """slug -> note id of every `kind: upstream` note: the word after the id in its first heading (design §5.3)."""
    work = _work()
    out = {}
    for p in sorted(work_dir.glob("PB*.md")):
        note, _why = work.read_note(p.name, p.read_text(encoding="utf-8"))
        if note and note.get("kind") == "upstream":
            m = re.search(r"^# (PB\d+) — ([a-z0-9][a-z0-9-]*)\b", note.get("_body", ""), re.MULTILINE)
            if m:
                out[m.group(2)] = m.group(1)
    return out


def upstream_ids(work_dir: pathlib.Path) -> set[str]:
    work = _work()
    out = set()
    for p in sorted(work_dir.glob("PB*.md")):
        note, _why = work.read_note(p.name, p.read_text(encoding="utf-8"))
        if note and note.get("kind") == "upstream":
            out.add(note["id"])
    return out


def draft_problems(d: Draft) -> list[str]:
    """The register's own bar on the note this draft would become (id PB0 stands in until the allocator gives one)."""
    work = _work()
    note, why = work.read_note("PB0.md", render_note(d, "PB0", "1970-01-01"))
    return [why] if note is None else work.upstream_problems(note)


def plan_filing(t: Triage, c: Collected, work_dir: pathlib.Path, spec: Spec | None = None) -> Filing:
    f = Filing()
    refuted = refutations(t)
    existing = existing_families(work_dir)
    upstream_note_ids = upstream_ids(work_dir)
    spec = spec or Spec()
    taken: set[str] = set()
    for _s, v in c.verdicts:
        if v["verdict"] != "GNUCOBOL-SIDE":
            continue
        up = v["upstream"]
        fam = up.get("family", "-")
        if up["disposition"] == "not-filed":
            f.held.append((f"{v['signature']}", "not filed by the adjudicator: " + up["reason"]))
            continue
        if up["disposition"] == "witness":
            if up["note"] in upstream_note_ids:
                f.witnesses.append((up["note"], v["signature"], up["programs"]))
            else:
                f.held.append((v["signature"], f"witness of {up['note']}, which is no `kind: upstream` note"))
            continue
        why = record_held(t, v, refuted.get(fam), spec)
        if isinstance(why, str):
            f.held.append((fam, why))
        elif fam in existing:
            f.witnesses.append((existing[fam], fam, up["programs"]))
        elif fam in taken:
            f.held.append((fam, "a second record of this family in the same run: fold it into the first"))
        else:
            taken.add(fam)
            f.drafts.append(why)
    for d in list(f.drafts):
        problems = draft_problems(d)
        if problems:
            f.drafts.remove(d)
            f.held.append((d.family, "the register's bar refuses the note: " + " | ".join(problems)))
    return f


def record_held(t: Triage, v: dict, ref: dict | None, spec: Spec) -> Draft | str:
    """A Draft for a verdict whose record can be filed, or the reason it cannot (it stays in the triage report)."""
    up = v["upstream"]
    if ref is None:
        return "not refuted: every GNUCOBOL-SIDE verdict goes to an independent refuter before it is filed (refute-plan)"
    if ref.get("outcome") not in OUTCOMES:
        return f"refutation outcome {ref.get('outcome')!r} is none of {list(OUTCOMES)}"
    if ref["outcome"] == "OVERTURNED":
        return "OVERTURNED by the refuter: " + str(ref.get("why", "")).strip()[:300]
    work = _work()
    cls = ref.get("class") if ref["outcome"] == "RECLASSIFY" and ref.get("class") else up["class"]
    edition = ref.get("edition") or up["edition"]
    if cls not in work.UPSTREAM_CLASSES or edition not in work.UPSTREAM_EDITIONS:
        return f"the refuter's class {cls!r} / edition {edition!r} is not one the register accepts"
    refuted_md = t.dir / "refuted" / f"{up['family']}.md"
    if not refuted_md.is_file():
        return f"the refuter {ref['outcome']} the record but wrote no corrected copy {posix(refuted_md)}"
    repro = refuted_md.with_suffix(".cob")
    repro = repro if repro.is_file() else resolve(t, up["repro"])
    if not repro.is_file():
        return f"the repro {posix(repro)} does not exist"
    text = refuted_md.read_text(encoding="utf-8")
    secs = record_sections(text)
    missing = [n for n in BAR if not (section(secs, n) or "").strip()]
    if missing:
        return "the record has no non-empty section: " + ", ".join(missing)
    if not any(same_program(b, repro.read_text(encoding="utf-8")) for b in fence_blocks(section(secs, "Minimal test case"))):
        return "the Minimal test case section carries no fenced block equal to the repro file: copy the program INTO the note"
    if "cobc" not in (section(secs, "Environment") or ""):
        return "the Environment section does not name the cobc command"
    title = draft_title(secs)
    if title is None:
        return "the Draft issue section does not open with its `**Title:**` line"
    bad = []
    for cit in up["citations"]:
        ok, first = spec.check(cit["clause"], cit["text"])
        if not ok:
            bad.append(first)
    if bad:
        return "a citation does not check: " + " | ".join(bad)
    refs = sorted({cit["clause"] for cit in up["citations"]},
                  key=lambda x: [(0, int(p)) if p.isdigit() else (1, p) for p in x.split(".")])
    return Draft(up["family"], t.run_id, cls, edition, up["version"], refs, title, secs, up["programs"])


def render_note(d: Draft, pb: str, date: str) -> str:
    """The note text of one draft (design §5.4): the register's frontmatter of an `upstream` record, then the record's
    sections in the bar's order with the run's witnesses at the end."""
    flag = CLASS_FLAGS.get(d.cls)
    flags = {k: (k == flag) for k in ("wrong_answer", "crashes", "silent", "rejects_legal_source", "under_rejects")}
    fm = ["---", f"title: {json.dumps(pb + ' — ' + d.title, ensure_ascii=False)}", f"id: {pb}", "kind: upstream",
          "status: open", "severity: MINOR", "area: tooling/upstream"]
    fm += [f"{k}: {str(x).lower()}" for k, x in flags.items()]
    fm += ["process_only: true", "blocked_by: []", "cluster: []", "tier: []",
           "spec_refs: [" + ", ".join(json.dumps(r) for r in d.spec_refs) + "]", "inventory_rows: []", "closes_rows: []",
           f"related: [{CAMPAIGN}]", f"filed: {date}",
           f"source: {json.dumps(f'the differential-fuzz triage of {d.run_id} (scripts/fuzz_triage.py file, kb/Work PB3972): {CAMPAIGN} plan item 5')}",
           f"tags: [upstream, gnucobol, {d.cls}, fuzz-triage]", "upstream_project: GnuCOBOL",
           f"upstream_version: {d.version}", f"upstream_class: {d.cls}", f"upstream_edition: {d.edition}",
           "upstream_status: unreported", "---", ""]
    body = [f"# {pb} — {d.family}: {d.title}", "",
            f"Filed {date} by `scripts/fuzz_triage.py file` from the triage of the differential-fuzz run `{d.run_id}`, "
            f"after an independent refuter. Upstream status `unreported`: the tracker has not been searched (owner-gated, "
            f"{CAMPAIGN}). The run directory is git-ignored and local: the minimal program below is the repro.", ""]
    used = set()
    order = ["Summary"] + list(BAR)
    for name in order:
        key = next((h for h in d.sections if h.lower().startswith(name.lower())), None)
        if key is not None:
            used.add(key)
            body += [f"## {key}", "", d.sections[key].strip(), ""]
    for key, text in d.sections.items():
        if key not in used and not key.lower().startswith("witnesses"):
            body += [f"## {key}", "", text.strip(), ""]
    body += ["## Witnesses", "", f"- {d.run_id}: {' '.join(d.programs)}", ""]
    return NL.join(fm + body)


def add_witness(text: str, run_id: str, programs: list[str]) -> str:
    """The note text with a witness bullet for this run's program ids (ids only, never program text); a run already
    named in the section adds nothing, so a second `file` is a no-op."""
    bullet = f"- {run_id}: {' '.join(programs)}"
    work = _work()
    secs = work.body_sections(text)
    key = next((h for h in secs if h.lower().startswith("witnesses")), None)
    if key is None:
        return text.rstrip() + f"{NL}{NL}## Witnesses{NL}{NL}{bullet}{NL}"
    if f"- {run_id}:" in secs[key]:
        return text
    # the section may be followed by another `##`: insert after its last non-blank line
    lines = text.split(NL)
    start = next(i for i, ln in enumerate(lines) if ln.strip() == f"## {key}")
    end = next((i for i in range(start + 1, len(lines)) if lines[i].startswith("## ")), len(lines))
    last = max([i for i in range(start + 1, end) if lines[i].strip()], default=start)
    lines.insert(last + 1, bullet)
    return NL.join(lines)


def alloc_ids(count: int, coord_override: str) -> list[str]:
    """`count` fresh kb/Work ids from the ONE locked allocator (scripts/orchestrator/alloc.py)."""
    cmd = [sys.executable, str(HERE / "orchestrator" / "alloc.py"), "pb", str(count), "--repo", str(REPO)]
    if coord_override:
        cmd += ["--coord", coord_override]
    out = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", check=True).stdout.strip()
    m = re.fullmatch(r"PB(\d+)(?:-PB(\d+))?", out)
    if not m:
        raise SystemExit(f"⛔ alloc.py answered {out!r}, not a PB id range")
    first, last = int(m.group(1)), int(m.group(2) or m.group(1))
    ids = [f"PB{n}" for n in range(first, last + 1)]
    if len(ids) != count:
        raise SystemExit(f"⛔ asked alloc.py for {count} ids and got {out!r}")
    return ids


def write_filing(f: Filing, work_dir: pathlib.Path, date: str, allocate, run_id: str) -> list[str]:
    """Write the notes and the witness lines. Everything is rendered and checked BEFORE the first byte is written, so a
    refusal leaves the register as it was. Returns the files written."""
    work = _work()
    ids = allocate(len(f.drafts)) if f.drafts else []
    notes = {}
    for d, pb in zip(f.drafts, ids):
        text = render_note(d, pb, date)
        note, why = work.read_note(f"{pb}.md", text)
        problems = [why] if note is None else work.upstream_problems(note)
        if problems:
            raise SystemExit("⛔ the note for " + d.family + " fails the register's bar: " + " | ".join(problems))
        notes[work_dir / f"{pb}.md"] = text
    edits = {}
    for note_id, _fam, programs in f.witnesses:
        p = work_dir / f"{note_id}.md"
        edits[p] = add_witness(edits.get(p) or p.read_text(encoding="utf-8"), run_id, programs)
    for p, text in {**notes, **edits}.items():
        p.write_text(text, encoding="utf-8", newline=NL)
    return [pathlib.Path(p).name for p in {**notes, **edits}]


def filing_report(t: Triage, f: Filing, written: list[str], c: Collected) -> str:
    out = [f"# Filing report — {t.run_id}", "", "Written by `scripts/fuzz_triage.py file`.", ""]
    out += ["- " + line for line in f.lines()]
    if written:
        out += ["", "Files written: " + ", ".join(written)]
    if c.problems:
        out += ["", "Verdict problems (not filed):"] + ["- " + p for p in c.problems]
    return NL.join(out) + NL


# ── command line ─────────────────────────────────────────────────────────────────────────────────────────────────────

def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    p = sub.add_parser("prepare", help="rescore the run, slice it, render the adjudicators' briefs")
    p.add_argument("--run", required=True, help="the run directory scripts/differential_fuzz.py wrote")
    p.add_argument("--judge", help="the judge of the run (default: run.json's cobol_exe)")
    p.add_argument("--fresh-judge", help="the build to re-run probes on (default: the judge)")
    p.add_argument("--triage", help="the triage directory (default: <run>/triage)")
    p.add_argument("--coord", default="", help="the coordination directory (default: coord.py's)")
    p.add_argument("--chunk", type=int, default=CHUNK, help="signatures per slice")
    for name, helptext in (("collect", "tally and validate the verdict files"), ("cites", "check every citation"),
                           ("verify", "re-run every WISEOWL-DEFECT repro on the fresh judge"),
                           ("refute-plan", "slice the GNUCOBOL-SIDE records for the refuters"),
                           ("render", "re-render the briefs of a prepared triage"),
                           ("file", "file the upheld GNUCOBOL-SIDE records as upstream notes")):
        q = sub.add_parser(name, help=helptext)
        q.add_argument("--triage", required=True, help="the prepared triage directory")
        if name == "refute-plan":
            q.add_argument("--per", type=int, default=REFUTE_PER_SLICE, help="records per refuter slice")
        if name == "file":
            q.add_argument("--write", action="store_true", help="allocate ids and write the notes (default: report only)")
            q.add_argument("--date", default=datetime.date.today().isoformat(), help="the `filed:` date")
            q.add_argument("--work", default=str(REPO / "kb" / "Work"), help="the register directory")
    args = ap.parse_args(argv)
    if args.cmd == "prepare":
        t = prepare(pathlib.Path(args.run), args.judge, args.fresh_judge, pathlib.Path(args.triage) if args.triage else None,
                    args.coord, args.chunk)
        sl = t.slices
        print(json.dumps({"triage": posix(t.dir), "slices": list(sl), "signatures": sum(s["n_sigs"] for s in sl.values()),
                          "rows": sum(s["n_rows"] for s in sl.values())}))
        return 0
    t = load_triage(args.triage)
    if args.cmd == "render":
        print("\n".join(render_briefs(t, t.slices)))
        return 0
    c = collect(t)
    if args.cmd == "collect":
        print("\n".join(summary(c)))
        return 1 if c.problems or c.undecided else 0
    if args.cmd == "cites":
        lines = citation_lines(c, Spec())
        print("\n".join(("OK    " if ok else "FAIL  ") + s for ok, s in lines))
        return 0 if all(ok for ok, _s in lines) else 1
    if args.cmd == "verify":
        n = verify(t, c)
        print(f"{n} WISEOWL-DEFECT repro(s) re-run")
        return 0
    if args.cmd == "refute-plan":
        print(json.dumps(refute_plan(t, c, args.per)))
        return 0
    work_dir = pathlib.Path(args.work)
    f = plan_filing(t, c, work_dir)
    written = write_filing(f, work_dir, args.date, lambda n: alloc_ids(n, t.coord), t.run_id) if args.write else []
    report = filing_report(t, f, written, c)
    (t.dir / FILING_REPORT).write_text(report, encoding="utf-8", newline=NL)
    print(report)
    print("next: python scripts/spec/work.py check" if written else "dry run: pass --write to file")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
