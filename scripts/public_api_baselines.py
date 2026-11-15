#!/usr/bin/env python3
"""public_api_baselines.py — THE ONE MODULE OF THE PUBLIC-API BASELINES' RULES (kb/Work PB5396, PB3694;
docs/rearchitecture/DESIGN-orchestrator-loop.md section 16.9).

A public-API baseline is a `PublicAPI.<kind>.txt` file the Microsoft.CodeAnalysis.PublicApiAnalyzers read: `Shipped` holds
what a release published and `Unshipped` what the next one adds or removes (`*REMOVED*X`). Two rules are kept here, for
every caller (the landing's arm 0b in `landing_check.py`, and the gate audit `PUBLIC API BASELINES`):

  · NO SYMBOL IS LISTED TWICE. RS0025 fails the Release build on a symbol twice in one file or in two baselines of one
    directory (Shipped and Unshipped are one project's API), and plain git keeps a symbol twice when two branches add it
    at different places (kb/Work PB3694). `baseline_duplicates` is the rule, `baseline_stops` its landing check over a
    branch's tree.
  · AN UNSHIPPED BASELINE IS IN ONE ORDER: its non-blank lines in ordinal order (`#nullable enable`'s `#`, 0x23, and
    `*REMOVED*`'s `*`, 0x2A, sort before every symbol, so one ordinal sort of the file keeps the header first). Two branches that
    add different symbols then insert them at different places, which plain git merges (no merge driver exists since the
    owner's decision of 2026-11-10, kb/Work PB5110), two that add the same symbol make the same edit and merge clean, and the
    duplicate PB3694 met cannot be written by an ordered file. The order is THIS module's, not the analyzer's: whatever order
    the RS0016 code fix inserts a line in, `--check` is the authority and `--write` the fixer. `Shipped` keeps its order: it
    feeds the runtime ABI pin line by line (`scripts/abi_pin.py`) and changes only at a release.

    python scripts/public_api_baselines.py --check   # the tree's baselines: no duplicate, every Unshipped in order
    python scripts/public_api_baselines.py --write   # put every Unshipped baseline in order (the one fixer)
"""
from __future__ import annotations

import argparse
import pathlib
import re
import subprocess
import sys
from collections.abc import Callable
from typing import Any

import git_batch
from standard_streams import ensure_utf8

REPO = pathlib.Path(__file__).resolve().parents[1]
#: A public-API baseline (Microsoft.CodeAnalysis.PublicApiAnalyzers): `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt` or any
#: other `PublicAPI.<kind>.txt` anywhere under src/.
SYMBOL_BASELINE = re.compile(r"src/(?:[^/]+/)*PublicAPI\.[^/]*\.txt")
#: The file name of the baseline that is kept in order.
UNSHIPPED = "PublicAPI.Unshipped.txt"
VERDICT = "=== PUBLIC API BASELINES: "


def is_symbol_baseline(path: str) -> bool:
    """Whether `path` (repository-relative, `/`-separated) is a public-API baseline."""
    return SYMBOL_BASELINE.fullmatch(path) is not None


def is_unshipped(path: str) -> bool:
    """Whether `path` is a baseline kept in order."""
    return is_symbol_baseline(path) and path.rsplit("/", 1)[-1] == UNSHIPPED


def baseline_duplicates(files: dict[str, str]) -> list[str]:
    """The symbols the baselines `files` ({path: text}) list twice, which Microsoft.CodeAnalysis.PublicApiAnalyzers fails
    the build on as RS0025 (kb/Work PB3694): a symbol line twice in one file, or one plain line in two files of one directory
    (Shipped and Unshipped are one project's API). A `*REMOVED*X` line is a different line from the Shipped `X` it removes,
    so that pair is no finding. A line is compared without its CR; a blank line and a `#` header line are no symbol. Sorted,
    one finding per symbol and place, each naming the file(s)."""
    by_dir: dict[str, dict[str, set[str]]] = {}
    found: list[str] = []
    for path, text in sorted(files.items()):
        seen: set[str] = set()
        for line in (ln.rstrip(chr(13)) for ln in text.split(chr(10))):
            if not line or line.startswith("#"):
                continue
            if line in seen:
                found.append(f"{path}: {line} is listed twice")
            seen.add(line)
        directory = str(pathlib.PurePosixPath(path).parent)
        for line in seen:
            by_dir.setdefault(directory, {}).setdefault(line, set()).add(path)
    for owners in by_dir.values():
        for line, paths in owners.items():
            if len(paths) > 1:
                found.append(f"{' and '.join(sorted(paths))}: {line} is listed in both")
    return sorted(found)


def ordered(text: str) -> str:
    """`text` in the Unshipped order: its non-blank lines in ordinal order, each ended by one LF. Duplicates stay (they are
    `baseline_duplicates`' findings, never silently dropped by a fixer)."""
    lines = sorted(ln.rstrip(chr(13)) for ln in text.split(chr(10)) if ln.strip())
    return "".join(ln + chr(10) for ln in lines)


def order_problems(files: dict[str, str]) -> list[str]:
    """The Unshipped baselines of `files` ({path: text}) that are not in the Unshipped order, one finding per file naming the
    first line out of place (or the blank line, CR or missing final LF that makes the file differ from `ordered`)."""
    found: list[str] = []
    for path, text in sorted(files.items()):
        if not is_unshipped(path) or text == ordered(text):
            continue
        lines = text.split(chr(10))
        if lines and lines[-1] == "":
            lines.pop()
        where = next((f"line {i + 2} ({lines[i + 1]!r}) sorts before the line above it" for i in range(len(lines) - 1)
                      if lines[i + 1].rstrip(chr(13)) < lines[i].rstrip(chr(13)) and lines[i + 1].strip()),
                     "it holds a blank line, a CR or no final LF")
        found.append(f"{path}: not in ordinal order ({where}): run `python scripts/public_api_baselines.py --write`")
    return found


def baseline_texts(repo: pathlib.Path, git: Callable[..., Any], tree: str) -> dict[str, str]:
    """{path: text} of every public-API baseline (is_symbol_baseline) in `tree`, from one `git ls-tree -r`
    and ONE batched blob read (git_batch.blobs), however many baselines the tree holds."""
    listed = git("ls-tree", "-r", "-z", tree).stdout.split(chr(0))
    paths = {}   # {path: blob id}: an entry reads `<mode> blob <id>\t<path>`
    for entry in filter(None, listed):
        meta, _, path = entry.partition(chr(9))
        if is_symbol_baseline(path):
            paths[path] = meta.split()[2]
    blob = dict(git_batch.blobs(repo, paths.values()))
    return {path: blob[oid].decode("utf-8", "replace") for path, oid in paths.items()}


def baseline_stops(repo: pathlib.Path, rev: str, git: Callable[..., Any]) -> list[str]:
    """The public-API baselines of the landing's own tree that list a symbol twice, or in both PublicAPI.Shipped.txt and
    PublicAPI.Unshipped.txt (kb/Work PB3694): RS0025 fails the Release build on one. Plain git keeps a symbol both sides
    added at different places twice (kb/Work PB5110: no merge driver sorts the baselines as a set any more); this reads
    what the branch's head holds, which no local test of a branch ever read.

    What it does NOT read is GitHub's merge group's own merge. That merge runs no driver (PB3642) and keeps a symbol both
    sides added at different places twice, but it merges the pushed head with the main of the moment it is made. Every
    caller presents a head that already contains origin/main (enqueue.queue refuses any other, exit 2; the landing lane
    checks from the merge base, which the head contains), so a `git merge-tree` of the two is the head's own tree and
    reads nothing. Only a main that advances after the push makes the merge differ, and no check before the push can
    read a commit that does not exist yet: CI's merge-group run is the net for it (kb/Work PB3694, Residual)."""
    tree = git("rev-parse", "--verify", "-q", f"{rev}^{{tree}}").stdout.strip()
    dups = baseline_duplicates(baseline_texts(repo, git, tree)) if tree else []
    return ([f"the landing's tree lists a public-API symbol twice, which fails the Release build (RS0025): "
             f"{'; '.join(dups[:5])}{' …' if len(dups) > 5 else ''} — keep each symbol once in the baselines "
             f"(after `git merge origin/main`, delete the second line of each), commit, and re-gate"]
            if dups else [])


def working_texts(root: pathlib.Path) -> dict[str, str]:
    """{path: text} of every tracked public-API baseline in the working tree at `root`, bytes decoded as UTF-8 with no newline
    translation (a CR is a finding, so it must be read)."""
    listed = subprocess.run(["git", "-C", str(root), "ls-files", "-z", "--", "src"], capture_output=True, check=True)
    paths = [p for p in listed.stdout.decode("utf-8").split(chr(0)) if is_symbol_baseline(p)]
    return {p: (root / p).read_bytes().decode("utf-8") for p in paths}


def problems(files: dict[str, str]) -> list[str]:
    """Every finding of the baselines `files`: a symbol twice, an Unshipped baseline out of order."""
    return baseline_duplicates(files) + order_problems(files)


def check(root: pathlib.Path = REPO) -> tuple[int, str]:
    """(exit code, report) of `--check` over the tracked baselines at `root`."""
    files = working_texts(root)
    if not any(is_unshipped(p) for p in files):
        return 1, f"{VERDICT}RED — no tracked Unshipped baseline under src/: the reader is broken and the audit would pass on nothing ==="
    found = problems(files)
    if found:
        return 1, "\n".join(f"  {f}" for f in found) + f"\n{VERDICT}RED — {len(found)} finding(s) in {len(files)} baseline(s) ==="
    return 0, f"{VERDICT}GREEN — {len(files)} baseline(s), no symbol listed twice, every Unshipped one in ordinal order ==="


def write(root: pathlib.Path = REPO) -> list[str]:
    """Put every tracked Unshipped baseline at `root` in order; the paths it changed."""
    changed = []
    for path, text in sorted(working_texts(root).items()):
        if is_unshipped(path) and text != ordered(text):
            (root / path).write_bytes(ordered(text).encode("utf-8"))
            changed.append(path)
    return changed


def main(argv: list[str] | None = None) -> int:
    ensure_utf8()
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    mode = ap.add_mutually_exclusive_group(required=True)
    mode.add_argument("--check", action="store_true", help="the tree's baselines: no duplicate, every Unshipped in order")
    mode.add_argument("--write", action="store_true", help="put every Unshipped baseline in order")
    args = ap.parse_args(argv)
    if args.write:
        changed = write()
        print("\n".join(f"ordered {p}" for p in changed) if changed else "every Unshipped baseline is already in order")
        return 0
    rc, report = check()
    print(report)
    return rc


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
