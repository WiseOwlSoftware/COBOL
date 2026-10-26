#!/usr/bin/env python3
"""The ONE locked allocator for DEVLOG entry numbers (`devlog.py stamp` takes them), kb/Work `PB` ids, `COBOL####` diagnostic codes and wave numbers.

    python scripts/orchestrator/alloc.py devlog          # reserve the next DEVLOG entry number   -> 1866
    python scripts/orchestrator/alloc.py wave            # reserve the next wave number (plan_wave's and a hand dispatch's) -> 20261108
    python scripts/orchestrator/alloc.py pb 5            # reserve five kb/Work ids                -> PB1982-PB1986
    python scripts/orchestrator/alloc.py code 3          # reserve three diagnostic codes          -> COBOL2799-COBOL2801
    python scripts/orchestrator/alloc.py peek code       # the next free value, reserving nothing
    python scripts/orchestrator/alloc.py peek code --probe   # the session-probe's "diag" line
    python scripts/orchestrator/alloc.py seed code 2798  # record values handed out before the allocator (never lowers)
    ... [--coord DIR] [--repo DIR]

Design: docs/rearchitecture/DESIGN-orchestrator-loop.md section 6. The next value of a kind is
max(truth, reserved) + 1. TRUTH is read from the repository on every call, by the one rule per kind below
(`session-probe.ps1` calls `peek code --probe` instead of computing it a second time). RESERVED is the highest value
handed out so far, kept in `<coord>/alloc.json` OUTSIDE every worktree, so an id given to an in-flight implementer
whose note has not landed yet is never handed out again (five collisions in one day cost a renumbering pass,
workstream SKILL section 4). A reservation is never returned: an unused id is a harmless gap.

The lock is `coord.locked(cdir, "alloc.lock")`, the coordination directory's one short mutex (an atomic create,
broken when stale; coord.py says how).
"""
from __future__ import annotations

import argparse
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import coord  # noqa: E402

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
import devlog  # noqa: E402  (the one reader of DEVLOG/)
from standard_streams import ensure_utf8  # noqa: E402

LOCK = "alloc.lock"
KINDS = ("devlog", "pb", "code", "wave")
PB_FILE = re.compile(r"^PB(\d+)\.md$")
CODE = re.compile(r"\bCOBOL(\d{4})\b")
CATALOG_CODE = re.compile(r'"COBOL(\d{4})"')
CATALOG = pathlib.Path("src/WiseOwl.Cobol.Editions/Diagnostics/DiagnosticCatalog.cs")


# ── truth: one rule per kind ─────────────────────────────────────────────────────────────────────────────────────
def devlog_top(repo: pathlib.Path) -> int:
    """The highest entry number in the repository's DEVLOG/ (one file per entry, kb/Work PB3025: devlog.py's rule)."""
    top = devlog.highest(devlog.names_in(repo))
    if not top:
        raise SystemExit(f"⛔ no entry file in {repo / devlog.DIR}")
    return top


def pb_max(repo: pathlib.Path) -> int:
    ids = [int(m.group(1)) for p in (repo / "kb" / "Work").glob("PB*.md") if (m := PB_FILE.match(p.name))]
    return max(ids, default=0)


def code_scans(repo: pathlib.Path) -> tuple[int, int]:
    """(highest COBOL#### code anywhere in src/**/*.cs outside bin/obj, highest quoted code in the catalog).

    The next free code is the CEILING of BOTH (the COBOL1573/1518 collision lesson). No band is pinned: a
    band in this pattern capped out twice (at 1600 and at 2000) and reported a code the catalog already held.
    The compiler channel's raw codes are not catalog descriptors, so src >= catalog is the steady state; a
    catalog maximum ABOVE every emitted code is an orphan descriptor worth reconciling before allocating.
    """
    src_max = 0
    for p in (repo / "src").rglob("*.cs"):
        if {"bin", "obj"} & set(p.relative_to(repo).parts):
            continue
        for m in CODE.finditer(p.read_text(encoding="utf-8", errors="replace")):
            src_max = max(src_max, int(m.group(1)))
    cat = (repo / CATALOG).read_text(encoding="utf-8", errors="replace")
    cat_max = max((int(m.group(1)) for m in CATALOG_CODE.finditer(cat)), default=0)
    return src_max, cat_max


#: How much of the newest DEVLOG the wave and train numbering reads (one file per entry, kb/Work PB3025): the newest
#: RECENT_ENTRIES entries, and of those the first RECENT_LINES lines for wave and train labels.
RECENT_ENTRIES = 20
RECENT_LINES = 200
WAVE_LABEL = re.compile(r"\b(?:[Ww]aves?|[Tt]rains?) (\d{3,})")


def wave_top(repo: pathlib.Path, cdir: pathlib.Path) -> int:
    """The highest wave OR train number in use: in a report's name in the reports directory (a hand dispatch's too), in
    the dispatch ledger, or in the newest RECENT_LINES lines of the DEVLOG's newest entries (0 when none). A wave's first
    train is labelled with the wave number, so a train label counts as taken too (kb/Work PB2562: trains 1035 and 1036
    came from waves 1031 and 1032, the planner proposed wave 1035, and "Train 1034" had already landed twice). A wave
    planned but with no report yet (another session's, awaiting the operator's GO) is taken as well, or the next wave
    reuses its number and overwrites its specs (kb/Work PB2806). A number HANDED OUT but not yet written anywhere is the
    reservation's (`alloc.json`, `peek`/`allocate`), which is how a hand dispatch's wave stays taken until its report
    exists (kb/Work PB3700)."""
    seen = [int(m.group("wave")) for p in coord.reports_dir(str(cdir)).glob("w*-PB*-report.md")
            if (m := coord.REPORT_NAME.match(p.name))]
    seen += [int(g["wave"]) for g in coord.ledger_groups(cdir)]
    for line in "".join(devlog.newest(repo, RECENT_ENTRIES)).split("\n")[:RECENT_LINES]:
        seen += [int(w) for w in WAVE_LABEL.findall(line)]
    return max(seen, default=0)


def truth(kind: str, repo: pathlib.Path, cdir: pathlib.Path) -> int:
    if kind == "devlog":
        return devlog_top(repo)
    if kind == "pb":
        return pb_max(repo)
    if kind == "wave":
        return wave_top(repo, cdir)
    return max(code_scans(repo))


def render(kind: str, first: int, count: int) -> str:
    last = first + count - 1
    if kind in ("devlog", "wave"):
        return str(first) if count == 1 else f"{first}-{last}"
    if kind == "pb":
        return f"PB{first}" if count == 1 else f"PB{first}-PB{last}"
    return f"COBOL{first:04d}" if count == 1 else f"COBOL{first:04d}-COBOL{last:04d}"


# ── allocation ───────────────────────────────────────────────────────────────────────────────────────────────────
def allocate(kind: str, count: int, repo: pathlib.Path, cdir: pathlib.Path) -> tuple[int, int]:
    """Reserve `count` consecutive values of `kind`; returns (first, last)."""
    if kind not in KINDS or count < 1:
        raise ValueError(f"bad allocation {kind!r} x{count}")
    t = truth(kind, repo, cdir)  # read outside the lock: it is the slow part, and truth only grows
    with coord.locked(cdir, LOCK):
        path = cdir / "alloc.json"
        state = coord.read_json(path, {})
        first = max(t, int(state.get(kind, 0))) + 1
        state[kind] = first + count - 1
        coord.write_json(path, state)
    return first, first + count - 1


def seed(kind: str, value: int, cdir: pathlib.Path) -> int:
    """Raise the reserved high-water mark to `value` (never lowers it): records ids handed out before the allocator
    existed, such as an in-flight wave's code ranges. Returns the mark now in force."""
    with coord.locked(cdir, LOCK):
        path = cdir / "alloc.json"
        state = coord.read_json(path, {})
        state[kind] = max(int(state.get(kind, 0)), value)
        coord.write_json(path, state)
        return state[kind]


def peek(kind: str, repo: pathlib.Path, cdir: pathlib.Path) -> int:
    state = coord.read_json(cdir / "alloc.json", {})
    return max(truth(kind, repo, cdir), int(state.get(kind, 0))) + 1


def probe_line(repo: pathlib.Path, cdir: pathlib.Path) -> str:
    src_max, cat_max = code_scans(repo)
    reserved = int(coord.read_json(cdir / "alloc.json", {}).get("code", 0))
    nxt = max(src_max, cat_max, reserved) + 1
    code = lambda n: render("code", n, 1)  # noqa: E731
    res = f" · reserved max {code(reserved)}" if reserved else ""
    head = f"src-grep max {code(src_max)} · catalog max {code(cat_max)}{res}"
    if cat_max > src_max:
        return (f"{head} ⚠ CATALOG ABOVE SRC (orphan descriptor {code(cat_max)} never emitted) — reconcile before "
                f"allocating; next free = {code(nxt)}")
    return f"{head} → next free = {code(nxt)}"


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("kind", choices=(*KINDS, "peek", "seed"))
    ap.add_argument("arg", nargs="?", help="count for pb/code; the kind for peek and seed")
    ap.add_argument("value", nargs="?", type=int, help="seed only: the highest value already handed out")
    ap.add_argument("--probe", action="store_true", help="with 'peek code': print the session-probe line")
    ap.add_argument("--coord", help=f"coordination directory (default ${coord.ENV} or {coord.DEFAULT})")
    ap.add_argument("--repo", default=str(coord.REPO), help="repository whose files are the truth (default: this one)")
    a = ap.parse_args(argv)
    repo, cdir = pathlib.Path(a.repo), coord.coord_dir(a.coord)
    if a.kind == "seed":
        if a.arg not in KINDS or a.value is None or a.value < 1:
            ap.error(f"seed needs one of {KINDS} and a positive value")
        print(f"{a.arg} reserved through {render(a.arg, seed(a.arg, a.value, cdir), 1)}")
        return 0
    if a.kind == "peek":
        if a.arg not in KINDS:
            ap.error(f"peek needs one of {KINDS}")
        if a.probe:
            if a.arg != "code":
                ap.error("--probe applies to 'peek code' only")
            print(probe_line(repo, cdir))
        else:
            print(render(a.arg, peek(a.arg, repo, cdir), 1))
        return 0
    if a.kind in ("devlog", "wave"):
        count = int(a.arg or 1)
    else:
        if not a.arg or not a.arg.isdigit() or int(a.arg) < 1:
            ap.error(f"{a.kind} needs a positive count")
        count = int(a.arg)
    first, _ = allocate(a.kind, count, repo, cdir)
    print(render(a.kind, first, count))
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
