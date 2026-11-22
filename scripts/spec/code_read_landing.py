#!/usr/bin/env python3
"""THE LANDING CHECK OF THE CODE READS (kb/Work PB6056; DESIGN-spec-conformance-review.md section 10.6.1;
DESIGN-test-build-ci.md section 3.17.2): a landing may not move the code a stored code read names.

    python scripts/spec/code_read_landing.py [--rev REV] [--base BASE] [--root DIR]

`enqueue.py checks` runs it from the MERGED head's own tree (like `work.py check`), with `--rev <head> --base origin/main`.
Exit 0 passes; exit 1 refuses, naming the rows and the remedy; any other exit is "could not run", never a pass.

WHY (kb/Work PB6053, PB6056). A code read is evidence at its pin, and a read whose code moved is owed a RE-READ
(`code_reads.moved_since`, kb/Work PB5863): the gate's CODE READS audit prints it as information and is never red for it,
because the tree moves and a read is not wrong for being old. But `edition_status.py` counts the same row as a term of its
CODE READS part, so a landing that deletes or renames a member a read names re-opens the edition with every check green,
and nothing told the lane that landed it (PB2635, 5 rows; PR #1946, 2 rows, on 2026-11-22). The audit audits one tree and
cannot know what main reads; the landing knows both trees, so the rule is here.

THE RULE. The change from `--base` (origin/main) to `--rev` (the merged head) is refused when it moves a member a stored
read of an older edition names (`code_reads.moved_by`: an entry that resolves in the base and no longer resolves at the
head), WHATEVER that edition's status on main (kb/Work PB6056, decision 2026-11-22: a pass for an edition that does not
read COMPLETE on main let un-resolved pins accumulate silently while the edition was open for unrelated reasons). A change
that carries the re-read (`code_reads.py import --replace`, whose record names members that resolve at the head) passes;
so does a member moved that no read names. The refusal's remedy is the re-read of the named rows on the branch
(`code_reads.py plan --edition YEAR`, a reader and a refuter, `import --replace`).

ONE REVISION FOR BOTH (kb/Work PB6056 branch review R1-2). The store, the rows and the schema the reads are judged with are
read as committed at `--rev`, the same revision `moved_by` resolves the locations in, never from a working tree: an
uncommitted edit of the store cannot change the verdict.

THE SWEEP OF `edition_status.py`'s OTHER TERMS (kb/Work PB6056) is in the note: the inventory GAP, the open notes and the
catalog prose holes are changes a landing makes deliberately (a verdict, a filed note, an edited transcription), visible in
its diff; the moved code is the one term an unrelated landing turns without touching any of them, so it is the one refused.
"""
from __future__ import annotations

import argparse
import pathlib
import sys
from typing import Any, Callable, Iterable, Iterator

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
import code_read_store  # noqa: E402  (the store as committed at a revision: `load_at`)
import code_reads  # noqa: E402  (the predicate `moved_by`, the contexts and records of an edition's reads)
import inventory_schema  # noqa: E402
import inventory_view  # noqa: E402  (an edition's rows as committed at a revision: `inventory_at`)
from standard_streams import ensure_utf8  # noqa: E402

TOOL = "code_read_landing"

Records = dict[str, dict[str, Any]]
#: (the repository root, the revision judged) -> each older held edition's context with the records its store holds AT that
#: revision; a self-test plants others.
Reads = Callable[[pathlib.Path, str], Iterable[tuple[code_reads.Context, Records]]]
#: (year, the repository root, the revision) -> the edition's context as committed at the revision; a self-test plants one.
ContextAt = Callable[[int, pathlib.Path, str], code_reads.Context]


class NotRun(Exception):
    """The check could not read what it decides on: never a pass (the caller exits 2)."""


def context_at(year: int, root: pathlib.Path, rev: str) -> code_reads.Context:
    """`year`'s context as committed at `rev`: that commit's schema, its rows and the next held edition's rows."""
    schema = inventory_schema.load_schema(inventory_schema.Tree(root, rev))
    rows = inventory_view.inventory_at(rev, year, root)
    targets = inventory_view.inventory_at(rev, schema.edition(year).next_held, root)
    if rows is None or targets is None:
        raise NotRun(f"{rev} holds no inventory of {year} or of the edition after it: its code reads cannot be judged")
    return code_reads.Context.of(year, root, schema, rows, targets)


def stored_reads(root: pathlib.Path, rev: str, schema: inventory_schema.Schema | None = None,
                 context: ContextAt = context_at) -> Iterator[tuple[code_reads.Context, Records]]:
    """Every older held edition whose store holds a record at `rev`, with its context and those records, all as committed
    at `rev` (`code_read_store.load_at`; never the working tree, R1-2). `schema` defaults to the commit's own."""
    tree = inventory_schema.Tree(root, rev)
    schema = schema or inventory_schema.load_schema(tree)
    for ed in schema.all_editions:
        if ed.held and not ed.newest and (records := code_read_store.load_at(ed.year, tree)):
            yield context(ed.year, root, rev), records


def refusal(year: int, rows: list[code_reads.ReRead]) -> list[str]:
    """The refusal's lines for one edition: each row with the entries the change moved, and the remedy."""
    out = [f"⛔ {TOOL}: this change moves the code {len(rows)} stored code read(s) of edition {year} name, so each would be "
           "owed a re-read once landed (edition_status.py counts it), whatever the edition's status on main:"]
    out += [f"    {r.rule_id}: " + "; ".join(r.moved) for r in rows]
    out.append(f"  REMEDY: re-read those rows on this branch (`python scripts/spec/code_reads.py plan --edition {year}` lists "
               "them; a reader and a refuter read the moved code, then `code_reads.py import --replace`), and re-gate; a "
               "branch that carries the re-read passes (kb/Work PB6056).")
    return out


def check(root: pathlib.Path, rev: str, base: str, say: Callable[[str], None] = print,
          reads: Reads = stored_reads) -> int:
    """THE CHECK: 0 passes, 1 refuses. Raises NotRun when it cannot read a tree."""
    moved: dict[int, list[code_reads.ReRead]] = {}
    with code_reads.pins_open(root) as pins:
        for ref in (rev, base):
            if not pins.is_commit(ref):
                raise NotRun(f"{ref} is no commit of the repository at {root}")
        head, before = pins.at(rev), pins.at(base)
        for ctx, records in reads(root, rev):
            if rows := code_reads.moved_by(ctx, records, head, before):
                moved[ctx.ed.year] = rows
    for year, rows in moved.items():
        for line in refusal(year, rows):
            say(line)
    if not moved:
        say(f"{TOOL}: no stored code read names code this change moves")
    return 1 if moved else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="Refuse a landing that moves the code a stored code read names "
                                             "(kb/Work PB6056).")
    ap.add_argument("--rev", default="HEAD", help="the merged head (the tree the reads and their store are judged at)")
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
