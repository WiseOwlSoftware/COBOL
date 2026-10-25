#!/usr/bin/env python3
"""turns_by_group_size.py — the turns a fix-lane agent spent against the number of notes its group carried: the
measurement `model_rules.json` `wave.max_notes_per_group_source` cites (kb/Work PB3641).

WHY. PB3611 raised the group cap from 5 to 8 notes on a fit computed by an untracked scratch script that counted the
note ids in each REPORT's title: 188 of the 316 reports it read counted 0 notes (their titles name none), so the line
rested on 128 reports, three notes and more on 12, and six on one; the source string then claimed a SPLIT onset at
190 turns that the script never computed. A provenance string has to name a measurement anyone can re-run, so the
measurement lives here, and it counts the notes where the planner wrote them: the first line of the group's DISPATCH
SPEC (`DISPATCH SPEC — wave <n> (<L>), <label>: PB…, PB…`, written by `make_dispatch_specs.py`).

THE JOIN. A report in the coordination directory's reports directory (`coord.reports_dir()`) is named
`w<wave><letter>-<lead>-report.md` (plan_wave.REPORT_NAME, the one fix-lane report name; a same-file successor's
letter carries its digit, `w1021b2-…`); its spec is `msg-w<wave>-<letter>.txt` under the scratch directory (searched
three levels down: the wave's own scratch, or the operator's `op/`). A `w*-report.md` that is no group report (a train,
refute or filing report) is left out and counted as such. A spec name found more than once with different note
sets is AMBIGUOUS and left out (counted); a report with no spec, or no turns figure, is left out (counted). Every
`w*-report.md` is therefore either joined or counted under exactly one cause (kb/Work PB3711). The turns
are the agent's own figure in its report header (`**turns:** ~N of 220`, `**turns:** about N`, `turns ~N` or
`Turns used: about N`): an estimate the agent writes, never a transcript count, so the fit is a planning figure, not a cost.

Usage:
    python scripts/orchestrator/turns_by_group_size.py [--coord <dir>] [--json]
    python scripts/orchestrator/turns_by_group_size.py --self-test
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import statistics
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import coord  # noqa: E402
from plan_wave import REPORT_NAME  # noqa: E402  (the ONE fix-lane report name, kb/Work PB3711: group letter AND successor digit)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

TURNS = re.compile(r"(?:\*\*turns:\*\*\s*(?:about\s+)?|\bturns\s+~|Turns used[^:\n]*:\s*(?:about\s*)?)~?\s*(\d+)", re.I)
STATUS = re.compile(r"^\s*(?:\*\*Status:\*\*\s*|Status:\s*)?(DONE|SPLIT|DISCHARGED|BLOCKED|GATE-PENDING)\b", re.M)
NOTE = re.compile(r"\bPB\d{3,5}\b")
HEAD_BYTES = 4000   # the header block, where the template puts the status and the turns


def spec_notes(scratch: pathlib.Path) -> dict[str, frozenset[str] | None]:
    """Each spec file name -> the notes its first line names; None where two specs of one name disagree."""
    out: dict[str, frozenset[str] | None] = {}
    for depth in ("msg-w*.txt", "*/msg-w*.txt", "*/*/msg-w*.txt"):
        for f in scratch.glob(depth):
            try:
                first = f.read_text(encoding="utf-8", errors="replace").split("\n", 1)[0]
            except OSError:
                continue
            if not first.startswith("DISPATCH SPEC"):
                continue
            notes = frozenset(NOTE.findall(first))
            if f.name in out and out[f.name] != notes:
                out[f.name] = None
            else:
                out.setdefault(f.name, notes)
    return out


def measure(cdir: pathlib.Path) -> dict:
    """One row per report joined to its spec, and the counts of what could not be joined."""
    specs = spec_notes(cdir / "scratch")
    rows = []
    skipped = {"not a group report": 0, "no turns figure": 0, "no spec": 0, "ambiguous spec": 0, "spec names no note": 0}
    for f in sorted(coord.reports_dir(str(cdir)).glob("w*-report.md")):
        m = REPORT_NAME.match(f.name)
        if not m:
            skipped["not a group report"] += 1     # a train, refute or filing report: no group spec to join
            continue
        head = f.read_text(encoding="utf-8", errors="replace")[:HEAD_BYTES]
        t = TURNS.search(head)
        if not t:
            skipped["no turns figure"] += 1
            continue
        # A same-file successor (`w1021b2-…`) is the group `b2`, whose spec is `msg-w1021-b2.txt` (make_dispatch_specs.py).
        name = f"msg-w{m.group('wave')}-{m.group('letter')}.txt"
        if name not in specs:
            skipped["no spec"] += 1
            continue
        notes = specs[name]
        if notes is None:
            skipped["ambiguous spec"] += 1
            continue
        if not notes:
            skipped["spec names no note"] += 1
            continue
        s = STATUS.search(head)
        rows.append({"report": f.name, "notes": len(notes), "turns": int(t.group(1)),
                     "status": s.group(1) if s else "?"})
    return {"rows": rows, "skipped": skipped}


def fit(rows: list[dict]) -> dict:
    """Per group size: n, mean, median, max turns and the SPLITs; and the least-squares line turns = a + b x notes."""
    by: dict[int, list[dict]] = {}
    for r in rows:
        by.setdefault(r["notes"], []).append(r)
    sizes = {k: {"n": len(v), "mean": round(statistics.fmean(x["turns"] for x in v)),
                 "median": statistics.median(x["turns"] for x in v), "max": max(x["turns"] for x in v),
                 "split": sum(1 for x in v if x["status"] == "SPLIT")} for k, v in sorted(by.items())}
    line = None
    if len(by) >= 2:
        xs, ys = [r["notes"] for r in rows], [r["turns"] for r in rows]
        mx, my = statistics.fmean(xs), statistics.fmean(ys)
        b = sum((x - mx) * (y - my) for x, y in zip(xs, ys)) / sum((x - mx) ** 2 for x in xs)
        line = {"per_group": round(my - b * mx, 1), "per_note": round(b, 1)}
    return {"sizes": sizes, "line": line, "largest": max(by) if by else 0}


def render(m: dict, f: dict) -> str:
    out = [f"{len(m['rows'])} reports joined to their dispatch spec; left out: "
           + ", ".join(f"{k} {v}" for k, v in m["skipped"].items())]
    for k, s in f["sizes"].items():
        out.append(f"  {k} note(s): n={s['n']:<4} mean {s['mean']:>4}  median {s['median']:>5}  max {s['max']:>4}  "
                   f"SPLIT {s['split']}")
    if f["line"]:
        out.append(f"line: turns ~ {f['line']['per_group']} + {f['line']['per_note']} per note; largest group "
                   f"observed {f['largest']} notes")
    return "\n".join(out)


def self_test() -> int:
    from scratch_repo import scratch_dir  # noqa: PLC0415  (scripts/, on the path through coord)
    fails: list[str] = []

    def check(ok: bool, what: str) -> None:
        if not ok:
            fails.append(what)

    root = scratch_dir("turns-selftest-")
    (root / "scratch" / "reports").mkdir(parents=True)
    (root / "scratch" / "w9001").mkdir()
    (root / "scratch" / "op").mkdir()

    def spec(rel: str, notes: str) -> None:
        (root / "scratch" / rel).write_text(f"DISPATCH SPEC — wave 9001 (A), FIX: {notes}\nBrief: x PB9999\n",
                                            encoding="utf-8", newline="\n")

    def report(name: str, header: str) -> None:
        (root / "scratch" / "reports" / name).write_text(header + "\n## body naming PB9998\n", encoding="utf-8",
                                                         newline="\n")

    spec("w9001/msg-w9001-a.txt", "PB1001")
    spec("w9001/msg-w9001-b.txt", "PB1002, PB1003, PB1004")
    spec("op/msg-w9001-c.txt", "PB1005")
    spec("w9001/msg-w9001-c.txt", "PB1006")        # the same name, other notes: ambiguous
    spec("op/msg-w9001-e.txt", "PB1007, PB1008, PB1009")
    spec("w9001/msg-w9001-b2.txt", "PB1012, PB1013")              # a same-file successor's spec
    report("w9001a-PB1001-report.md", "DONE\n**Status:** DONE · **turns:** ~80 of 220 · **worktree:** x")
    report("w9001b-PB1002-report.md", "SPLIT\n**Status:** SPLIT · **turns:** ~120 of 220")
    report("w9001c-PB1005-report.md", "DONE\n**Status:** DONE · **turns:** ~50 of 220")
    report("w9001d-PB1010-report.md", "DONE\n**Status:** DONE · **turns:** ~50 of 220")   # no spec
    report("w9001e-PB1007-report.md", "DONE\nTurns used: about 100.")
    report("w9001f-PB1011-report.md", "DONE\nno figure here")
    spec("w9001/msg-w9001-g.txt", "PB1014")
    spec("w9001/msg-w9001-h.txt", "PB1015")
    report("w9001g-PB1014-report.md", "DONE\n**Status:** DONE · **turns:** about 60 of 220")      # the bold "about" form
    report("w9001h-PB1015-report.md", "DONE\n# implementer report · turns ~75 of 220 · x")        # the unbolded form
    report("clerk-w9001-leads-report.md", "DONE\n**turns:** ~10")                        # not named w*: never read
    report("w9001b2-PB1012-report.md", "DONE\n**Status:** DONE · **turns:** ~95 of 220")  # the successor of group b
    report("w9001a-train-report.md", "DONE\n**turns:** ~10")                             # not a group report: counted
    m = measure(root)
    got = {r["report"]: (r["notes"], r["turns"], r["status"]) for r in m["rows"]}
    check(got == {"w9001a-PB1001-report.md": (1, 80, "DONE"), "w9001b-PB1002-report.md": (3, 120, "SPLIT"),
                  "w9001e-PB1007-report.md": (3, 100, "DONE"), "w9001b2-PB1012-report.md": (2, 95, "DONE"),
                  "w9001g-PB1014-report.md": (1, 60, "DONE"), "w9001h-PB1015-report.md": (1, 75, "DONE")},
          f"the notes come from the spec's first line, the turns from the header, in every form: {got}")
    check(m["skipped"] == {"not a group report": 1, "no turns figure": 1, "no spec": 1, "ambiguous spec": 1,
                           "spec names no note": 0},
          f"every report left out is counted by its cause: {m['skipped']}")
    f = fit(m["rows"])
    check(f["sizes"][3] == {"n": 2, "mean": 110, "median": 110.0, "max": 120, "split": 1},
          f"per size n, mean, median, max and SPLITs: {f['sizes']}")
    check(f["largest"] == 3, f"the largest group observed is three notes: {f['largest']}")
    line = fit([{"notes": n, "turns": t, "status": "DONE"} for n, t in ((1, 80), (3, 120), (3, 100))])["line"]
    check(line == {"per_group": 65.0, "per_note": 15.0},
          f"the least-squares line over (1,80), (3,120), (3,100) is 65 + 15 per note: {line}")
    check(fit([{"notes": 2, "turns": 90, "status": "DONE"}])["line"] is None, "one group size fits no line")
    for x in fails:
        print(f"FAIL: {x}")
    print(f"=== TURNS_BY_GROUP_SIZE SELF-TEST: {'GREEN' if not fails else 'RED'} ===")
    return 1 if fails else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--coord", help="the coordination directory (default: coord.coord_path())")
    ap.add_argument("--json", action="store_true", help="print the rows, the counts and the fit as JSON")
    ap.add_argument("--self-test", action="store_true")
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    cdir = coord.coord_path(a.coord)
    m = measure(cdir)
    f = fit(m["rows"])
    print(json.dumps({**m, "fit": f}, indent=1) if a.json else render(m, f))
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
