#!/usr/bin/env python3
"""case_collisions.py — NO TWO TRACKED PATHS DIFFER ONLY IN LETTER CASE (kb/Work PB5694).

    python scripts/case_collisions.py --check        # exit 1 naming every colliding pair (the gate's audit, CI's too)

⛔ WHY (kb/Work PB5694). Two landings of 2026-11-15 each created a conformance case directory for §2002:8.4.2.2 with
its own casing, `8.4.2.2@2002-FUNCTION-IDENTIFIER` and `8.4.2.2@2002-Function-identifier`. Git stores both. A
case-insensitive checkout (Windows, macOS) folds them into one directory, and a case-sensitive one (Linux, CI) keeps
two. A test-ref, a generator path or a `Pinned by` cell spelled one way then names a different set of files on each
operating system, and no test fails on either. A colliding pair is a fact about the committed tree, so it gets a check
over the whole committed tree, here, the way conflict_markers.py checks for markers. It is an audit and not a test,
for kb/Work PB2878's reason: a test that reads every tracked path would make every path one that a test reads.

THE RULE. Each tracked path (`git ls-files`, the index, where both spellings can sit together even on a checkout
that holds only one), and each directory above it, is a NAME. Two different names collide when `fold` maps them to
the same text. The audit reports a collision only at the shortest prefix where it starts: two directories that differ
in case are ONE finding, not one per file under them. `fold` upper-cases one character at a time and keeps a
character whose upper case is more than one character (`ß`, `ŉ`). That is the simple one-to-one case mapping that
case-insensitive file systems apply (NTFS's upcase table, APFS's case-insensitive form), so `ß` and `SS` are two names
there and two names here. There is no allow-list, because no colliding pair is ever correct.

A broken enumeration is RED and never a clean tree: an enumeration that finds fewer than MIN_TRACKED tracked paths is
the wrong directory or no git, and a missing observation is not a negative one.
"""
from __future__ import annotations

import argparse
import subprocess
import sys
from collections import defaultdict
from collections.abc import Iterable
from pathlib import Path

from standard_streams import ensure_utf8

REPO = Path(__file__).resolve().parents[1]
#: An enumeration of fewer tracked paths than this is a broken enumeration (no git, a wrong directory), never a clean tree.
MIN_TRACKED = 1000
VERDICT = "=== CASE COLLISIONS: "
SHOWN = 50


def fold(name: str) -> str:
    """`name` as a case-insensitive file system compares it: each character upper-cased by its simple (one-character)
    mapping, and a character whose upper case is several characters (`ß` -> `SS`) left as it is."""
    return "".join(u if len(u := c.upper()) == 1 else c for c in name)


def collisions(paths: Iterable[str]) -> list[list[str]]:
    """Every group of two or more names (tracked paths and the directories above them, `/`-separated) that `fold` maps
    to one text, at the shortest prefix where the names first differ only in case. Each group is sorted, and the groups
    are sorted by their first name."""
    spellings: dict[str, set[str]] = defaultdict(set)
    for path in paths:
        parts = path.split("/")
        for i in range(1, len(parts) + 1):
            name = "/".join(parts[:i])
            spellings[fold(name)].add(name)
    groups = []
    for key, names in spellings.items():
        if len(names) < 2:
            continue
        parent = key.rpartition("/")[0]
        if parent and len(spellings[parent]) > 1:
            continue   # the collision starts above this name and is reported there
        groups.append(sorted(names))
    return sorted(groups)


def tracked(root: Path) -> list[str]:
    out = subprocess.run(["git", "-C", str(root), "ls-files", "-z"], capture_output=True, check=True).stdout
    return [p for p in out.decode("utf-8").split("\0") if p]


def check(root: Path = REPO, minimum: int = MIN_TRACKED) -> tuple[int, str]:
    """(exit code, report) of `--check` over the tracked paths at `root`."""
    try:
        paths = tracked(root)
    except (subprocess.CalledProcessError, OSError) as e:
        return 1, f"{VERDICT}RED — `git ls-files` failed ({e}), so nothing was compared ==="
    if len(paths) < minimum:
        return 1, (f"{VERDICT}RED — `git ls-files` reported only {len(paths)} tracked path(s) under {root}: the enumeration "
                   "is broken, so the audit would compare nothing ===")
    found = collisions(paths)
    if found:
        lines = [f"    {' | '.join(group)}" for group in found[:SHOWN]]
        return 1, "\n".join(lines + [
            f"{VERDICT}RED — {len(found)} set(s) of tracked names differ only in letter case, of {len(paths)} tracked "
            "path(s): a case-insensitive checkout folds each set into one name and Linux keeps them apart. Keep one "
            "spelling (`git mv` through a temporary name, so git records the rename on a case-insensitive file system) "
            "and change every reference to the other ==="])
    return 0, f"{VERDICT}GREEN — {len(paths)} tracked path(s), no two names differ only in letter case ==="


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--check", action="store_true", required=True)
    ap.add_argument("--root", type=Path, default=REPO, help=argparse.SUPPRESS)
    args = ap.parse_args(argv)
    rc, report = check(args.root)
    print(report)
    return rc


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
