#!/usr/bin/env python3
"""THE LANDING CHECK OF THE CODE READS (kb/Work PB6056; DESIGN-spec-conformance-review.md section 10.6.1;
DESIGN-test-build-ci.md section 3.17.2): a landing may not move the code a stored code read of a COMPLETE edition names.

    python scripts/spec/code_read_landing.py [--rev REV] [--base BASE] [--root DIR]

`enqueue.py checks` runs it from the MERGED head's own tree (like `work.py check`), with `--rev <head> --base origin/main`.
Exit 0 passes; exit 1 refuses, naming the rows and the remedy; any other exit is "could not run", never a pass.

WHY (kb/Work PB6053, PB6056). A code read is evidence at its pin, and a read whose code moved is owed a RE-READ
(`code_reads.moved_since`, kb/Work PB5863): the gate's CODE READS audit prints it as information and is never red for it,
because the tree moves and a read is not wrong for being old. But `edition_status.py` counts the same row as a term of its
CODE READS part, so a landing that deletes or renames a member a read names turns an edition that read COMPLETE on main
NOT COMPLETE with every check green, and nothing told the lane that landed it (PB2635, 5 rows; PR #1946, 2 rows, on
2026-11-22). The audit audits one tree and cannot know what main reads; the landing knows both trees, so the rule is here.

THE RULE. The change from `--base` (origin/main) to `--rev` (the merged head) is refused when it moves a member a stored
read of an older edition names (`code_reads.moved_by`: an entry that resolves in the base and no longer resolves at the
head), unless that edition does not read COMPLETE on main (`edition_status.py` at the base: an edition main already holds
open is not re-opened by a change). A change that carries the re-read (`code_reads.py import --replace`, whose record names
members that resolve at the head) passes; so does a member moved that no read names. The refusal's remedy is the re-read
of the named rows on the branch (`code_reads.py plan --edition YEAR`, a reader and a refuter, `import --replace`).

THE SWEEP OF `edition_status.py`'s OTHER TERMS (kb/Work PB6056) is in the note: the inventory GAP, the open notes and the
catalog prose holes are changes a landing makes deliberately (a verdict, a filed note, an edited transcription), visible in
its diff; the moved code is the one term an unrelated landing turns without touching any of them, so it is the one refused.

"COMPLETE on main" is `edition_status.py`'s own verdict line, run by the base's own copy in a detached worktree of the base,
made only when a change moved a read's code (the common landing moves none and pays for the resolution only).
"""
from __future__ import annotations

import argparse
import contextlib
import json
import os
import pathlib
import subprocess
import sys
import tempfile
from typing import Any, Callable, Iterable, Iterator

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
import code_reads  # noqa: E402  (the predicate `moved_by`, the contexts and records of an edition's reads)
import inventory_schema  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402

TOOL = "code_read_landing"
#: (year, base ref, repository root) -> does `edition_status.py --edition year` read COMPLETE at the base? A self-test plants one.
CompleteOnMain = Callable[[int, str, pathlib.Path], bool]


#: (the repository root) -> each older held edition's context with the records its store holds; a self-test plants others.
Reads = Callable[[pathlib.Path], Iterable[tuple[code_reads.Context, dict[str, dict[str, Any]]]]]


class NotRun(Exception):
    """The check could not read what it decides on: never a pass (the caller exits 2)."""


@contextlib.contextmanager
def base_worktree(root: pathlib.Path, base: str) -> Iterator[pathlib.Path]:
    """A detached worktree of `base` in a new temporary directory outside every git tree, removed with `git worktree remove`
    (never a recursive delete of a path built here) and its empty parent directory."""
    parent = pathlib.Path(tempfile.mkdtemp(prefix="code-read-landing-"))
    wt = parent / "main"
    add = subprocess.run(["git", "-C", str(root), "worktree", "add", "--detach", "--quiet", str(wt), base],
                         capture_output=True, text=True)
    if add.returncode:
        os.rmdir(parent)
        raise NotRun(f"git worktree add {base} failed: {add.stderr.strip()}")
    try:
        yield wt
    finally:
        rm = subprocess.run(["git", "-C", str(root), "worktree", "remove", "--force", str(wt)],
                            capture_output=True, text=True)
        if rm.returncode:
            print(f"{TOOL}: warning: the temporary worktree {wt} was not removed: {rm.stderr.strip()}", file=sys.stderr)
        else:
            os.rmdir(parent)


def complete_on_main(year: int, base: str, root: pathlib.Path) -> bool:
    """Does `edition_status.py --edition YEAR`, run by the base's own copy, read COMPLETE at `base`? Anything but its exit 0
    (COMPLETE) or exit 1 (NOT COMPLETE) with a parseable document is NotRun: a status that could not be read is never read as
    "not complete", which would let the change through."""
    with base_worktree(root, base) as wt:
        script = wt / "scripts" / "spec" / "edition_status.py"
        if not script.is_file():
            raise NotRun(f"{base} has no scripts/spec/edition_status.py: main's status of {year} cannot be read")
        run = subprocess.run([sys.executable, str(script), "--edition", str(year), "--json"], cwd=wt,
                             capture_output=True, text=True, encoding="utf-8")
        try:
            doc = json.loads(run.stdout)
            complete = bool(doc["complete"])
        except (ValueError, KeyError, TypeError):
            raise NotRun(f"edition_status.py --edition {year} at {base} printed no verdict (exit {run.returncode}): "
                         f"{(run.stderr or run.stdout).strip()[:300]}") from None
        if run.returncode != (0 if complete else 1):
            raise NotRun(f"edition_status.py --edition {year} at {base} exited {run.returncode} against its own verdict")
        return complete


def stored_reads(root: pathlib.Path) -> Iterator[tuple[code_reads.Context, dict[str, dict[str, Any]]]]:
    """Every older held edition that has a store, with the records it holds (`code_reads.older_years`, `load`)."""
    schema = inventory_schema.load_schema()
    for year in code_reads.older_years(root):
        ctx = code_reads.Context.of(year, root, schema)
        yield ctx, code_reads.load(ctx)


def refusal(year: int, rows: list[code_reads.ReRead]) -> list[str]:
    """The refusal's lines for one edition: each row with the entries the change moved, and the remedy."""
    out = [f"⛔ {TOOL}: edition {year} reads COMPLETE on main, and this change moves the code {len(rows)} stored code read(s) "
           "of it name, so it would read NOT COMPLETE once landed:"]
    out += [f"    {r.rule_id}: " + "; ".join(r.moved) for r in rows]
    out.append(f"  REMEDY: re-read those rows on this branch (`python scripts/spec/code_reads.py plan --edition {year}` lists "
               "them; a reader and a refuter read the moved code, then `code_reads.py import --replace`), and re-gate; a "
               "branch that carries the re-read passes (kb/Work PB6056).")
    return out


def check(root: pathlib.Path, rev: str, base: str, complete: CompleteOnMain = complete_on_main,
          say: Callable[[str], None] = print, reads: Reads = stored_reads) -> int:
    """THE CHECK: 0 passes, 1 refuses. Raises NotRun when it cannot read a tree or a status."""
    moved: dict[int, list[code_reads.ReRead]] = {}
    with code_reads.pins_open(root) as pins:
        for ref in (rev, base):
            if not pins.is_commit(ref):
                raise NotRun(f"{ref} is no commit of the repository at {root}")
        head, before = pins.at(rev), pins.at(base)
        for ctx, records in reads(root):
            if rows := code_reads.moved_by(ctx, records, head, before):
                moved[ctx.ed.year] = rows
    refused = 0
    for year, rows in moved.items():
        if complete(year, base, root):
            refused += 1
            for line in refusal(year, rows):
                say(line)
        else:
            say(f"{TOOL}: this change moves the code of {len(rows)} stored code read(s) of edition {year}, which does not "
                "read COMPLETE on main: not refused (kb/Work PB6056)")
    if not moved:
        say(f"{TOOL}: no stored code read names code this change moves")
    return 1 if refused else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="Refuse a landing that moves the code a COMPLETE edition's code reads name "
                                             "(kb/Work PB6056).")
    ap.add_argument("--rev", default="HEAD", help="the merged head (the tree the reads are judged at)")
    ap.add_argument("--base", default="origin/main", help="main, what the change is measured from")
    ap.add_argument("--root", type=pathlib.Path, default=code_reads.REPO)
    a = ap.parse_args(argv)
    try:
        return check(a.root, a.rev, a.base)
    except NotRun as e:
        print(f"⛔ {TOOL}: NOT RUN: {e}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
