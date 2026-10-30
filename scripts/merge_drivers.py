#!/usr/bin/env python3
"""merge_drivers.py — THE GIT MERGE DRIVERS FOR THE FILES TWO LANDINGS BOTH EDIT, and the one way main is merged in
(kb/Work PB3610; DESIGN-test-build-ci.md section 3.17.2 step 2).

    python scripts/merge_drivers.py merge <ref> [--message M] [--keep] [--repo DIR]   merge <ref> with the drivers, settle the ABI pin
    python scripts/merge_drivers.py driver <name> <base> <ours> <theirs> [<path>]   what git runs per conflicting file
    python scripts/merge_drivers.py --check        # .gitattributes and DRIVERS agree, every pattern names a tracked file, and
                                                   # a branch leaves a NEW driver's files as origin/main has them (PB3825)
    python scripts/merge_drivers.py --conflicts <logs dir>   # which files the landing logs stopped on, driver-covered or not
    python scripts/merge_drivers.py --hunks <file>...        # replay history's merges: were a file's conflicts insertions or revisions
    python scripts/merge_drivers.py --self-test

⛔ WHY. Landings conflicted on files that are not code: `conformance-suite/wiseowl/diagnostics.json`, the traceability
inventory, `tests/version-matrix/constructs.json`, the work notes two branches both append to, and the runtime ABI pin
in `RuntimeAbiPinDriftTests.cs`. Each conflict (the landing logs, E:\\COBOL-coord\\logs\\land-lane-*.log, counted 15 on
one manifest, 13 on another, 4 on the pin, 4 on diagnostics.json) stopped the landing and cost a finisher cycle (about
100 k tokens and a re-gate) to do what a program does exactly: keep both sides' entries. Two branches that each ADD an
entry to a keyed catalogue touch the same neighbourhood, which a line merge calls a conflict although the answer is
always both entries.

WHAT A DRIVER IS. A file that `.gitattributes` marks `merge=<name>` is merged by the command configured as
`merge.<name>.driver` whenever BOTH sides changed it; git passes the three versions and takes the result in place of its
own line merge. The configuration is not part of the repository, so every merge this repository makes passes it on the
command line (`config_args`, a `-c merge.<name>.driver=…` per driver): `enqueue.py land` merges main in through
`merge()` below, and a finisher resolving a landing's CONFLICT merges through `merge_drivers.py merge`, so the drivers
cannot be forgotten and no persistent per-checkout configuration can go stale. The ATTRIBUTES come from the registry
too (`attributes_tree`, given to git as `-c attr.tree=…`): the merged side's own `.gitattributes` followed by a
`<pattern> merge=<driver>` line per DRIVERS pattern, so a branch cut before a driver existed (its `.gitattributes`
still says `merge=union`, or nothing) merges that driver's files through it. Without that, a branch cut before
kb/Work PB3727 merging a main whose public-API baseline the driver SORTED got its union merge: 31 duplicated symbols in one
measured case, RS0025 in the Release build.

THE DRIVERS (DRIVERS: the one registry; `.gitattributes` repeats its patterns, and `--check` holds the two together):
  KEYED CATALOGUES  a file of entries that are each a block of lines at one indentation, KEYED (an object's member
                    name, or a field of an array element). The merge is three-way per KEY: an entry only one side
                    changed takes that side's text, an entry both sides changed identically is taken once, an entry
                    both changed differently (or one changed and one removed) is a REAL conflict. The entry text is
                    preserved byte for byte (diagnostics.json holds both raw § and \\u00a7 spellings: a parse and
                    re-serialize would rewrite every line), and the merged file must still parse as JSON.
  WORK NOTES        kb/Work/PB*.md: the frontmatter is merged per KEY (the same rule), the body by git's UNION merge
                    (two branches that each append a paragraph keep both; a line both rewrote differently is kept
                    twice, visible to the reader, never lost).
  RUNTIME ABI PIN   src/WiseOwl.Cobol.Runtime/RuntimeAbi.pin (abi_pin.py): two branches that each raised a counter raise it
                    by both, and the hashes of the merged surface, which no merge can know, are marked UNMEASURED for
                    `abi_pin.py settle` to measure on the merged tree before the merge commit is made.
  SYMBOL SETS       src/**/PublicAPI.*.txt, the public-API baselines (Microsoft.CodeAnalysis.PublicApiAnalyzers): a SET of
                    symbol lines under the `#nullable enable` header, a `*REMOVED*` line one more member (kb/Work PB3727;
                    DESIGN-orchestrator-loop.md section 16.9). A line either side added is kept, a line either side
                    removed is gone, the header lines stay first and the rest is sorted (ordinal), so the merge never
                    declines: a line is atomic, and a stale symbol fails the build (RS0017) as any wrong baseline does.
                    git's union merge, which governed these files before, kept a line one side removed when the other
                    side added a line beside it, and kept a symbol both sides added at different places twice (PB3694).
                    `landing_check.py` (arm 0b) reads the baselines of the landing's tree and STOPS on a symbol listed
                    twice (baseline_duplicates, RS0025). GitHub's merge group runs no driver and merges the pushed head
                    with the main of the moment: a main that advanced after the push is read by CI alone (PB3694).
PROSE HAS NO DRIVER, ON PURPOSE (kb/Work PB3643). `docs/CONFORMANCE.md` and the `DESIGN-*.md` documents look append-only
and conflict at landings, but `--hunks <path>…` (replay every merge in history that touched the file, classify each
conflict hunk of git's line merge) measured otherwise over CONFORMANCE.md, DESIGN-runtime-library.md and
DESIGN-test-build-ci.md: 45 conflicted merges holding 3 hunks where both sides inserted at one place and 115 hunks where
both sides REVISED text the base already held (the same paragraph of a mechanism, the same row of the Annex A.1
register). A union driver would keep both versions of a revised paragraph, stale text beside its replacement in a
document that must describe the CURRENT compiler (CLAUDE.md rule 6); a word-level merge of 32 of those merges (the first two documents', before `--hunks` walked full history)
resolved 9, and 3 of those 9 came out different from the resolution a person made (text the person had removed
was still there). Such a conflict is a decision about what the text now says: it stays the finisher's. Run `--hunks`
again before proposing a prose driver; a file whose conflicts turn out to be insertions earns one.
A file a driver declines (a real conflict at its level) falls back to git's own line merge of the three files: where that
merges cleanly, as it would without a driver, the file merges; else the file the finisher opens carries the usual markers.

A NEW DRIVER LANDS ALONE (kb/Work PB3727, PB3825; DESIGN-orchestrator-loop.md section 16.9): until the branch that adds a driver
lands, its landing merges main in through MAIN's registry, which has no such driver, so the files the driver governs merge
by git's line merge. `--check` therefore refuses a branch that changes a file a DRIVERS pattern governs while origin/main's
`.gitattributes` does not yet give that pattern that driver (and the merge base's did not either: a driver main re-pointed since
the cut is main's change), unless the file is main's bytes (`new_driver_files`).

THE MERGE (`merge`): `git merge --no-commit` with the drivers, then `abi_pin.settle` when the pin says UNMEASURED, then
the merge commit. The settle writes only the files abi_pin.OWNED names; `prune_worktrees.mechanical` (the review's
coverage rule) accepts a merge whose tree differs from the drivers' own merge in those files and nowhere else.
`merge` answers with a `Merged`, which tells THREE ends apart (kb/Work PB3673): the branch now contains the ref (`clean`),
files left in conflict (`conflicts`: a finisher resolves them), and a CLEAN merge whose runtime ABI pin could not be
settled (`unsettled`: abi_pin's Failure, classified by run_gate_legs.NOT_RUN_CAUSES as the host's or the tree's). Both
non-clean ends abort the merge and leave the branch unchanged (`--keep` leaves it in progress for a finisher); the landing
(enqueue.py) ends them differently, because a conflict is a finisher's work and a settle the host blocked is a retry. The
`merge` command's exit: 0 merged · 1 conflicts · EXIT_INCOMPLETE (3) a clean merge that could not be completed, its line
`NOT COMPLETED [<cause>] — …` naming the cause's tag (run_gate_legs.NOT_RUN_CAUSES: `merge-blocked` is the host's,
`merge-failed` the merged tree's).
"""
from __future__ import annotations

import argparse
import dataclasses
import json
import pathlib
import re
import subprocess
import sys
import tempfile
from typing import Callable, NamedTuple

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import abi_pin  # noqa: E402  (the runtime ABI pin: the driver's merger, the settle `merge` runs and its Failure, kb/Work PB3610, PB3673)
import verdict_line  # noqa: E402  (the one home of the `=== ENQUEUE: ` verdict line and the CONFLICT outcome, kb/Work PB3714)
from standard_streams import ensure_utf8

#: A merge function: (base, ours, theirs) texts -> the merged text, or None for a real conflict.
Merger = Callable[[str, str, str], "str | None"]
NL = chr(10)
#: `merge_drivers.py merge`'s exit for a clean merge that could not be completed (1 is a conflict).
EXIT_INCOMPLETE = 3
#: The ref a landing merges in and `--check` reads the attributes of (new_driver_files).
MAIN_REF = "origin/main"


# ── the three-way rule, over keyed blocks ────────────────────────────────────────────────────────────────────────
def merge_maps(base: dict[str, str], ours: dict[str, str], theirs: dict[str, str]) -> tuple[list[tuple[str, str]], list[str]]:
    """Three-way merge of keyed texts: (the merged (key, text) pairs in order, the keys in real conflict).
    Order: ours', then each key only theirs holds placed after the nearest preceding key of theirs already placed
    (before the first one, at the front), so a file kept in a deliberate order keeps it."""
    value: dict[str, str | None] = {}
    conflicts: list[str] = []
    for k in dict.fromkeys([*ours, *theirs, *base]):
        b, o, t = base.get(k), ours.get(k), theirs.get(k)
        if o == t:
            value[k] = o
        elif o == b:
            value[k] = t
        elif t == b:
            value[k] = o
        else:
            conflicts.append(k)
    order = [k for k in ours if value.get(k) is not None]
    tkeys = list(theirs)
    for i, k in enumerate(tkeys):
        if k in order or value.get(k) is None:
            continue
        j = i - 1
        while j >= 0 and tkeys[j] not in order:
            j -= 1
        order.insert(order.index(tkeys[j]) + 1 if j >= 0 else 0, k)
    return [(k, value[k]) for k in order if value.get(k) is not None], conflicts   # type: ignore[misc]


def merge_atom(base: str, ours: str, theirs: str) -> str | None:
    """Three-way merge of one indivisible text (a catalogue's head or tail): None when both sides changed it differently."""
    if ours == theirs or theirs == base:
        return ours
    return theirs if ours == base else None


# ── keyed JSON catalogues ────────────────────────────────────────────────────────────────────────────────────────
@dataclasses.dataclass(frozen=True)
class Layout:
    """Where a catalogue's entries are: the items of the container reached from the root through the member names in
    `path` (the root itself when empty). An object's items are its members, keyed by member name; an array's items are
    its elements, keyed by their `key_field` member. Read from the JSON STRUCTURE, never from indentation:
    constructs.json carries entries hand-written at three different indentations."""
    path: tuple[str, ...] = ()
    key_field: str | None = None


_MEMBER_KEY = re.compile(r'"(?:[^"\\]|\\.)*"\s*:')
_TOKEN = re.compile(r'"(?:[^"\\]|\\.)*"|[\[\]{},:]')


def _items(text: str, open_at: int) -> tuple[list[tuple[int, int]], int] | None:
    """(the (start, end) of each item of the container whose opening bracket is at `open_at`, the index of its closing
    bracket), or None when the text ends inside it. An item is trimmed of the whitespace around it."""
    depth, spans, start = 0, [], open_at + 1
    for tok in _TOKEN.finditer(text, open_at):
        c = tok.group()
        if c in "[{":
            depth += 1
        elif c in "]}":
            depth -= 1
            if depth == 0:
                if text[start:tok.start()].strip():
                    spans.append((start, tok.start()))
                return [_trim(text, a, b) for a, b in spans], tok.start()
        elif c == "," and depth == 1:
            spans.append((start, tok.start()))
            start = tok.end()
    return None


def _trim(text: str, a: int, b: int) -> tuple[int, int]:
    piece = text[a:b]
    return a + len(piece) - len(piece.lstrip()), b - (len(piece) - len(piece.rstrip()))


def _descend(text: str, open_at: int, name: str) -> int:
    """The index of the opening bracket of the value of member `name` of the object opening at `open_at`, or -1."""
    found = _items(text, open_at) if text[open_at] == "{" else None
    for a, b in found[0] if found else ():
        try:
            key = next(iter(json.loads("{" + text[a:b] + "}")))
        except (ValueError, StopIteration):
            return -1
        if key == name:
            colon = _MEMBER_KEY.match(text, a)
            return next((i for i in range(colon.end(), b) if not text[i].isspace()), -1) if colon else -1
    return -1


def segment(text: str, layout: Layout) -> tuple[str, dict[str, tuple[str, str]], str] | None:
    """(head, {key: (lead, item text)} in file order, tail) of a catalogue: `head` is everything up to and including the
    container's opening bracket, `lead` the whitespace before an item, `tail` everything from the last item's end. None
    when the text is not shaped as the layout says (no such container, no items, a keyless or duplicate key)."""
    open_at = next((i for i, c in enumerate(text) if not c.isspace()), -1)
    for name in layout.path:
        open_at = _descend(text, open_at, name) if open_at >= 0 else -1
    found = _items(text, open_at) if open_at >= 0 and text[open_at] in "{[" else None
    if not found or not found[0]:
        return None
    spans, entries, prev_end = found[0], {}, open_at + 1
    for a, b in spans:
        lead, item = text[prev_end:a].lstrip(","), text[a:b]
        try:
            key = next(iter(json.loads("{" + item + "}"))) if text[open_at] == "{" else json.loads(item)[layout.key_field]
        except (ValueError, KeyError, TypeError, StopIteration):
            return None
        if not isinstance(key, str) or key in entries:
            return None
        entries[key] = (lead, item)
        prev_end = b
    return text[:open_at + 1], entries, text[spans[-1][1]:]


def merge_keyed(layout: Layout) -> Merger:
    def merge(base: str, ours: str, theirs: str) -> str | None:
        sb, so, st = segment(base, layout), segment(ours, layout), segment(theirs, layout)
        if not (sb and so and st):
            return None
        head, tail = merge_atom(sb[0], so[0], st[0]), merge_atom(sb[2], so[2], st[2])
        merged, conflicts = merge_maps(*({k: lead + item for k, (lead, item) in s[1].items()} for s in (sb, so, st)))
        if head is None or tail is None or conflicts or not merged:
            return None
        out = head + merged[0][1] + "".join("," + text for _, text in merged[1:]) + tail
        try:
            json.loads(out)
        except ValueError:
            return None
        return out
    return merge


# ── work notes ───────────────────────────────────────────────────────────────────────────────────────────────────
def split_note(text: str) -> tuple[dict[str, str], str] | None:
    """({frontmatter key: its lines}, the body) of a note, or None when it has no frontmatter. A key's block is its
    `key: …` line and the indented or `-` lines under it."""
    lines = text.split(NL)
    if not lines or lines[0] != "---" or "---" not in lines[1:]:
        return None
    end = lines.index("---", 1)
    blocks: dict[str, list[str]] = {}
    key = None
    for ln in lines[1:end]:
        m = re.match(r"^([A-Za-z_][\w-]*):", ln)
        if m:
            key = m.group(1)
            if key in blocks:
                return None
            blocks[key] = [ln]
        elif key is not None:
            blocks[key].append(ln)
        else:
            return None
    return {k: NL.join(v) for k, v in blocks.items()}, NL.join(lines[end:])


def merge_file(options: list[str], base: str, ours: str, theirs: str) -> subprocess.CompletedProcess:
    """`git merge-file -p <options>` over three texts: the one place the three versions are written out for git. The exit
    status is the number of conflicts (0 clean), or negative / above 127 when git failed."""
    with tempfile.TemporaryDirectory(prefix="merge-file-") as d:
        paths = []
        for name, text in (("ours", ours), ("base", base), ("theirs", theirs)):
            p = pathlib.Path(d) / name
            p.write_bytes(text.encode("utf-8"))
            paths.append(str(p))
        return subprocess.run(["git", "merge-file", "-p", *options, *paths], capture_output=True)


def union_text(base: str, ours: str, theirs: str) -> str | None:
    """git's union merge of three texts: both sides' changes, a line both rewrote kept from both; None when git failed."""
    r = merge_file(["--union"], base, ours, theirs)
    return r.stdout.decode("utf-8") if r.returncode == 0 else None


# ── what a conflict is made of (the measurement a prose driver would be judged against, kb/Work PB3643) ──────────────
#: A conflict hunk of `git merge-file --diff3`: the lines of ours, of the base and of theirs between the markers.
Hunk = tuple[list[str], list[str], list[str]]
_DIFF3 = ["--diff3", "-L", "ours", "-L", "base", "-L", "theirs"]


def conflict_hunks(base: str, ours: str, theirs: str) -> list[Hunk] | None:
    """The conflict hunks of git's line merge of three texts: [] when it merges clean, None when git failed or its
    output is not the diff3 layout (ours, base, theirs, in that order; a marker is read only in the state that expects
    it, so a document line that merely looks like one is content)."""
    r = merge_file(_DIFF3, base, ours, theirs)
    if r.returncode < 0 or r.returncode > 127:
        return None
    hunks: list[Hunk] = []
    state, cur = "text", ([], [], [])
    for ln in r.stdout.decode("utf-8").split(NL):
        if state == "text" and ln == "<<<<<<< ours":
            state, cur = "ours", ([], [], [])
        elif state == "ours" and ln == "||||||| base":
            state = "base"
        elif state == "base" and ln == "=======":
            state = "theirs"
        elif state == "theirs" and ln == ">>>>>>> theirs":
            hunks.append(cur)
            state = "text"
        elif state != "text":
            cur[("ours", "base", "theirs").index(state)].append(ln)
    return hunks if state == "text" else None


def hunk_kind(hunk: Hunk) -> str:
    """`insertion` when the base section is EMPTY (both sides inserted text at one place: nothing of the base was
    touched, so keeping both is exact), else `revision` (a side changed or removed base text the other also touched: the
    conflict is a decision about what the text should now say, which no merge can make)."""
    return "revision" if hunk[1] else "insertion"


def history_hunks(repo: pathlib.Path, paths: list[str]) -> dict[str, dict[str, int]]:
    """{path: {merges where both parents changed it, conflicted merges, insertion hunks, revision hunks}} over the merge
    commits in `repo`'s history that touched the paths: each is replayed as git's line merge of the merge base and the
    two parents, so the answer is what a driver would have been asked, not what a finisher happened to do (`unreadable`:
    git could not merge the three texts, counted so that a failure never reads as a clean merge)."""
    out = {p: {"both-changed": 0, "conflicted": 0, "insertion": 0, "revision": 0, "unreadable": 0} for p in paths}
    for p in paths:
        # One walk per path, `--full-history`: git's default simplification drops a merge TREESAME to one parent (a
        # resolution that took one side whole) with the side history behind it, and drops different merges for each set
        # of paths asked about. A merge both of whose parents changed the file differs from at least one parent, so the
        # full-history walk lists every one of them, and the count for a file never depends on the other paths asked.
        for ln in git(repo, "log", "--merges", "--full-history", "--format=%H %P", "--", p).stdout.splitlines():
            parents = ln.split()[1:]
            base = git(repo, "merge-base", *parents).stdout.strip() if len(parents) == 2 else ""
            blobs = [git(repo, "show", f"{rev}:{p}") for rev in (base, *parents)] if base else []
            if not blobs or any(b.returncode for b in blobs) or blobs[1].stdout == blobs[2].stdout:
                continue
            b, o, t = (x.stdout for x in blobs)
            if o == b or t == b:
                continue
            out[p]["both-changed"] += 1
            hunks = conflict_hunks(b, o, t)
            if hunks is None:
                out[p]["unreadable"] += 1
                continue
            out[p]["conflicted"] += bool(hunks)
            for h in hunks:
                out[p][hunk_kind(h)] += 1
    return out


def merge_note(base: str, ours: str, theirs: str) -> str | None:
    sb, so, st = split_note(base), split_note(ours), split_note(theirs)
    if not (sb and so and st):
        return None
    merged, conflicts = merge_maps(sb[0], so[0], st[0])
    if conflicts:
        return None
    body = union_text(sb[1], so[1], st[1])
    return None if body is None else NL.join(["---", *(text for _, text in merged), body])


# ── symbol sets: the public-API baselines ───────────────────────────────────────────────────────────────────────
def merge_symbol_set(base: str, ours: str, theirs: str) -> str:
    """Three-way merge of a public-API baseline as a SET of lines (kb/Work PB3727; DESIGN-orchestrator-loop.md section
    16.9): (base ∪ (ours − base) ∪ (theirs − base)) − (base − ours) − (base − theirs), i.e. a line either side added is
    kept and a line either side removed is gone. The header (the `#` lines: `#nullable enable`) comes first, the symbol
    lines after it, each part in ordinal order, LF-terminated; a blank line is not a member and a CR is not part of a
    line. Never None: two lines of a set cannot conflict."""
    b, o, t = ({ln.rstrip(chr(13)) for ln in text.split(NL)} - {""} for text in (base, ours, theirs))
    kept = (b | (o - b) | (t - b)) - (b - o) - (b - t)
    header = sorted(ln for ln in kept if ln.startswith("#"))
    return "".join(ln + NL for ln in header + sorted(kept.difference(header)))


#: The DRIVERS row that merges the public-API baselines: the one definition of which files are symbol baselines.
SYMBOL_SET_DRIVER = "cobol-keyed-publicapi"


def is_symbol_baseline(path: str) -> bool:
    """Whether `path` is a public-API baseline, a file the SYMBOL_SET_DRIVER row governs (its pattern, pattern_matches)."""
    return any(pattern_matches(path, p) for d in DRIVERS if d.name == SYMBOL_SET_DRIVER for p in d.patterns)


def baseline_duplicates(files: dict[str, str]) -> list[str]:
    """The symbols the baselines `files` ({path: text}) list twice, which Microsoft.CodeAnalysis.PublicApiAnalyzers fails
    the build on as RS0025 (kb/Work PB3694): a symbol line twice in one file, or one plain line in two files of one directory
    (Shipped and Unshipped are one project's API). A `*REMOVED*X` line is a different line from the Shipped `X` it removes,
    so that pair is no finding. A line is compared as merge_symbol_set reads it (a CR is no part of it; a blank line and a
    `#` header line are no symbol). Sorted, one finding per symbol and place, each naming the file(s)."""
    by_dir: dict[str, dict[str, set[str]]] = {}
    found: list[str] = []
    for path, text in sorted(files.items()):
        seen: set[str] = set()
        for line in (ln.rstrip(chr(13)) for ln in text.split(NL)):
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


# ── the registry ─────────────────────────────────────────────────────────────────────────────────────────────────
@dataclasses.dataclass(frozen=True)
class Driver:
    name: str                      # the `merge=` attribute value
    patterns: tuple[str, ...]      # the .gitattributes patterns it governs
    merger: Callable[[], Merger]   # built on first use
    why: str


DRIVERS: tuple[Driver, ...] = (
    Driver("cobol-keyed-object", ("conformance-suite/wiseowl/diagnostics.json", "conformance-suite/wiseowl/options.json"),
           lambda: merge_keyed(Layout()),
           "test id -> its entry: every refusal and option a landing adds is one more member"),
    Driver("cobol-keyed-inventory", ("tests/version-matrix/traceability-inventory.json",),
           lambda: merge_keyed(Layout((), "rule-id")),
           "rule id -> its verdict row: a landing re-verdicts the rows its fix closes"),
    Driver("cobol-keyed-constructs", ("tests/version-matrix/constructs.json",),
           lambda: merge_keyed(Layout(("constructs",), "id")),
           "construct id -> its entry in the version-matrix catalogue"),
    Driver("cobol-work-note", ("kb/Work/PB*.md",), lambda: merge_note,
           "frontmatter per key, body by union: two branches that each append to one note keep both"),
    Driver("cobol-abi-pin", ("src/WiseOwl.Cobol.Runtime/RuntimeAbi.pin",), lambda: abi_pin.merge_text,
           "the runtime major and call ABI counters add; the merged surface's hashes are measured, not merged"),
    Driver("cobol-keyed-publicapi", ("src/**/PublicAPI.*.txt",), lambda: merge_symbol_set,
           "a public-API baseline is a set of symbol lines: a line either side added is kept, a line either side removed "
           "is gone, the header stays first, the rest sorted"),
)


def python_posix() -> str:
    return pathlib.Path(sys.executable).as_posix()


def config_args(attributes: str | None = None) -> list[str]:
    """The `git -c merge.<name>.driver=…` arguments that make a merge, or a `merge-tree`, use every driver, and with
    `attributes` (a tree, attributes_tree) the `-c attr.tree=…` that has git read the attributes from it. The script is
    THIS checkout's: a branch cut before the drivers existed still merges through them."""
    script = pathlib.Path(__file__).resolve().as_posix()
    out: list[str] = []
    for d in DRIVERS:
        out += ["-c", f'merge.{d.name}.driver="{python_posix()}" "{script}" driver {d.name} %O %A %B %P']
    return out + (["-c", f"attr.tree={attributes}"] if attributes else [])


def attributes_tree(repo: pathlib.Path, rev: str) -> str | None:
    """THE ATTRIBUTES A MERGE READS: a tree written into `repo` whose one file is a `.gitattributes` holding `rev`'s own
    (none when `rev` has none) followed by a `<pattern> merge=<driver>` line per DRIVERS pattern. In gitattributes(5) a
    later line wins, so the registry decides the merge attribute of every file it covers and `rev` decides everything
    else (`text`, `eol`, `-text`, its other merge attributes). `rev` is the side being merged INTO (HEAD for `merge`,
    the first parent for `merged_tree`), whose attributes git reads by default. None when git could not write the
    tree; the caller then merges with git's own attribute lookup, and says so. The repository keeps ONE `.gitattributes`,
    at its root, which is the one this tree carries."""
    own = git(repo, "show", f"{rev}:.gitattributes")
    text = own.stdout if own.returncode == 0 else ""
    text += ("" if not text or text.endswith(NL) else NL) + "".join(f"{p} merge={d.name}{NL}" for d in DRIVERS for p in d.patterns)
    blob = subprocess.run(["git", "hash-object", "-w", "--stdin"], cwd=repo, input=text.encode("utf-8"), capture_output=True)
    if blob.returncode != 0:
        return None
    tree = subprocess.run(["git", "mktree"], cwd=repo, capture_output=True,
                          input=f"100644 blob {blob.stdout.decode().strip()}{chr(9)}.gitattributes{NL}".encode("utf-8"))
    made = tree.stdout.decode().strip() if tree.returncode == 0 else ""
    return made or None


def git(repo: pathlib.Path, *args: str, attributes_of: str | None = None) -> subprocess.CompletedProcess:
    """`git <args>` in `repo`; with `attributes_of` (a revision) through every driver, reading attributes_tree(repo,
    attributes_of)."""
    extra: list[str] = []
    if attributes_of is not None:
        tree = attributes_tree(repo, attributes_of)
        if tree is None:
            print(f"merge_drivers: could not write the attributes tree in {repo}; git reads its own attributes", file=sys.stderr)
        extra = config_args(tree)
    return subprocess.run(["git", *extra, *args], cwd=repo, capture_output=True, text=True, encoding="utf-8", errors="replace")


# ── the driver entry point ───────────────────────────────────────────────────────────────────────────────────────
def run_driver(name: str, base: pathlib.Path, ours: pathlib.Path, theirs: pathlib.Path) -> int:
    """What git runs for one conflicting file: the merged text replaces `ours`, exit 0; else the plain three-way line
    merge decides: its clean result replaces `ours`, exit 0, or its conflict markers do, exit 1."""
    driver = next((d for d in DRIVERS if d.name == name), None)
    if driver is None:
        print(f"merge_drivers: no driver named {name}", file=sys.stderr)
        return 2
    merged = driver.merger()(base.read_bytes().decode("utf-8"), ours.read_bytes().decode("utf-8"),
                             theirs.read_bytes().decode("utf-8"))
    if merged is not None:
        ours.write_bytes(merged.encode("utf-8"))
        return 0
    # The keyed merger declined (None): the plain three-way line merge decides, as it would without a driver. Clean (exit 0):
    # the merged lines are in `ours` and the file merges; else `ours` holds the conflict markers and git is told so.
    fallback = subprocess.run(["git", "merge-file", "-L", "ours", "-L", "base", "-L", "theirs", str(ours), str(base),
                               str(theirs)], capture_output=True)
    return 0 if fallback.returncode == 0 else 1


# ── the merge ────────────────────────────────────────────────────────────────────────────────────────────────────
class Merged(NamedTuple):
    """What `merge` made of its ref (kb/Work PB3673): THREE ends, which a caller must tell apart because three different
    people fix them. `clean`: the branch now contains the ref. `conflicts`: the files left in conflict, a finisher's work.
    `incomplete`: the merge was clean (or git refused to start it) but could not be completed, abi_pin's Failure with the
    cause classified by run_gate_legs: the ABI pin could not be settled, or git refused the merge commit or the fast-forward;
    MERGE_BLOCKED is the host's, a retry's, MERGE_FAILED is the merged tree's. Never both a conflict and an incomplete merge.
    `in_progress`: the merge was left in progress for a finisher (`keep`), not aborted."""
    conflicts: tuple[str, ...] = ()
    incomplete: abi_pin.Failure | None = None
    in_progress: bool = False

    @property
    def clean(self) -> bool:
        return not self.conflicts and self.incomplete is None


def _refused(what: str, p: subprocess.CompletedProcess) -> Merged:
    """The Merged of a git command that failed with no file in conflict: the host's refusal (a lock, a full disk, a
    worktree that would be overwritten), with git's own words; never reported as a conflict in some file."""
    return Merged(incomplete=abi_pin.Failure(abi_pin.MERGE_BLOCKED, f"{what} failed: {(p.stderr or p.stdout).strip()}"))


def merge(repo: pathlib.Path, ref: str, message: str, measure: abi_pin.Runner | None = None, keep: bool = False) -> Merged:
    """MERGE `ref` into the checked-out branch with every driver: `clean` when the branch now contains it (the merge
    commit made, the ABI pin settled in it); else the files left in conflict (`conflicts`) or why a clean merge could not be
    completed (`incomplete`: the pin could not be settled, or git refused), with the merge aborted and the branch
    unchanged. A branch that only needs `ref` fast-forwards. `measure`: abi_pin.settle's measurement (a self-test's
    stand-in for the build). `keep` (a finisher): the merge is left in progress instead of aborted, with its conflict
    markers or, when only the pin could not be settled, as it stood when the settle failed; the finisher resolves, runs
    `abi_pin.py settle --stage` when the pin says UNMEASURED, and commits."""
    if git(repo, "merge-base", "--is-ancestor", ref, "HEAD").returncode == 0:
        return Merged()                                  # the branch already contains `ref`: nothing to merge
    if git(repo, "merge-base", "--is-ancestor", "HEAD", ref).returncode == 0:
        ff = git(repo, "merge", "--ff-only", ref)
        return Merged() if ff.returncode == 0 else _refused(f"git merge --ff-only {ref}", ff)
    started = git(repo, "merge", "--no-commit", "--no-ff", ref, attributes_of="HEAD")
    if started.returncode != 0:
        files = git(repo, "diff", "--name-only", "--diff-filter=U").stdout.split()
        if not keep:
            git(repo, "merge", "--abort")
        return Merged(tuple(files), in_progress=keep) if files else _refused(f"git merge {ref}", started)   # none: git refused
    if abi_pin.needs_settle(repo):
        err = abi_pin.settle(repo, measure=measure or abi_pin.run_measurement)
        if err:
            print(f"merge_drivers: the runtime ABI pin could not be settled [{err.cause}]: {err.why}", file=sys.stderr)
            if not keep:
                git(repo, "merge", "--abort")
                git(repo, "checkout", "HEAD", "--", *abi_pin.OWNED)   # abort keeps the settle's own edits to files the merge changed
            return Merged(incomplete=err, in_progress=keep)
    done = git(repo, "commit", "--no-edit", "-m", message)
    if done.returncode != 0:
        git(repo, "merge", "--abort")
        return _refused("git commit of the merge", done)
    return Merged()


def merge_conflicts(repo: pathlib.Path | str, ref: str) -> list[str] | None:
    """What `merge` of `ref` into the checked-out HEAD would leave in conflict, computed without touching the worktree
    or the index: `git merge-tree` through every driver (config_args, as is_drivers_merge reads the merge). [] when it
    merges clean; else the conflicted files, where a file a driver merges by key is listed only when the driver
    declined it (both sides changed one entry differently: a real conflict, kb/Work PB3640 review R1-1); None when git
    cannot say (a missing ref: merge-tree exits 1 for that too, with no tree on stdout, so the tree line decides). The
    runtime ABI pin's settle needs a build, so a pin `merge` could not settle is not foreseen here."""
    return probe(repo, ["HEAD", ref], attributes_of="HEAD")[1]


def probe(repo: pathlib.Path | str, parents: list[str], attributes_of: str | None = None) -> tuple[str | None, list[str] | None]:
    """THE ONE `git merge-tree --write-tree` THROUGH EVERY DRIVER (kb/Work PB3723): (the merged tree, []) when `parents`
    merge clean; (None, the conflicted files) when they do not (a file a driver merges by key is listed only when the
    driver declined it); (None, None) when git cannot say (a missing ref, unrelated histories: merge-tree exits 1 for
    that too, with no tree on stdout, so the tree line decides). `attributes_of` (default: the first parent) names the
    revision whose attributes the merge reads (attributes_tree). The worktree and the index are not touched."""
    r = git(pathlib.Path(repo), "merge-tree", "--write-tree", "--name-only", "--no-messages", *parents,
            attributes_of=attributes_of or parents[0])
    lines = r.stdout.splitlines()
    if r.returncode not in (0, 1) or not lines or not re.fullmatch(r"[0-9a-f]{40,64}", lines[0].strip()):
        return None, None
    if r.returncode == 0:
        return lines[0].strip(), []
    return None, [f for f in dict.fromkeys(ln.strip() for ln in lines[1:]) if f]


class Foreseen(NamedTuple):
    """A queued head that conflicts with the queue ahead of it: the files left in conflict, and the entries ahead it
    conflicts with (their labels; empty: with main alone, which GitHub already reads as DIRTY)."""
    files: tuple[str, ...]
    ahead: tuple[str, ...]


def foresee_queue(repo: pathlib.Path | str, base: str, entries: list[tuple[str, str]]) -> dict[str, Foreseen]:
    """THE MERGE QUEUE, SIMULATED (kb/Work PB3723): the queue builds each entry's merge group on main plus the entries
    ahead of it that stay, and REMOVES an entry whose head conflicts there (reason `merge_conflict`, with no failed run
    and no event a reader sees until it has happened). `entries`: (label, head commit) in queue order; `base`: main.
    Each head is merged onto the result of those before it through every driver (probe), and an entry that conflicts is
    skipped, as the queue skips it. Returns {label: Foreseen} for each such entry, so its finisher can bring the entries
    ahead in while they are still queued. A head git cannot merge (an object this repository lacks) is left out: it is
    unknown, never a conflict. The runtime ABI pin's settle needs a build and is not foreseen (merge_conflicts)."""
    out: dict[str, Foreseen] = {}
    result, ahead = base, []
    for label, head in entries:
        tree, files = probe(repo, [result, head], attributes_of=base)
        if files is None:
            continue
        if tree is None:
            if probe(repo, [base, head], attributes_of=base)[1]:   # it conflicts with main itself: GitHub reads that DIRTY
                out[label] = Foreseen(tuple(files), ())
                continue
            with_head = [lab for lab, h in ahead if probe(repo, [h, head], attributes_of=base)[1]]
            out[label] = Foreseen(tuple(files), tuple(with_head or [lab for lab, _ in ahead]))
            continue
        made = git(pathlib.Path(repo), "-c", "user.name=merge_drivers", "-c", "user.email=merge_drivers@localhost",
                   "commit-tree", tree, "-p", result, "-p", head, "-m", f"queue simulation: {label}")
        if made.returncode != 0:
            break
        result = made.stdout.strip()
        ahead.append((label, head))
    return out


def merged_tree(repo: pathlib.Path | str, parents: list[str]) -> str | None:
    """The tree of the drivers' own merge of `parents` (`git merge-tree --write-tree` through every driver, config_args),
    computed without touching the worktree or the index; None when it does not merge clean or git cannot say (a missing
    ref, unrelated histories, a conflict a driver declined). THE ONE DEFINITION OF A CLEAN MERGE'S TREE (kb/Work PB3713):
    the review's coverage rule (is_drivers_merge) and the branch survey's LANDED proof
    (prune_worktrees.merges_to_main_unchanged) both read it, so the survey and the landing agree on what merges clean: a
    plain `git merge-tree` conflicts in a keyed catalogue the drivers merge by key. The attributes are the first
    parent's with the registry's merge lines over them (attributes_tree), as `merge` reads HEAD's."""
    return probe(repo, parents)[0]


def is_drivers_merge(repo: pathlib.Path | str, parents: list[str], tree: str) -> bool:
    """Whether a two-parent commit whose tree is `tree` is what `merge` makes of its parents: the drivers' own merge of
    them (merged_tree), or that tree with the runtime ABI pin settled into it (the three files abi_pin.OWNED names, and
    only when the drivers left the pin UNMEASURED). This is the review's coverage rule for a merge of main
    (prune_worktrees.mechanical), so it must be exactly the merge's own definition: there is no other."""
    clean = merged_tree(repo, parents)
    if clean is None:
        return False
    if clean == tree:
        return True
    changed = git(pathlib.Path(repo), "diff-tree", "-r", "--name-only", clean, tree).stdout.split()
    pin = git(pathlib.Path(repo), "cat-file", "blob", f"{clean}:{abi_pin.PIN_PATH}").stdout
    return bool(changed) and set(changed) <= set(abi_pin.OWNED) and abi_pin.unmeasured(pin)


# ── the audit ────────────────────────────────────────────────────────────────────────────────────────────────────
def pattern_matches(path: str, pattern: str) -> bool:
    """Whether the `.gitattributes` pattern names the repository-relative, `/`-separated `path`, by git's own rules
    (gitattributes(5), gitignore(5) "PATTERN FORMAT"): a pattern with no `/` matches the last component at any depth;
    one with a `/` matches the whole path from the root; `*` and `?` stay within one component, a leading `**/` and an
    inner `/**/` span zero or more directories, a trailing `/**` everything inside, `[...]` is one character of a class.
    `pathlib.PurePath.match` is not this: its `**` spans exactly one component, so `src/**/PublicAPI.*.txt` would miss
    a baseline two directories down that git hands to the driver (kb/Work PB3727)."""
    pat, subject = pattern.lstrip("/"), path
    if "/" not in pattern.rstrip("/"):
        subject = path.rsplit("/", 1)[-1]
    rx, i = "", 0
    while i < len(pat):
        at_start = i == 0 or pat[i - 1] == "/"
        if at_start and pat.startswith("**/", i):
            rx, i = rx + "(?:[^/]*/)*", i + 3
        elif at_start and pat.startswith("**", i) and i + 2 == len(pat):
            rx, i = rx + ".*", i + 2
        elif pat[i] == "*":
            rx, i = rx + "[^/]*", i + 1
        elif pat[i] == "?":
            rx, i = rx + "[^/]", i + 1
        elif pat[i] == "[" and "]" in pat[i + 2:]:
            end = pat.index("]", i + 2)
            body = pat[i + 1:end]
            rx, i = rx + "[" + ("^" + body[1:] if body.startswith("!") else body).replace(chr(92), chr(92) * 2) + "]", end + 1
        else:
            rx, i = rx + re.escape(pat[i]), i + 1
    return re.fullmatch(rx, subject) is not None


def attribute_drivers(text: str) -> dict[str, str]:
    """{pattern: driver name} of every `<pattern> merge=<name>` line in a .gitattributes text."""
    out = {}
    for ln in text.split(NL):
        m = re.match(r"^(\S+)\s+(?:.*\s)?merge=(\S+)\s*$", ln)
        if m and not ln.lstrip().startswith("#"):
            out[m.group(1)] = m.group(2)
    return out


def check(repo: pathlib.Path) -> list[str]:
    """Findings: the .gitattributes drivers and DRIVERS differ, or a driver's pattern names no tracked file."""
    findings = []
    declared = {p: d.name for d in DRIVERS for p in d.patterns}
    written = {p: n for p, n in attribute_drivers((repo / ".gitattributes").read_text(encoding="utf-8")).items()
               if n.startswith("cobol-")}
    for p, n in declared.items():
        if written.get(p) != n:
            findings.append(f".gitattributes does not say `{p} merge={n}` (it says {written.get(p)!r})")
    for p, n in written.items():
        if declared.get(p) != n:
            findings.append(f".gitattributes says `{p} merge={n}`, which no DRIVERS entry declares")
    tracked = git(repo, "ls-files", "-z").stdout.split(chr(0))
    for p in declared:
        if not any(pattern_matches(t, p) for t in tracked if t):
            findings.append(f"the pattern {p} of a driver names no tracked file")
    return findings


def new_driver_files(repo: pathlib.Path, main: str = MAIN_REF) -> tuple[list[str], str | None]:
    """A NEW DRIVER LANDS ALONE, mechanically (kb/Work PB3825; DESIGN-orchestrator-loop.md section 16.9): (findings, the
    reason the rule could not be read). A finding is a file the branch changed that a DRIVERS pattern governs while `main`'s
    `.gitattributes` does not yet give that pattern that driver, and whose bytes are not `main`'s.

    WHY. `enqueue.py land` merges main in through MAIN's registry, so until a branch that adds a driver (or moves a pattern
    to it) lands, git merges that driver's files by its line merge, not the driver. A branch that had also reformatted
    those files (PB3727 sorted both public-API baselines) met every later main change to them as a conflict and bounced
    out of the merge queue. A file left as main has it cannot conflict: git takes main's side and runs no driver.

    "The branch changed" is `merge-base(HEAD, main)..HEAD`, so a main that moved since is not the branch's change, and a
    file the branch changed back to main's bytes passes. A driver is the branch's to add only when the merge base's
    `.gitattributes` did not give the pattern it either: a branch cut before main renamed or re-pointed a driver still
    declares the old one, and adds none. The reason is not None when the check could not run (no `main`
    ref in this checkout, as in CI's, or no merge base in a shallow one): the caller says so and never reads it as a pass."""
    if git(repo, "rev-parse", "--verify", "-q", f"{main}^{{commit}}").returncode != 0:
        return [], f"{main} is not in this checkout"
    base = git(repo, "merge-base", "HEAD", main)
    if base.returncode != 0 or not base.stdout.strip():
        return [], f"HEAD and {main} have no merge base in this checkout"
    def attributes(rev: str) -> dict[str, str]:
        shown = git(repo, "show", f"{rev}:.gitattributes")
        return attribute_drivers(shown.stdout if shown.returncode == 0 else "")
    assigned, at_base = attributes(main), attributes(base.stdout.strip())
    # NEW is the pair THE BRANCH added: neither main's attributes nor the merge base's gave the pattern that driver. A pair
    # the base had and main since renamed, re-pointed or removed is main's change the branch has not merged yet: the remedy
    # there is to merge main in, never to revert the branch's own change (branch review R1-2).
    new = [(p, d.name) for d in DRIVERS for p in d.patterns if assigned.get(p) != d.name and at_base.get(p) != d.name]
    if not new:
        return [], None

    def blob(rev: str, path: str) -> str | None:
        r = git(repo, "rev-parse", "--verify", "-q", f"{rev}:{path}")
        return r.stdout.strip() if r.returncode == 0 else None
    findings = []
    changed = git(repo, "diff", "--name-only", "-z", "--no-renames", base.stdout.strip(), "HEAD").stdout.split(chr(0))
    for path in filter(None, changed):
        for pattern, name in new:
            if pattern_matches(path, pattern) and blob("HEAD", path) != blob(main, path):
                findings.append(
                    f"{path}: the driver {name} governs it (pattern {pattern}) but {main}'s .gitattributes says "
                    f"{assigned.get(pattern)!r} for that pattern, so the landing merges main in without the driver: a new "
                    f"driver lands alone, leave the file as main has it (`git checkout {main} -- {path}`)")
                break
    return findings, None


def covered_by_driver(path: str) -> bool:
    """Whether a driver merges `path` (its pattern names it as `.gitattributes` does, pattern_matches)."""
    return any(pattern_matches(path, p) for d in DRIVERS for p in d.patterns)


def conflict_report(logs: pathlib.Path) -> tuple[dict[str, int], int]:
    """({file: how many landing logs stopped on it}, logs read): the measurement the drivers' coverage is judged against.
    A file a driver does not cover and that is a keyed or measured catalogue is a driver to add; source code is a real
    conflict, which a finisher resolves."""
    counts: dict[str, int] = {}
    read = 0
    for log in sorted(logs.glob("land-lane-*.log")):
        read += 1
        for f in verdict_line.conflict_files(log.read_text(encoding="utf-8", errors="replace")):
            counts[f] = counts.get(f, 0) + 1
    return dict(sorted(counts.items(), key=lambda kv: -kv[1])), read


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
def self_test() -> int:
    from scratch_repo import ScratchRepo, remove_tree, scratch_dir  # noqa: PLC0415  (scripts/: the one scratch fixture)
    failures: list[str] = []

    def expect(label: str, ok: bool, detail: str = "") -> None:
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + (f"  ({detail})" if detail and not ok else ""))
        if not ok:
            failures.append(label)

    obj = lambda *members: "{" + NL + (("," + NL).join(members)) + NL + "}" + NL    # noqa: E731
    m = lambda k, v: f' "{k}": {{{NL}  "refuse-at": [{NL}   "{v}"{NL}  ]{NL} }}'    # noqa: E731
    keyed = merge_keyed(Layout())

    base = obj(m("a", "85"), m("c", "85"))
    ours = obj(m("a", "85"), m("b", "2002"), m("c", "85"), m("o", "85"))
    theirs = obj(m("a", "85"), m("c", "85"), m("t", "2014"))
    got = keyed(base, ours, theirs)
    expect("keyed: entries each side ADDED are both kept, ours' order first, theirs' after the key it followed",
           got == obj(m("a", "85"), m("b", "2002"), m("c", "85"), m("t", "2014"), m("o", "85")), repr(got))
    expect("keyed: the merged text is the entries' own bytes and parses", got is not None and json.loads(got) == {
        "a": {"refuse-at": ["85"]}, "b": {"refuse-at": ["2002"]}, "c": {"refuse-at": ["85"]},
        "o": {"refuse-at": ["85"]}, "t": {"refuse-at": ["2014"]}})
    expect("keyed: an entry only one side changed takes that side; one both changed identically is taken once",
           keyed(base, obj(m("a", "2002"), m("c", "85")), obj(m("a", "85"), m("c", "2014")))
           == obj(m("a", "2002"), m("c", "2014"))
           and keyed(base, obj(m("a", "2002"), m("c", "85")), obj(m("a", "2002"), m("c", "85")))
           == obj(m("a", "2002"), m("c", "85")))
    expect("keyed: an entry both sides changed DIFFERENTLY is a real conflict (None)",
           keyed(base, obj(m("a", "2002"), m("c", "85")), obj(m("a", "2014"), m("c", "85"))) is None)
    expect("keyed: one side removed an entry the other left alone: removed; the other changed it: a conflict",
           keyed(base, obj(m("a", "85")), obj(m("a", "85"), m("c", "85"))) == obj(m("a", "85"))
           and keyed(base, obj(m("a", "85")), obj(m("a", "85"), m("c", "2014"))) is None)
    expect("keyed: a text that is not shaped as the layout says is None, never a guess",
           keyed(base, "not json" + NL, theirs) is None)
    mixed = f'{{{NL} "e": {{{NL}  "expect": "ISO §14.9 (a)"{NL} }},{NL} "f": {{{NL}  "expect": "ISO \\u00a714.9 (b)"{NL} }}{NL}}}{NL}'
    expect("keyed: a raw § and a \\u00a7 spelling in one file are kept as written (no re-serialization)",
           keyed(mixed, mixed.replace('"f"', '"f2"'), mixed) is not None
           and "§14.9 (a)" in keyed(mixed, mixed.replace('"f"', '"f2"'), mixed)
           and "\\u00a714.9 (b)" in keyed(mixed, mixed.replace('"f"', '"f2"'), mixed))

    rows = lambda *ids: "[" + NL + (("," + NL).join(f' {{{NL}  "rule-id": "{i}",{NL}  "verdict": "GAP"{NL} }}' for i in ids)) + NL + "]" + NL    # noqa: E731
    inv = merge_keyed(Layout((), "rule-id"))
    expect("keyed rows: an array of objects is keyed by a member; both sides' appended rows are kept",
           inv(rows("R1", "R2"), rows("R1", "R2", "R3"), rows("R1", "R2", "R4")) in (rows("R1", "R2", "R4", "R3"), rows("R1", "R2", "R3", "R4"))
           and inv(rows("R1"), rows("R1"), rows("R1", "R2")) == rows("R1", "R2"))
    nested = lambda *ids: ('{' + NL + ' "_comment": "x",' + NL + ' "constructs": [' + NL + (("," + NL).join(f'  {{{NL}   "id": "{i}"{NL}  }}' for i in ids)) + NL + ' ]' + NL + '}' + NL)    # noqa: E731
    con = merge_keyed(Layout(("constructs",), "id"))
    expect("keyed nested: entries at a deeper indentation inside a member are keyed too, the head and tail untouched",
           con(nested("a"), nested("a", "b"), nested("a", "c")) in (nested("a", "b", "c"), nested("a", "c", "b"))
           and con(nested("a"), nested("a", "b"), nested("a")) == nested("a", "b"))

    note = lambda status, body, extra="": f"---{NL}title: t{NL}status: {status}{NL}{extra}---{NL}# T{NL}{body}"    # noqa: E731
    nb = note("open", f"text{NL}")
    expect("note: frontmatter merges per key and the bodies by union: a status flip and an appended paragraph both stay",
           merge_note(nb, note("landed", f"text{NL}"), note("open", f"text{NL}Landed A.{NL}")) == note("landed", f"text{NL}Landed A.{NL}")
           and merge_note(nb, note("open", f"text{NL}Landed A.{NL}"), note("open", f"text{NL}Landed B.{NL}")) is not None
           and "Landed A." in merge_note(nb, note("open", f"text{NL}Landed A.{NL}"), note("open", f"text{NL}Landed B.{NL}"))
           and "Landed B." in merge_note(nb, note("open", f"text{NL}Landed A.{NL}"), note("open", f"text{NL}Landed B.{NL}")))
    expect("note: a frontmatter key both changed differently is a conflict (None); a note without frontmatter too",
           merge_note(nb, note("landed", ""), note("retired", "")) is None and merge_note("x", "y", "z") is None)

    api = lambda *lines: "".join(ln + NL for ln in lines)    # noqa: E731
    hdr = "#nullable enable"
    expect("publicapi: lines each side added are both kept, where git's union merge (the premise) kept a symbol both "
           "sides added at different places twice (kb/Work PB3694)",
           merge_symbol_set(api(hdr, "A.X", "A.Z"), api(hdr, "A.X", "A.Y", "A.Z"), api(hdr, "A.W", "A.X", "A.Z"))
           == api(hdr, "A.W", "A.X", "A.Y", "A.Z")
           and merge_symbol_set(api(hdr, "A.X", "A.Z"), api(hdr, "A.D", "A.X", "A.Z"), api(hdr, "A.X", "A.Z", "A.D"))
           == api(hdr, "A.D", "A.X", "A.Z")
           and (union_text(api(hdr, "A.X", "A.Z"), api(hdr, "A.D", "A.X", "A.Z"), api(hdr, "A.X", "A.Z", "A.D")) or "").count("A.D") == 2)
    expect("publicapi: a line one side removed is gone, though the other side added a line beside it (where git's union "
           "merge kept it)",
           merge_symbol_set(api(hdr, "A.X", "A.Y", "A.Z"), api(hdr, "A.X", "A.Z", "A.G"), api(hdr, "A.X", "A.Y", "A.N", "A.Z", "A.G"))
           == api(hdr, "A.G", "A.N", "A.X", "A.Z")
           and "A.Y" in (union_text(api(hdr, "A.X", "A.Y", "A.Z"), api(hdr, "A.X", "A.Z", "A.G"),
                                    api(hdr, "A.X", "A.Y", "A.N", "A.Z", "A.G")) or "")
           and merge_symbol_set(api(hdr, "A.X", "A.Y"), api(hdr, "A.X", "A.Y"), api(hdr, "A.X")) == api(hdr, "A.X"))
    expect("publicapi: the header stays first and the rest is sorted (ordinal: a *REMOVED* line first, upper case before "
           "lower), LF-terminated, blank lines and CRs dropped, and a merge is never declined",
           merge_symbol_set(api(hdr, "b.B", "A.A"), api(hdr, "b.B", "A.A", "*REMOVED*Z.Z", "B.a"), api("", "c.C", hdr, "A.A", "b.B" + chr(13)))
           == api(hdr, "*REMOVED*Z.Z", "A.A", "B.a", "b.B", "c.C")
           and merge_symbol_set("", "", "") == "")
    expect("publicapi: the same line added by both is one line",
           merge_symbol_set(api(hdr, "A.X"), api(hdr, "A.N", "A.X"), api(hdr, "A.X", "A.N")) == api(hdr, "A.N", "A.X"))

    shipped, unshipped = "src/L/PublicAPI.Shipped.txt", "src/L/PublicAPI.Unshipped.txt"
    expect("publicapi: baseline_duplicates names a symbol listed twice in one file and a plain line in both Shipped and "
           "Unshipped of one directory (RS0025, kb/Work PB3694); a `*REMOVED*X` beside the Shipped X, one symbol in two "
           "directories, a repeated `#` header line, blank lines and a CR are no duplicate",
           baseline_duplicates({shipped: api(hdr, "A.A", "B.B"), unshipped: api(hdr, "C.C", "C.C", "A.A", "*REMOVED*B.B")})
           == [f"{shipped} and {unshipped}: A.A is listed in both", f"{unshipped}: C.C is listed twice"]
           and baseline_duplicates({shipped: api(hdr, "A.A"), unshipped: api(hdr, "*REMOVED*A.A", "B.B", "", "B.A" + chr(13)),
                                    "src/M/PublicAPI.Shipped.txt": api(hdr, "A.A", "B.B")}) == []
           and baseline_duplicates({unshipped: api(hdr, "B.A", "B.A" + chr(13))}) == [f"{unshipped}: B.A is listed twice"]
           and baseline_duplicates({}) == [])
    expect("publicapi: is_symbol_baseline reads the SYMBOL_SET_DRIVER row's pattern, so a baseline anywhere under src/ is one "
           "and a tests/ copy or a backup is not",
           is_symbol_baseline("src/WiseOwl.Cobol.Runtime/PublicAPI.Unshipped.txt") and is_symbol_baseline("src/PublicAPI.Shipped.txt")
           and not is_symbol_baseline("tests/a/PublicAPI.Shipped.txt") and not is_symbol_baseline("src/a/PublicAPI.Shipped.txt.bak")
           and any(d.name == SYMBOL_SET_DRIVER for d in DRIVERS))

    lines = lambda *ls: NL.join(ls) + NL    # noqa: E731
    twelve = [f"l{i}" for i in range(1, 13)]
    hunks = conflict_hunks(lines(*twelve), lines("l1", "ours-ins", *twelve[1:8], "l9-ours", *twelve[9:]),
                           lines("l1", "theirs-ins", *twelve[1:8], "l9-theirs", *twelve[9:]))
    expect("hunks: two insertions at one place are an INSERTION hunk (empty base section), two rewrites of one line a "
           "REVISION hunk; a clean merge has none, a marker-like document line is content",
           hunks is not None and [hunk_kind(h) for h in hunks] == ["insertion", "revision"]
           and hunks[0] == (["ours-ins"], [], ["theirs-ins"]) and hunks[1] == (["l9-ours"], ["l9"], ["l9-theirs"])
           and conflict_hunks(lines(*twelve), lines(*twelve), lines(*twelve, "l13")) == []
           and conflict_hunks(lines("a", "=======", "b"), lines("a", "=======", "b1"), lines("a", "=======", "b2"))
           == [(["b1"], ["b"], ["b2"])])

    # The whole path through git: a scratch repository whose two branches each add an entry to a keyed file.
    root = scratch_dir("merge-drivers-selftest-")
    try:
        scratch = ScratchRepo.init(root / "repo")
        repo = scratch.path
        (repo / ".gitattributes").write_text("d.json merge=cobol-keyed-object" + NL + "kb/Work/PB*.md merge=cobol-work-note" + NL
                                             + "src/**/PublicAPI.*.txt merge=cobol-keyed-publicapi" + NL, encoding="utf-8", newline=NL)
        (repo / "d.json").write_text(obj(m("a", "85"), m("c", "85")), encoding="utf-8", newline=NL)
        baseline = repo / "src" / "Lib" / "PublicAPI.Unshipped.txt"
        baseline.parent.mkdir(parents=True)
        baseline.write_text(api(hdr, "A.X", "A.Y", "A.Z"), encoding="utf-8", newline=NL)
        (repo / "kb" / "Work").mkdir(parents=True)
        (repo / "kb" / "Work" / "PB1.md").write_text(nb, encoding="utf-8", newline=NL)
        scratch.git("add", "-A")
        scratch.git("commit", "-q", "-m", "base")
        scratch.git("switch", "-q", "-c", "topic")
        (repo / "d.json").write_text(obj(m("a", "85"), m("c", "85"), m("ours", "85")), encoding="utf-8", newline=NL)
        (repo / "kb" / "Work" / "PB1.md").write_text(note("landed", f"text{NL}Landed A.{NL}"), encoding="utf-8", newline=NL)
        baseline.write_text(api(hdr, "A.D", "A.X", "A.Z", "A.G"), encoding="utf-8", newline=NL)
        scratch.git("commit", "-q", "-am", "topic")
        scratch.git("switch", "-q", "main")
        (repo / "d.json").write_text(obj(m("a", "85"), m("c", "85"), m("theirs", "85")), encoding="utf-8", newline=NL)
        (repo / "kb" / "Work" / "PB1.md").write_text(nb + f"Main note.{NL}", encoding="utf-8", newline=NL)
        baseline.write_text(api(hdr, "A.X", "A.Y", "A.N", "A.Z", "A.G", "A.D"), encoding="utf-8", newline=NL)
        scratch.git("commit", "-q", "-am", "main")
        scratch.git("switch", "-q", "topic")
        plain = scratch.run("merge", "--no-commit", "--no-ff", "main")
        scratch.run("merge", "--abort")
        expect("git: WITHOUT the drivers the same merge conflicts (the premise)", plain.returncode != 0)
        plain_tree = scratch.run("merge-tree", "--write-tree", "--name-only", "--no-messages", "HEAD", "main")
        expect("conflicts: the dry run lists nothing where the drivers merge, though a plain merge-tree names d.json",
               merge_conflicts(repo, "main") == [] and "d.json" in plain_tree.stdout.splitlines()[1:],
               f"{merge_conflicts(repo, 'main')} {plain_tree.stdout!r}")
        expect("conflicts: a ref git cannot read is None (git cannot say), never []",
               merge_conflicts(repo, "refs/heads/no-such-branch") is None)
        files = merge(repo, "main", "Merge main (self-test)")
        merged = json.loads((repo / "d.json").read_text(encoding="utf-8")) if files.clean else {}
        body = (repo / "kb" / "Work" / "PB1.md").read_text(encoding="utf-8")
        expect("git: merge() brings main in through the drivers: both entries, the status flip and both note paragraphs",
               files.clean and set(merged) == {"a", "c", "ours", "theirs"} and "status: landed" in body
               and "Landed A." in body and "Main note." in body and len(scratch.git("log", "--merges", "--format=%h").split()) == 1,
               f"{files} {sorted(merged)} {body!r}")
        expect("git: a public-API baseline both branches changed merges through its driver as a set: the symbol both "
               "added once, the line one side removed gone, sorted",
               files.clean and baseline.read_text(encoding="utf-8") == api(hdr, "A.D", "A.G", "A.N", "A.X", "A.Z"),
               repr(baseline.read_text(encoding="utf-8")))
        expect("git: merge-tree with config_args reproduces the merge's tree (the review's coverage rule reads it)",
               git(repo, "merge-tree", "--write-tree", "HEAD^1", "HEAD^2", attributes_of="HEAD^1").stdout.split()[:1]
               == [scratch.git("rev-parse", "HEAD^{tree}")])
        expect("tree: merged_tree is that merge's tree, the one definition both readers share (kb/Work PB3713); a ref git "
               "cannot read, and a merge a driver declines, are None",
               merged_tree(repo, ["HEAD^1", "HEAD^2"]) == scratch.git("rev-parse", "HEAD^{tree}")
               and merged_tree(repo, ["HEAD", "refs/heads/no-such-branch"]) is None)
        # A real conflict: both branches change the same entry differently.
        scratch.git("switch", "-q", "-c", "left", "HEAD")
        (repo / "d.json").write_text(obj(m("a", "2002"), m("c", "85"), m("ours", "85"), m("theirs", "85")), encoding="utf-8", newline=NL)
        scratch.git("commit", "-q", "-am", "left")
        scratch.git("switch", "-q", "-c", "right", "HEAD~1")
        (repo / "d.json").write_text(obj(m("a", "2014"), m("c", "85"), m("ours", "85"), m("theirs", "85")), encoding="utf-8", newline=NL)
        scratch.git("commit", "-q", "-am", "right")
        tip = scratch.git("rev-parse", "HEAD")
        expect("conflicts: a keyed file whose driver DECLINES (one entry both sides changed differently) is listed by "
               "the dry run, a real conflict (kb/Work PB3640 review R1-1), and the worktree is untouched",
               merge_conflicts(repo, "left") == ["d.json"] and not scratch.git("status", "--porcelain"),
               str(merge_conflicts(repo, "left")))
        files = merge(repo, "left", "Merge left (self-test)")
        expect("git: an entry both sides changed differently is a conflict naming the file; the merge is aborted, the branch unchanged",
               files.conflicts == ("d.json",) and not files.in_progress and files.incomplete is None
               and scratch.git("rev-parse", "HEAD") == tip and not scratch.git("status", "--porcelain"),
               f"{files}")
        kept = merge(repo, "left", "Merge left (self-test)", keep=True)
        in_progress = scratch.run("rev-parse", "-q", "--verify", "MERGE_HEAD").returncode == 0
        marked = "<<<<<<<" in (repo / "d.json").read_text(encoding="utf-8")
        scratch.run("merge", "--abort")
        expect("git: keep leaves a conflicting merge IN PROGRESS with its markers, for a finisher to resolve",
               kept.conflicts == ("d.json",) and kept.in_progress and in_progress and marked and scratch.git("rev-parse", "HEAD") == tip, f"{kept}")
        # kb/Work PB3713, the branch survey's LANDED proof: main already holds the topic's entry (another carrier put it
        # there) beside one of its own, so a PLAIN merge-tree conflicts in the keyed file, while the drivers merge it to
        # main's own tree, which is what makes the branch LANDED
        lr = ScratchRepo.init(root / "landed")
        lr.commit("base", {"d.json": obj(m("a", "85"), m("c", "85")), ".gitattributes": f"d.json merge=cobol-keyed-object{NL}"})
        lr.git("switch", "-q", "-c", "topic")
        lr.commit("topic", {"d.json": obj(m("a", "85"), m("c", "85"), m("x", "85"))})
        lr.git("switch", "-q", "main")
        lr.commit("main", {"d.json": obj(m("a", "85"), m("c", "85"), m("y", "85"), m("x", "85"))})
        expect("tree: an entry main already holds beside its own merges to main's tree through the drivers, where a plain "
               "merge-tree conflicts (the premise)",
               lr.run("merge-tree", "--write-tree", "main", "topic").returncode != 0
               and merged_tree(lr.path, ["main", "topic"]) == lr.git("rev-parse", "main^{tree}"))
        # kb/Work PB3723, THE QUEUE SIMULATED: e1 and e2 edit one line of a.txt, e3 another file, e4 that line again, e6 a
        # line of c.txt that main changed since the branches were cut. The queue removes an entry that conflicts on the
        # entries ahead that STAY, so e2 conflicts with e1, e3 stays, e4 conflicts with e1 (e2 is gone), e6 with main alone;
        # a head this repository lacks is unknown, never a conflict.
        qr = ScratchRepo.init(root / "queue")
        qr.commit("base", {"a.txt": f"1{NL}2{NL}3{NL}", "b.txt": f"1{NL}2{NL}3{NL}", "c.txt": f"1{NL}2{NL}3{NL}"})
        for name, path, text in (("e1", "a.txt", f"1{NL}x{NL}3{NL}"), ("e2", "a.txt", f"1{NL}y{NL}3{NL}"),
                                 ("e3", "b.txt", f"1{NL}b{NL}3{NL}"), ("e4", "a.txt", f"1{NL}z{NL}3{NL}"),
                                 ("e6", "c.txt", f"1{NL}n{NL}3{NL}")):
            qr.git("switch", "-q", "-c", name, "main")
            qr.commit(name, {path: text})
            qr.git("switch", "-q", "main")
        qr.commit("main moves", {"c.txt": f"1{NL}m{NL}3{NL}"})
        heads = {n: qr.git("rev-parse", n) for n in ("e1", "e2", "e3", "e4", "e6")}
        order = [(n, heads[n]) for n in ("e1", "e2", "e3", "e4", "e6")] + [("e5", "0" * 40)]
        foreseen = foresee_queue(qr.path, "main", order)
        expect("queue: an entry that conflicts on the entries ahead that stay is foreseen, with the entry it conflicts with "
               "(e2 and e4 with e1, the removed e2 not counted for e4), main alone when none ahead does (e6), and a clean "
               "entry or an unreadable head is not",
               foreseen == {"e2": Foreseen(("a.txt",), ("e1",)), "e4": Foreseen(("a.txt",), ("e1",)),
                            "e6": Foreseen(("c.txt",), ())}, str(foreseen))
        expect("queue: the simulation touches no worktree, and one clean entry of the queue is foreseen as nothing",
               not qr.git("status", "--porcelain") and foresee_queue(qr.path, "main", [("e2", heads["e2"])]) == {},
               qr.git("status", "--porcelain"))
        # A file the keyed merger declines (both sides changed one entry) but git's line merge merges cleanly (the changes are
        # on lines apart, as a plain `git merge` would merge them) merges: no conflict, no markers (review finding R1-1).
        fat = lambda p, s: (f'{{{NL} "x": {{{NL}  "p": "{p}",{NL}  "q": "q",{NL}  "r": "r",{NL}  "s": "{s}"{NL} }}{NL}}}{NL}')    # noqa: E731
        fr = ScratchRepo.init(root / "fallback")
        fr.commit("base", {"d.json": fat("1", "4"), ".gitattributes": f"d.json merge=cobol-keyed-object{NL}"})
        fr.git("switch", "-q", "-c", "topic")
        fr.commit("topic", {"d.json": fat("1+", "4")})
        fr.git("switch", "-q", "main")
        fr.commit("main", {"d.json": fat("1", "4+")})
        fr.git("switch", "-q", "topic")
        plain_clean = fr.run("merge", "--no-commit", "--no-ff", "main")
        fr.run("merge", "--abort")
        files = merge(fr.path, "main", "Merge main (fallback self-test)")
        text = (fr.path / "d.json").read_text(encoding="utf-8")
        expect("git: an entry both sides changed, on lines apart, merges through git's own line merge exactly as a plain "
               "git merge does (the keyed merger declines, the fallback is clean): no conflict, no markers",
               plain_clean.returncode == 0 and files.clean and text == fat("1+", "4+") and not fr.git("status", "--porcelain"),
               f"{plain_clean.returncode} {files} {text!r}")
        # A branch cut BEFORE a driver existed: its .gitattributes still says merge=union for the baseline, and main switched
        # the attribute to the driver, whose first two-sided merge sorted the baseline. The registry's attributes decide (kb/Work
        # PB3727): a union of the sorted file with the branch's added line duplicates the lines main moved.
        old = ScratchRepo.init(root / "old-attributes")
        unsorted = api(hdr, "C.C", "A.A", "D.D", "B.B", "F.F", "E.E")
        old.commit("base", {"src/L/PublicAPI.Unshipped.txt": unsorted, ".gitattributes": f"src/**/PublicAPI.*.txt merge=union{NL}"})
        old.git("switch", "-q", "-c", "topic")
        old.commit("topic", {"src/L/PublicAPI.Unshipped.txt": api(hdr, "C.C", "A.A", "D.D", "N.N", "B.B", "F.F", "E.E")})
        old.git("switch", "-q", "main")
        old.commit("main", {"src/L/PublicAPI.Unshipped.txt": api(hdr, "A.A", "B.B", "C.C", "D.D", "E.E", "F.F"),
                            ".gitattributes": f"src/**/PublicAPI.*.txt merge=cobol-keyed-publicapi{NL}"})
        old.git("switch", "-q", "topic")
        union = union_text(unsorted, api(hdr, "C.C", "A.A", "D.D", "N.N", "B.B", "F.F", "E.E"), api(hdr, "A.A", "B.B", "C.C", "D.D", "E.E", "F.F")) or ""
        files = merge(old.path, "main", "Merge main (old attributes self-test)")
        text = (old.path / "src" / "L" / "PublicAPI.Unshipped.txt").read_text(encoding="utf-8")
        expect("git: a branch cut with the old attributes (merge=union) merges a baseline through the registry's driver: "
               "the set, sorted, no symbol twice (the branch's own union would duplicate the lines main moved)",
               len(union.split()) != len(set(union.split())) and files.clean
               and text == api(hdr, "A.A", "B.B", "C.C", "D.D", "E.E", "F.F", "N.N")
               and is_drivers_merge(old.path, [old.git("rev-parse", "HEAD^1"), old.git("rev-parse", "HEAD^2")],
                                    old.git("rev-parse", "HEAD^{tree}")),
               f"{files} {text!r} {union!r}")
        # `ref` already inside the branch: nothing to merge, as a bare `git merge` answers "Already up to date" (R1-2).
        before = fr.git("rev-parse", "HEAD")
        again = merge(fr.path, "main", "Merge main (again)")
        older = merge(fr.path, "main~1", "Merge main~1 (ancestor)")
        expect("git: a ref the branch already contains is no conflict and no new commit (a resumed agent's first merge step)",
               again.clean and older.clean and fr.git("rev-parse", "HEAD") == before, f"{again} {older}")
        # The history replay (kb/Work PB3643): a recorded merge whose branches each inserted at one place and each rewrote
        # one line, resolved by hand, counts as one conflicted merge with one hunk of each kind; a merge that took clean
        # (one side only) is no conflict.
        hr = ScratchRepo.init(root / "history")
        hr.commit("base", {"d.md": lines(*twelve), "e.md": lines(*twelve)})
        hr.git("switch", "-q", "-c", "topic")
        hr.commit("topic", {"d.md": lines("l1", "ours-ins", *twelve[1:8], "l9-ours", *twelve[9:]), "e.md": lines(*twelve, "x")})
        hr.git("switch", "-q", "main")
        hr.commit("main", {"d.md": lines("l1", "theirs-ins", *twelve[1:8], "l9-theirs", *twelve[9:])})
        hr.git("switch", "-q", "topic")
        hr.run("merge", "--no-ff", "-m", "Merge main", "main")
        hr.commit("Merge main (resolved by hand)", {"d.md": lines(*twelve)})
        got = history_hunks(hr.path, ["d.md", "e.md"])
        expect("hunks: the history replay counts a merge both parents changed, whether git's line merge conflicted, and "
               "its hunks by kind; a file only one parent changed is not counted",
               got["d.md"] == {"both-changed": 1, "conflicted": 1, "insertion": 1, "revision": 1, "unreadable": 0}
               and got["e.md"]["both-changed"] == 0, f"{got}")
        # A merge that took one side whole (`-s ours`) is TREESAME to that parent, which git's default history simplification
        # drops from `git log -- <path>` (review finding R1-1): the replay must still count it, and the count for a file
        # must not change with the other paths asked about.
        ho = ScratchRepo.init(root / "history-ours")
        ho.commit("base", {"f.md": lines(*twelve), "g.md": lines(*twelve)})
        ho.git("switch", "-q", "-c", "topic")
        ho.commit("topic", {"f.md": lines("l1", "l2-ours", *twelve[2:])})
        ho.git("switch", "-q", "main")
        ho.commit("main", {"f.md": lines("l1", "l2-theirs", *twelve[2:]), "g.md": lines(*twelve, "y")})
        ho.git("switch", "-q", "topic")
        ho.run("merge", "--no-ff", "-s", "ours", "-m", "Merge main (took ours whole)", "main")
        alone, together = history_hunks(ho.path, ["f.md"])["f.md"], history_hunks(ho.path, ["f.md", "g.md"])["f.md"]
        expect("hunks: a merge that took one parent's file whole is counted, and a file's counts do not depend on the other "
               "paths asked about",
               alone == together == {"both-changed": 1, "conflicted": 1, "insertion": 0, "revision": 1, "unreadable": 0},
               f"{alone} {together}")
        # The ABI pin through git: both branches raise the major; the merge adds them and settles before its commit.
        pr = ScratchRepo.init(root / "pin")
        pin = lambda major, h: f"runtime-major {major}{NL}runtime-hash {h}{NL}call-abi 2{NL}boundary-hash {h}{NL}"    # noqa: E731
        files_at = lambda major, h: {abi_pin.PIN_PATH: pin(major, h), abi_pin.PROPS_PATH: f"<Version>{major}.0.0</Version>{NL}",    # noqa: E731
                                     abi_pin.CALL_ABI_PATH: f"public const int CallAbi = 2;{NL}",
                                     ".gitattributes": f"{abi_pin.PIN_PATH} merge=cobol-abi-pin{NL}"}
        pr.commit("base", files_at(17, "A0"))
        pr.git("switch", "-q", "-c", "topic")
        pr.commit("topic", files_at(18, "A1"))
        pr.git("switch", "-q", "main")
        pr.commit("main", files_at(18, "B1"))
        pr.git("switch", "-q", "topic")

        def fake_measure(r: pathlib.Path) -> abi_pin.Failure | None:        # the test's write mode
            (r / abi_pin.PIN_PATH).write_bytes(abi_pin.with_values((r / abi_pin.PIN_PATH).read_text(encoding="utf-8"), {
                "runtime-hash": "MEASURED", "boundary-hash": "MEASURED"}).encode("utf-8"))
            return None
        no_sdk = abi_pin.Failure(abi_pin.MERGE_BLOCKED, "the measuring run could not start: no SDK")
        broken = abi_pin.Failure(abi_pin.MERGE_FAILED, "the measuring run exited 1")
        gone = merge(pr.path, "main", "Merge main (pin self-test)", measure=lambda r: no_sdk)
        expect("pin: a settle that fails aborts the merge, the branch unchanged, and is NOT a conflict: the merge answers "
               "with the settle's own Failure and its class (kb/Work PB3673: it used to name the pin as a conflicted file, "
               "and the landing sent a finisher to resolve a merge with no conflict)",
               gone.incomplete == no_sdk and gone.conflicts == () and not gone.clean and not gone.in_progress
               and pr.git("log", "--format=%s", "-1") == "topic" and not pr.git("status", "--porcelain"),
               f"{gone}")
        stuck = merge(pr.path, "main", "Merge main (pin self-test)", measure=lambda r: broken, keep=True)
        left = pr.run("rev-parse", "-q", "--verify", "MERGE_HEAD").returncode == 0
        pr.run("merge", "--abort")
        pr.git("checkout", "HEAD", "--", *abi_pin.OWNED)
        expect("pin: keep (a finisher) leaves a merge whose pin could not be settled IN PROGRESS, for `abi_pin.py settle "
               "--stage` once the cause is removed, and still answers with the Failure, not a conflict",
               stuck.incomplete == broken and stuck.conflicts == () and stuck.in_progress and left
               and pr.git("log", "--format=%s", "-1") == "topic" and not pr.git("status", "--porcelain"), f"{stuck} {left}")
        done = merge(pr.path, "main", "Merge main (pin self-test)", measure=fake_measure)
        pinned = abi_pin.parse((pr.path / abi_pin.PIN_PATH).read_text(encoding="utf-8"))
        expect("pin: both branches raised the major 17 -> 18: the merge makes it 19 (the counters add), settles the "
               "props and the pin BEFORE the one merge commit, and leaves the tree clean",
               done.clean and pinned is not None and pinned["runtime-major"] == "19" and pinned["runtime-hash"] == "MEASURED"
               and "<Version>19.0.0</Version>" in (pr.path / abi_pin.PROPS_PATH).read_text(encoding="utf-8")
               and len(pr.git("log", "--merges", "--format=%h").split()) == 1 and not pr.git("status", "--porcelain"),
               f"{done} {pinned}")
        parents, tree = [pr.git("rev-parse", "HEAD^1"), pr.git("rev-parse", "HEAD^2")], pr.git("rev-parse", "HEAD^{tree}")
        pr.commit("hand edit", {"extra.txt": "x"})
        expect("coverage: is_drivers_merge accepts the merge with its pin settled, and refuses a tree with any other file "
               "changed (a hand resolution) — the review's coverage rule for a merge of main",
               is_drivers_merge(pr.path, parents, tree)
               and not is_drivers_merge(pr.path, parents, pr.git("rev-parse", "HEAD^{tree}")))
        p2 = ScratchRepo.init(root / "pin-prose")
        prose_pin = f"# the hashes read {abi_pin.UNMEASURED} and are measured on the merged tree{NL}" + pin(17, "A0")
        p2.commit("base", {**files_at(17, "A0"), abi_pin.PIN_PATH: prose_pin})
        p2.git("switch", "-q", "-c", "topic")
        p2.commit("topic", {"t.txt": "t"})
        p2.git("switch", "-q", "main")
        p2.commit("main", {"m.txt": "m"})
        p2.git("switch", "-q", "topic")
        p2.git("merge", "-q", "--no-commit", "--no-ff", "main")
        (p2.path / abi_pin.PROPS_PATH).write_bytes(f"<Version>99.0.0</Version>{NL}".encode("utf-8"))
        p2.git("add", "-A")
        p2.git("commit", "-q", "-m", "hand-resolved merge")
        expect("coverage: a merge whose pin is MEASURED (its header names UNMEASURED only in prose) and whose ABI files "
               "were edited by hand is not the drivers' merge (kb/Work PB3671 review R1-1)",
               not is_drivers_merge(p2.path, [p2.git("rev-parse", "HEAD^1"), p2.git("rev-parse", "HEAD^2")],
                                    p2.git("rev-parse", "HEAD^{tree}")))
        logs = root / "landing-logs"
        logs.mkdir()
        (logs / "land-lane-1-x.log").write_text(
            f"land lane: attempt 1 ended exit 4{NL}"
            + verdict_line.render(verdict_line.conflict_outcome(["conformance-suite/wiseowl/diagnostics.json", "src/A.cs"]), "b")
            + NL, encoding="utf-8", newline="\n")
        counts, read = conflict_report(logs)
        expect("conflicts: the landing logs' CONFLICT lines name their files, and a driver's file is told from the rest",
               counts == {"conformance-suite/wiseowl/diagnostics.json": 1, "src/A.cs": 1} and read == 1
               and covered_by_driver("conformance-suite/wiseowl/diagnostics.json") and covered_by_driver("kb/Work/PB3610.md")
               and not covered_by_driver("src/A.cs"))
        expect("patterns: a driver's pattern names a path as git does: `**/` spans zero or more directories, `*` and `?` "
               "stay within one, a pattern without `/` matches the last component at any depth",
               pattern_matches("src/WiseOwl.Cobol.Runtime/PublicAPI.Unshipped.txt", "src/**/PublicAPI.*.txt")
               and pattern_matches("src/a/b/PublicAPI.Shipped.txt", "src/**/PublicAPI.*.txt")
               and pattern_matches("src/PublicAPI.Shipped.txt", "src/**/PublicAPI.*.txt")
               and not pattern_matches("tests/a/PublicAPI.Shipped.txt", "src/**/PublicAPI.*.txt")
               and not pattern_matches("src/a/PublicAPI.Shipped.txt.bak", "src/**/PublicAPI.*.txt")
               and pattern_matches("kb/Work/PB3727.md", "kb/Work/PB*.md") and not pattern_matches("kb/Work/x/PB1.md", "kb/Work/PB*.md")
               and pattern_matches("a/b/x.nupkg", "*.nupkg") and pattern_matches("kb/Work/PB1.md", "kb/Work/PB?.md")
               and pattern_matches("d/x/y", "d/**") and pattern_matches("v1.md", "v[0-9].md") and not pattern_matches("v1.md", "v[!0-9].md")
               and covered_by_driver("src/WiseOwl.Cobol.Runtime/PublicAPI.Shipped.txt")
               # kb/Work PB3748: covered_by_driver asks pattern_matches, which anchors a pattern with a `/` at the root, so a
               # path that only ENDS in a driver's pattern is not covered (PurePosixPath.match matched from the right)
               and not covered_by_driver("x/kb/Work/PB1.md") and not covered_by_driver("x/src/WiseOwl.Cobol.Runtime/RuntimeAbi.pin")
               and not covered_by_driver("kb/Work/sub/PB1.md") and covered_by_driver("kb/Work/PB1.md"))
        # kb/Work PB3825: a new driver lands alone. `main` here stands for origin/main (the function's `main` argument).
        api = "src/a/PublicAPI.Unshipped.txt"
        api_main = f"#nullable enable{NL}b{NL}a{NL}"
        nd = ScratchRepo.init(root / "new-driver")
        nd.commit("main", {".gitattributes": f"src/**/PublicAPI.*.txt merge=union{NL}conformance-suite/wiseowl/diagnostics.json merge=cobol-keyed-object{NL}",
                           api: api_main, "conformance-suite/wiseowl/diagnostics.json": f"{{{NL}}}{NL}", "other.txt": "o"})
        nd.git("switch", "-q", "-c", "topic")
        nd.commit("topic: the new driver, and the baseline sorted too", {
            ".gitattributes": f"src/**/PublicAPI.*.txt merge=cobol-keyed-publicapi{NL}conformance-suite/wiseowl/diagnostics.json merge=cobol-keyed-object{NL}",
            api: f"#nullable enable{NL}a{NL}b{NL}"})
        found, why = new_driver_files(nd.path, "main")
        expect("new driver: a branch that gives a pattern a driver main's attributes do not yet give it AND changes a file "
               "the pattern governs is refused, naming the file, the driver and the way out (the PB3727 bounce)",
               why is None and len(found) == 1 and found[0].startswith(f"{api}: ") and "cobol-keyed-publicapi" in found[0]
               and "'union'" in found[0] and "a new driver lands alone" in found[0] and f"git checkout main -- {api}" in found[0],
               f"{found} {why}")
        nd.commit("topic: the baseline back to main's bytes", {api: api_main})
        expect("new driver: the same file back at main's bytes passes (the file is the one main has, so git takes main's side "
               "and runs no driver), though the branch still adds the driver",
               new_driver_files(nd.path, "main") == ([], None))
        nd.commit("topic: a file whose driver main already assigns, and a file no driver governs",
                  {"conformance-suite/wiseowl/diagnostics.json": f"{{{NL} \"t\": 1{NL}}}{NL}", "other.txt": "changed"})
        expect("new driver: a changed file whose driver main's attributes already assign, and a file no pattern governs, "
               "are no finding",
               new_driver_files(nd.path, "main") == ([], None))
        nd.git("switch", "-q", "main")
        nd.commit("main moves on: its own change to the baseline", {api: f"#nullable enable{NL}b{NL}a{NL}c{NL}"})
        nd.git("switch", "-q", "topic")
        expect("new driver: a main that moved since the branch was cut is not the branch's change (the diff starts at the "
               "merge base): the baseline the branch left alone but main changed is no finding",
               new_driver_files(nd.path, "main") == ([], None))
        # branch review R1-2: main re-points a pattern that a branch cut earlier still declares under the driver the base gave it
        rp = ScratchRepo.init(root / "repointed-driver")
        rp.commit("base", {".gitattributes": f"src/**/PublicAPI.*.txt merge=cobol-keyed-publicapi{NL}", api: api_main})
        rp.git("switch", "-q", "-c", "topic")
        rp.commit("topic: changes the baseline", {api: f"#nullable enable{NL}a{NL}b{NL}"})
        rp.git("switch", "-q", "main")
        rp.commit("main re-points the pattern to another driver", {".gitattributes": f"src/**/PublicAPI.*.txt merge=union{NL}"})
        rp.git("switch", "-q", "topic")
        expect("new driver: a pattern main re-pointed since the branch was cut (the base gave it the driver the branch still "
               "declares) is main's change, not a driver the branch adds: no finding",
               new_driver_files(rp.path, "main") == ([], None), str(new_driver_files(rp.path, "main")))
        expect("new driver: where the checkout has no such ref (CI's has no origin/main) the answer is a REASON, never a "
               "silent pass",
               new_driver_files(nd.path, "origin/main")[0] == [] and "origin/main is not in this checkout" in (new_driver_files(nd.path, "origin/main")[1] or ""))
        expect("check: the registry and this repository's .gitattributes agree",
               not check(HERE.parent), "; ".join(check(HERE.parent)))
        bad = root / "bad"
        (bad / "x").mkdir(parents=True)
        findings_empty = ScratchRepo.init(bad / "r")
        (findings_empty.path / ".gitattributes").write_text("zzz.json merge=cobol-keyed-object" + NL, encoding="utf-8", newline=NL)
        findings_empty.git("add", "-A")
        findings_empty.git("commit", "-q", "-m", "x")
        found = check(findings_empty.path)
        expect("check: a driver the attributes miss, an attribute no driver declares and a pattern naming no file are findings",
               any("does not say" in f for f in found) and any("no DRIVERS entry" in f for f in found)
               and any("names no tracked file" in f for f in found), "; ".join(found))
    finally:
        remove_tree(root)
    print(f"merge_drivers self-test: {'FAIL' if failures else 'PASS'}")
    return 1 if failures else 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split(NL)[0])
    ap.add_argument("--self-test", action="store_true")
    ap.add_argument("--check", action="store_true")
    ap.add_argument("--conflicts", metavar="LOGS_DIR", type=pathlib.Path,
                    help="count the files the landing logs in LOGS_DIR stopped on, and say which a driver covers")
    ap.add_argument("--hunks", metavar="PATH", nargs="+",
                    help="replay the merges in git history that touched PATH(s) and say whether their conflicts were "
                         "insertions (a driver could keep both) or revisions (a decision no merge can make)")
    sub = ap.add_subparsers(dest="cmd")
    d = sub.add_parser("driver")
    d.add_argument("name")
    d.add_argument("base", type=pathlib.Path)
    d.add_argument("ours", type=pathlib.Path)
    d.add_argument("theirs", type=pathlib.Path)
    d.add_argument("path", nargs="?")
    g = sub.add_parser("merge")
    g.add_argument("ref")
    g.add_argument("--message")
    g.add_argument("--keep", action="store_true", help="on a conflict leave the merge in progress for a finisher to resolve")
    g.add_argument("--repo", type=pathlib.Path, default=pathlib.Path.cwd())
    a = ap.parse_args()
    if a.self_test:
        return self_test()
    if a.conflicts:
        counts, read = conflict_report(a.conflicts)
        for f, n in counts.items():
            print(f"{n:4d}  {'covered by a driver' if covered_by_driver(f) else 'NOT covered     '}  {f}")
        print(f"{read} landing log(s) read, {sum(counts.values())} conflicted file(s)")
        return 0
    if a.hunks:
        for path, counts in history_hunks(HERE.parent, a.hunks).items():
            print(f"{path}: " + ", ".join(f"{n} {k}" for k, n in counts.items()))
        return 0
    if a.check:
        findings = check(HERE.parent)
        landing, unread = new_driver_files(HERE.parent)
        findings += landing
        for f in findings:
            print(f"  FINDING  {f}")
        if unread:
            print(f"  NOTE  'a new driver lands alone' was NOT CHECKED: {unread} (fetch origin, or run where main is present)")
        print(f"=== MERGE DRIVERS: {'RED' if findings else 'GREEN'} ===")
        return 1 if findings else 0
    if a.cmd == "driver":
        return run_driver(a.name, a.base, a.ours, a.theirs)
    if a.cmd == "merge":
        done = merge(a.repo, a.ref, a.message or f"Merge {a.ref}", keep=a.keep)
        if done.incomplete:
            print(f"NOT COMPLETED [{done.incomplete.cause}] — the merge was clean but " + done.incomplete.why.splitlines()[0] + "; "
                  + (f"the merge is left in progress: remove the cause, then `python scripts/abi_pin.py settle --stage`, "
                     f"`git add` and `git commit`" if done.in_progress else "the merge was aborted, the branch unchanged"))
            return EXIT_INCOMPLETE
        if done.conflicts and done.in_progress:
            print(f"CONFLICT in {', '.join(done.conflicts)} — resolve each against BOTH sides' intent; then, when "
                  f"{abi_pin.PIN_PATH} says UNMEASURED, `python scripts/abi_pin.py settle --stage`; then `git add` and `git commit`")
            return 1
        if done.conflicts:
            print(verdict_line.conflict_outcome(list(done.conflicts)))
            return 1
        return 0
    ap.print_help()
    return 2


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
