#!/usr/bin/env python3
"""conflict_markers.py — NO TRACKED FILE CARRIES AN UNRESOLVED MERGE CONFLICT (kb/Work PB735, PB2878).

    python scripts/conflict_markers.py --check        # exit 1 naming every marker line (the gate's audit, CI's too)
    python scripts/conflict_markers.py --self-test

⛔ WHY (kb/Work PB735): commit c6460d0f staged `scripts/spec/audit_grammar_optional_words.py` with four conflicted regions
still in it; the file stopped parsing, so the audit it implements was dead, and every gate stayed green because no test
imported that script. The marker is a BYTE-LEVEL fact about the committed tree, so it gets a byte-level check over the
whole committed tree. The checkpoint protocol's `git diff --cached --check` stops most of them before a commit; this
check is the layer that does not depend on anyone remembering to run it.

⭐ AN AUDIT, NOT A TEST (kb/Work PB2878). This rule was a Unit drift test (ConflictMarkerDriftTests) until the inert lane:
a test that reads EVERY tracked file makes every path one a test reads, so no change could ever skip the test legs.
As an audit it runs where every audit runs — the local gate's audit phase before the slot, CI's `audits` job on EVERY
event (a pull request, a merge group, a push; a docs-only change included, where the test never ran in CI because the
matrix was skipped), the battery's phase -1 — and it reads the tree without a build.

THE RULE, as git writes a conflict: a marker is a run of exactly seven identical characters at the start of a line —
an opener (seven `<`) or a closer (seven `>`) followed by a space or the end of the line, the diff3 ancestor divider
(seven `|`) followed by a space or the end of the line, or the bare divider (seven `=`) followed by the end of the line
and nothing else. THE COMPANY RULE: an opener or a closer is self-evidencing, but `=======` is a legal Markdown setext
underline and `|||||||` a legal (if odd) table row, and git never writes either without its brackets, so a divider
counts only in a file that also holds an opener or a closer. A line ending is LF or CRLF (the CR settles a marker just
as the LF does: two arms, one rule). A file whose first 8,192 bytes hold a NUL is binary (git's heuristic) and is not
scanned. Only TRACKED files are read (`git ls-files`), so the GnuCOBOL corpus, which carries marker-shaped lines as test
data and is gitignored, is excluded structurally, with no list to maintain. A tracked path that is absent from the
checkout (a gitlink, a sparse checkout) is counted, not read; a file that exists but cannot be opened is a RED, never a
skip. The check reads the tree it can see: an enumeration that finds fewer than MIN_TRACKED tracked paths is a RED (a
missing observation is not a negative one).
"""
from __future__ import annotations

import argparse
import os
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
RUN = 7
PROBE_BYTES = 8192
#: An enumeration of fewer tracked paths than this is a broken enumeration (no git, a wrong directory), never a clean tree.
MIN_TRACKED = 1000
#: The four marker characters, BUILT (never written as a run), so this file is not itself an offender.
OURS, BASE, SPLIT, THEIRS = "<" * RUN, "|" * RUN, "=" * RUN, ">" * RUN
SHOWN = 50


@dataclass
class Sweep:
    offenders: list[tuple[str, int, str]] = field(default_factory=list)  # (path, line, marker)
    scanned: int = 0
    binary: int = 0
    missing: int = 0
    unreadable: list[str] = field(default_factory=list)


def markers(data: bytes) -> list[tuple[int, str]]:
    """Every marker line of `data` as (1-based line, marker), before the company rule; [] for nothing."""
    hits = []
    for number, raw in enumerate(data.split(b"\n"), 1):
        line = raw[:-1] if raw.endswith(b"\r") else raw
        if len(line) < RUN or line[:1] not in (b"<", b"|", b"=", b">"):
            continue
        c = line[:1]
        if line[:RUN] != c * RUN:
            continue
        rest = line[RUN:]
        if rest == b"" or (c != b"=" and rest[:1] == b" "):
            hits.append((number, (c * RUN).decode("ascii")))
    return hits


def scan(root: Path, paths: list[str]) -> Sweep:
    """The sweep of `paths` (repo-relative) under `root`."""
    s = Sweep()
    for rel in paths:
        full = root / rel
        if not full.is_file():
            s.missing += 1
            continue
        try:
            data = full.read_bytes()
        except OSError as e:
            if not full.exists():
                s.missing += 1
                continue
            s.unreadable.append(f"{rel}: {type(e).__name__}: {e}")
            continue
        if b"\0" in data[:PROBE_BYTES]:
            s.binary += 1
            continue
        s.scanned += 1
        found = markers(data)
        bracketed = any(m in (OURS, THEIRS) for _, m in found)
        s.offenders += [(rel, n, m) for n, m in found if bracketed or m in (OURS, THEIRS)]
    return s


def tracked(root: Path) -> list[str]:
    out = subprocess.run(["git", "-C", str(root), "ls-files", "-z"], capture_output=True, check=True).stdout
    return [p for p in out.decode("utf-8").split("\0") if p]


def check(root: Path) -> int:
    try:
        paths = tracked(root)
    except (subprocess.CalledProcessError, OSError) as e:
        print(f"=== CONFLICT MARKERS: RED — `git ls-files` failed ({e}), so nothing was swept ===")
        return 1
    if len(paths) < MIN_TRACKED:
        print(f"=== CONFLICT MARKERS: RED — `git ls-files` reported only {len(paths)} tracked path(s) under {root}: "
              "the enumeration is broken, so the sweep would scan nothing ===")
        return 1
    s = scan(root, paths)
    for rel, n, m in s.offenders[:SHOWN]:
        print(f"    {rel}:{n}  {m}")
    for u in s.unreadable[:SHOWN]:
        print(f"    could not read {u}")
    summary = (f"{s.scanned} text file(s) scanned, {s.binary} binary, {s.missing} absent from the checkout, "
               f"of {len(paths)} tracked")
    if s.offenders or s.unreadable:
        print(f"=== CONFLICT MARKERS: RED — {len(s.offenders)} unresolved merge-conflict marker line(s), "
              f"{len(s.unreadable)} tracked file(s) unreadable ({summary}). Finish the merge: `git diff --check` names "
              "them in the working tree and `git diff --cached --check` in the index — run the staged form between "
              "the `git add -A` and the `git commit` of every checkpoint ===")
        return 1
    print(f"=== CONFLICT MARKERS: GREEN — {summary} ===")
    return 0


def self_test() -> int:
    failures: list[str] = []

    def expect(label: str, got, want) -> None:
        ok = got == want
        print(f"  {'ok  ' if ok else 'FAIL'} {label}" + ("" if ok else f": got {got!r}, want {want!r}"))
        if not ok:
            failures.append(label)

    from scratch_repo import ScratchRepo, scratch_dir  # noqa: PLC0415

    root = scratch_dir("conflict-markers-")
    for eol in ("\r\n", "\n"):
        name = "crlf.txt" if eol == "\r\n" else "lf.txt"
        (root / name).write_bytes(eol.join(["context above", OURS + " HEAD", "our side", BASE + " merged common",
                                            "the ancestor", SPLIT, "their side", THEIRS + " origin/main",
                                            "context below"]).encode() + eol.encode())
        s = scan(root, [name])
        expect(f"a planted diff3 hunk ({'CRLF' if eol == chr(13) + chr(10) else 'LF'}) fires on all four markers, "
               "by line", [(n, m) for _, n, m in s.offenders], [(2, OURS), (4, BASE), (6, SPLIT), (8, THEIRS)])
    (root / "heading.md").write_bytes(("Title\r\n" + SPLIT + "\r\n\r\nbody\r\n").encode())
    (root / "table.md").write_bytes(("| a | b |\n| - | - |\n" + BASE + "\n").encode())
    s = scan(root, ["heading.md", "table.md"])
    expect("a divider without its brackets is not a conflict (the company rule)", (s.scanned, s.offenders), (2, []))
    (root / "blob.bin").write_bytes(b"\0\1\2binary\r\n" + (OURS + " HEAD\r\n").encode())
    s = scan(root, ["blob.bin"])
    expect("a file with a NUL in its first 8 KB is binary and not scanned", (s.scanned, s.binary, s.offenders),
           (0, 1, []))
    (root / "eight.txt").write_bytes(("<" * 8 + "\n" + "=" * 7 + " x\n" + OURS + "x\n").encode())
    s = scan(root, ["eight.txt"])
    expect("eight characters, a labelled divider and an opener glued to text are not markers", s.offenders, [])
    (root / "eof.txt").write_bytes(("text\n" + THEIRS).encode())
    expect("the end of the file ends the last line as a newline would", [m for _, _, m in scan(root, ["eof.txt"]).offenders],
           [THEIRS])
    s = scan(root, ["absent.txt", "lf.txt"])
    expect("a tracked path absent from the checkout is counted, and the file after it is still swept",
           (s.missing, len(s.offenders)), (1, 4))
    (root / "sub").mkdir()
    s = scan(root, ["sub"])
    expect("a directory (a gitlink's checkout) is absent, never read", (s.missing, s.unreadable), (1, []))
    if os.name != "nt" and os.geteuid() != 0:
        locked = root / "locked.txt"
        locked.write_bytes(b"plain\n")
        locked.chmod(0)
        s = scan(root, ["locked.txt", "lf.txt"])
        locked.chmod(0o600)
        expect("a file that exists but cannot be opened is reported, and the sweep goes on",
               (len(s.unreadable), len(s.offenders)), (1, 4))
    # check(): a broken enumeration is RED, never a clean tree — no repository at all, and a repository whose
    # enumeration finds fewer than MIN_TRACKED paths (the heading file alone, clean)
    expect("a directory git cannot enumerate is RED", check(root), 1)
    repo = ScratchRepo.init(root)
    repo.git("add", "heading.md")
    expect("an enumeration of too few tracked paths is RED", check(root), 1)
    print(f"=== CONFLICT MARKERS SELF-TEST: {'FAIL — ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    group = ap.add_mutually_exclusive_group(required=True)
    group.add_argument("--check", action="store_true")
    group.add_argument("--self-test", action="store_true")
    ap.add_argument("--root", type=Path, default=REPO, help=argparse.SUPPRESS)
    args = ap.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")
    return self_test() if args.self_test else check(args.root)


if __name__ == "__main__":
    sys.exit(main())
