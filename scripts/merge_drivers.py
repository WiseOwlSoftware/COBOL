#!/usr/bin/env python3
"""merge_drivers.py — THE GIT MERGE DRIVERS FOR THE FILES TWO LANDINGS BOTH EDIT, and the one way main is merged in
(kb/Work PB3610; DESIGN-test-build-ci.md section 3.17.2 step 2).

    python scripts/merge_drivers.py merge <ref> [--message M] [--keep] [--repo DIR]   merge <ref> with the drivers, settle the ABI pin
    python scripts/merge_drivers.py driver <name> <base> <ours> <theirs> [<path>]   what git runs per conflicting file
    python scripts/merge_drivers.py --check        # .gitattributes and DRIVERS agree, every pattern names a tracked file
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
cannot be forgotten and no persistent per-checkout configuration can go stale.

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
  RUNTIME ABI PIN   src/Cobol.Net.Runtime/RuntimeAbi.pin (abi_pin.py): two branches that each raised a counter raise it
                    by both, and the hashes of the merged surface, which no merge can know, are marked UNMEASURED for
                    `abi_pin.py settle` to measure on the merged tree before the merge commit is made.
A conflict a driver cannot resolve falls back to git's own line merge of the three files, so the file the finisher opens
carries the usual conflict markers.

THE MERGE (`merge`): `git merge --no-commit` with the drivers, then `abi_pin.settle` when the pin says UNMEASURED, then
the merge commit. The settle writes only the files abi_pin.OWNED names; `prune_worktrees.mechanical` (the review's
coverage rule) accepts a merge whose tree differs from the drivers' own merge in those files and nowhere else.
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
from typing import Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

#: A merge function: (base, ours, theirs) texts -> the merged text, or None for a real conflict.
Merger = Callable[[str, str, str], "str | None"]
NL = chr(10)


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


def union_text(base: str, ours: str, theirs: str) -> str | None:
    """git's union merge of three texts: both sides' changes, a line both rewrote kept from both; None when git failed."""
    with tempfile.TemporaryDirectory(prefix="merge-union-") as d:
        paths = []
        for name, text in (("ours", ours), ("base", base), ("theirs", theirs)):
            p = pathlib.Path(d) / name
            p.write_bytes(text.encode("utf-8"))
            paths.append(str(p))
        r = subprocess.run(["git", "merge-file", "-p", "--union", *paths], capture_output=True)
    return r.stdout.decode("utf-8") if r.returncode == 0 else None


def merge_note(base: str, ours: str, theirs: str) -> str | None:
    sb, so, st = split_note(base), split_note(ours), split_note(theirs)
    if not (sb and so and st):
        return None
    merged, conflicts = merge_maps(sb[0], so[0], st[0])
    if conflicts:
        return None
    body = union_text(sb[1], so[1], st[1])
    return None if body is None else NL.join(["---", *(text for _, text in merged), body])


def _abi_pin_merger() -> Merger:
    import abi_pin  # noqa: PLC0415  (scripts/: the pin's own format and counters)
    return abi_pin.merge_text


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
    Driver("cobol-abi-pin", ("src/Cobol.Net.Runtime/RuntimeAbi.pin",), _abi_pin_merger,
           "the runtime major and call ABI counters add; the merged surface's hashes are measured, not merged"),
)


def python_posix() -> str:
    return pathlib.Path(sys.executable).as_posix()


def config_args() -> list[str]:
    """The `git -c merge.<name>.driver=…` arguments that make a merge, or a `merge-tree`, use every driver. The script is
    THIS checkout's: a branch cut before the drivers existed still merges through them."""
    script = pathlib.Path(__file__).resolve().as_posix()
    out: list[str] = []
    for d in DRIVERS:
        out += ["-c", f'merge.{d.name}.driver="{python_posix()}" "{script}" driver {d.name} %O %A %B %P']
    return out


def git(repo: pathlib.Path, *args: str, config: bool = False) -> subprocess.CompletedProcess:
    return subprocess.run(["git", *(config_args() if config else []), *args], cwd=repo, capture_output=True, text=True,
                          encoding="utf-8", errors="replace")


# ── the driver entry point ───────────────────────────────────────────────────────────────────────────────────────
def run_driver(name: str, base: pathlib.Path, ours: pathlib.Path, theirs: pathlib.Path) -> int:
    """What git runs for one conflicting file: the merged text replaces `ours`, exit 0; or the three-way line merge
    with its conflict markers does, exit 1."""
    driver = next((d for d in DRIVERS if d.name == name), None)
    if driver is None:
        print(f"merge_drivers: no driver named {name}", file=sys.stderr)
        return 2
    merged = driver.merger()(base.read_bytes().decode("utf-8"), ours.read_bytes().decode("utf-8"),
                             theirs.read_bytes().decode("utf-8"))
    if merged is not None:
        ours.write_bytes(merged.encode("utf-8"))
        return 0
    subprocess.run(["git", "merge-file", "-L", "ours", "-L", "base", "-L", "theirs", str(ours), str(base), str(theirs)],
                   capture_output=True)
    return 1


# ── the merge ────────────────────────────────────────────────────────────────────────────────────────────────────
def merge(repo: pathlib.Path, ref: str, message: str, measure: Callable[[pathlib.Path], str | None] | None = None,
          keep: bool = False) -> list[str] | None:
    """MERGE `ref` into the checked-out branch with every driver: None when the branch now contains it (the merge
    commit made, the ABI pin settled in it); else the files left in conflict, or the pin's path when it could not be
    settled, with the merge aborted and the branch unchanged. A branch that only needs `ref` fast-forwards. `measure`:
    abi_pin.settle's measurement (a self-test's stand-in for the build). `keep` (a finisher resolving a conflict): the merge
    is left in progress with its conflict markers instead of aborted; the finisher resolves, runs `abi_pin.py settle
    --stage` when the pin says UNMEASURED, and commits."""
    import abi_pin  # noqa: PLC0415
    if git(repo, "merge-base", "--is-ancestor", "HEAD", ref).returncode == 0:
        return None if git(repo, "merge", "--ff-only", ref).returncode == 0 else ["?"]
    if git(repo, "merge", "--no-commit", "--no-ff", ref, config=True).returncode != 0:
        files = git(repo, "diff", "--name-only", "--diff-filter=U").stdout.split()
        if not keep:
            git(repo, "merge", "--abort")
        return files or ["?"]
    if abi_pin.needs_settle(repo):
        err = abi_pin.settle(repo, measure=measure or abi_pin.run_measurement)
        if err:
            print(f"merge_drivers: the runtime ABI pin could not be settled: {err}", file=sys.stderr)
            git(repo, "merge", "--abort")
            git(repo, "checkout", "HEAD", "--", *abi_pin.OWNED)   # abort keeps the settle's own edits to files the merge changed
            return [abi_pin.PIN_PATH]
    done = git(repo, "commit", "--no-edit", "-m", message)
    if done.returncode != 0:
        git(repo, "merge", "--abort")
        return ["?"]
    return None


def is_drivers_merge(repo: pathlib.Path | str, parents: list[str], tree: str) -> bool:
    """Whether a two-parent commit whose tree is `tree` is what `merge` makes of its parents: the drivers' own merge of
    them (`git merge-tree`, which honors the drivers when given config_args), or that tree with the runtime ABI pin
    settled into it (the three files abi_pin.OWNED names, and only when the drivers left the pin UNMEASURED). This is the
    review's coverage rule for a merge of main (prune_worktrees.mechanical), so it must be exactly the merge's own
    definition: there is no other."""
    import abi_pin  # noqa: PLC0415
    r = git(pathlib.Path(repo), "merge-tree", "--write-tree", *parents, config=True)
    clean = r.stdout.split()[:1]
    if r.returncode != 0 or not clean:
        return False
    if clean[0] == tree:
        return True
    changed = git(pathlib.Path(repo), "diff-tree", "-r", "--name-only", clean[0], tree).stdout.split()
    pin = git(pathlib.Path(repo), "cat-file", "blob", f"{clean[0]}:{abi_pin.PIN_PATH}").stdout
    return bool(changed) and set(changed) <= set(abi_pin.OWNED) and abi_pin.UNMEASURED in pin


# ── the audit ────────────────────────────────────────────────────────────────────────────────────────────────────
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
        if not any(pathlib.PurePosixPath(t).match(p) for t in tracked if t):
            findings.append(f"the pattern {p} of a driver names no tracked file")
    return findings


CONFLICT_LINE = re.compile(r"^=== ENQUEUE: CONFLICT in (.+?) — merge aborted")


def conflict_files(log_text: str) -> list[str]:
    """The files the landing lane's logs name in their `=== ENQUEUE: CONFLICT in <files> — merge aborted …` lines."""
    return [f.strip() for ln in log_text.split(NL) if (m := CONFLICT_LINE.match(ln)) for f in m.group(1).split(",")]


def covered_by_driver(path: str) -> bool:
    return any(pathlib.PurePosixPath(path).match(p) for d in DRIVERS for p in d.patterns)


def conflict_report(logs: pathlib.Path) -> tuple[dict[str, int], int]:
    """({file: how many landing logs stopped on it}, logs read): the measurement the drivers' coverage is judged against.
    A file a driver does not cover and that is a keyed or measured catalogue is a driver to add; source code is a real
    conflict, which a finisher resolves."""
    counts: dict[str, int] = {}
    read = 0
    for log in sorted(logs.glob("land-lane-*.log")):
        read += 1
        for f in conflict_files(log.read_text(encoding="utf-8", errors="replace")):
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

    # The whole path through git: a scratch repository whose two branches each add an entry to a keyed file.
    root = scratch_dir("merge-drivers-selftest-")
    try:
        scratch = ScratchRepo.init(root / "repo")
        repo = scratch.path
        (repo / ".gitattributes").write_text("d.json merge=cobol-keyed-object" + NL + "kb/Work/PB*.md merge=cobol-work-note" + NL,
                                             encoding="utf-8", newline=NL)
        (repo / "d.json").write_text(obj(m("a", "85"), m("c", "85")), encoding="utf-8", newline=NL)
        (repo / "kb" / "Work").mkdir(parents=True)
        (repo / "kb" / "Work" / "PB1.md").write_text(nb, encoding="utf-8", newline=NL)
        scratch.git("add", "-A")
        scratch.git("commit", "-q", "-m", "base")
        scratch.git("switch", "-q", "-c", "topic")
        (repo / "d.json").write_text(obj(m("a", "85"), m("c", "85"), m("ours", "85")), encoding="utf-8", newline=NL)
        (repo / "kb" / "Work" / "PB1.md").write_text(note("landed", f"text{NL}Landed A.{NL}"), encoding="utf-8", newline=NL)
        scratch.git("commit", "-q", "-am", "topic")
        scratch.git("switch", "-q", "main")
        (repo / "d.json").write_text(obj(m("a", "85"), m("c", "85"), m("theirs", "85")), encoding="utf-8", newline=NL)
        (repo / "kb" / "Work" / "PB1.md").write_text(nb + f"Main note.{NL}", encoding="utf-8", newline=NL)
        scratch.git("commit", "-q", "-am", "main")
        scratch.git("switch", "-q", "topic")
        plain = scratch.run("merge", "--no-commit", "--no-ff", "main")
        scratch.run("merge", "--abort")
        expect("git: WITHOUT the drivers the same merge conflicts (the premise)", plain.returncode != 0)
        files = merge(repo, "main", "Merge main (self-test)")
        merged = json.loads((repo / "d.json").read_text(encoding="utf-8")) if files is None else {}
        body = (repo / "kb" / "Work" / "PB1.md").read_text(encoding="utf-8")
        expect("git: merge() brings main in through the drivers: both entries, the status flip and both note paragraphs",
               files is None and set(merged) == {"a", "c", "ours", "theirs"} and "status: landed" in body
               and "Landed A." in body and "Main note." in body and len(scratch.git("log", "--merges", "--format=%h").split()) == 1,
               f"{files} {sorted(merged)} {body!r}")
        expect("git: merge-tree with config_args reproduces the merge's tree (the review's coverage rule reads it)",
               git(repo, "merge-tree", "--write-tree", "HEAD^1", "HEAD^2", config=True).stdout.split()[:1]
               == [scratch.git("rev-parse", "HEAD^{tree}")])
        # A real conflict: both branches change the same entry differently.
        scratch.git("switch", "-q", "-c", "left", "HEAD")
        (repo / "d.json").write_text(obj(m("a", "2002"), m("c", "85"), m("ours", "85"), m("theirs", "85")), encoding="utf-8", newline=NL)
        scratch.git("commit", "-q", "-am", "left")
        scratch.git("switch", "-q", "-c", "right", "HEAD~1")
        (repo / "d.json").write_text(obj(m("a", "2014"), m("c", "85"), m("ours", "85"), m("theirs", "85")), encoding="utf-8", newline=NL)
        scratch.git("commit", "-q", "-am", "right")
        tip = scratch.git("rev-parse", "HEAD")
        files = merge(repo, "left", "Merge left (self-test)")
        expect("git: an entry both sides changed differently is a conflict naming the file; the merge is aborted, the branch unchanged",
               files == ["d.json"] and scratch.git("rev-parse", "HEAD") == tip and not scratch.git("status", "--porcelain"),
               f"{files}")
        kept = merge(repo, "left", "Merge left (self-test)", keep=True)
        in_progress = scratch.run("rev-parse", "-q", "--verify", "MERGE_HEAD").returncode == 0
        marked = "<<<<<<<" in (repo / "d.json").read_text(encoding="utf-8")
        scratch.run("merge", "--abort")
        expect("git: keep leaves a conflicting merge IN PROGRESS with its markers, for a finisher to resolve",
               kept == ["d.json"] and in_progress and marked and scratch.git("rev-parse", "HEAD") == tip, f"{kept}")
        # The ABI pin through git: both branches raise the major; the merge adds them and settles before its commit.
        import abi_pin  # noqa: PLC0415
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

        def fake_measure(r: pathlib.Path) -> str | None:        # the test's write mode
            (r / abi_pin.PIN_PATH).write_bytes(abi_pin.with_values((r / abi_pin.PIN_PATH).read_text(encoding="utf-8"), {
                "runtime-hash": "MEASURED", "boundary-hash": "MEASURED"}).encode("utf-8"))
            return None
        gone = merge(pr.path, "main", "Merge main (pin self-test)", measure=lambda r: "no SDK")
        expect("pin: a settle that fails aborts the merge and names the pin, the branch unchanged",
               gone == [abi_pin.PIN_PATH] and pr.git("log", "--format=%s", "-1") == "topic" and not pr.git("status", "--porcelain"),
               f"{gone}")
        done = merge(pr.path, "main", "Merge main (pin self-test)", measure=fake_measure)
        pinned = abi_pin.parse((pr.path / abi_pin.PIN_PATH).read_text(encoding="utf-8"))
        expect("pin: both branches raised the major 17 -> 18: the merge makes it 19 (the counters add), settles the "
               "props and the pin BEFORE the one merge commit, and leaves the tree clean",
               done is None and pinned is not None and pinned["runtime-major"] == "19" and pinned["runtime-hash"] == "MEASURED"
               and "<Version>19.0.0</Version>" in (pr.path / abi_pin.PROPS_PATH).read_text(encoding="utf-8")
               and len(pr.git("log", "--merges", "--format=%h").split()) == 1 and not pr.git("status", "--porcelain"),
               f"{done} {pinned}")
        parents, tree = [pr.git("rev-parse", "HEAD^1"), pr.git("rev-parse", "HEAD^2")], pr.git("rev-parse", "HEAD^{tree}")
        pr.commit("hand edit", {"extra.txt": "x"})
        expect("coverage: is_drivers_merge accepts the merge with its pin settled, and refuses a tree with any other file "
               "changed (a hand resolution) — the review's coverage rule for a merge of main",
               is_drivers_merge(pr.path, parents, tree)
               and not is_drivers_merge(pr.path, parents, pr.git("rev-parse", "HEAD^{tree}")))
        log = (f"land lane: attempt 1 ended exit 4{NL}=== ENQUEUE: CONFLICT in conformance-suite/wiseowl/diagnostics.json, "
               f"src/A.cs — merge aborted, branch unchanged (b) ==={NL}")
        expect("conflicts: the landing logs' CONFLICT lines name their files, and a driver's file is told from the rest",
               conflict_files(log) == ["conformance-suite/wiseowl/diagnostics.json", "src/A.cs"]
               and covered_by_driver("conformance-suite/wiseowl/diagnostics.json") and covered_by_driver("kb/Work/PB3610.md")
               and not covered_by_driver("src/A.cs"))
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


def abi_pin_path() -> str:
    import abi_pin  # noqa: PLC0415
    return abi_pin.PIN_PATH


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split(NL)[0])
    ap.add_argument("--self-test", action="store_true")
    ap.add_argument("--check", action="store_true")
    ap.add_argument("--conflicts", metavar="LOGS_DIR", type=pathlib.Path,
                    help="count the files the landing logs in LOGS_DIR stopped on, and say which a driver covers")
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
    if a.check:
        findings = check(HERE.parent)
        for f in findings:
            print(f"  FINDING  {f}")
        print(f"=== MERGE DRIVERS: {'RED' if findings else 'GREEN'} ===")
        return 1 if findings else 0
    if a.cmd == "driver":
        return run_driver(a.name, a.base, a.ours, a.theirs)
    if a.cmd == "merge":
        files = merge(a.repo, a.ref, a.message or f"Merge {a.ref}", keep=a.keep)
        if files and a.keep:
            print(f"CONFLICT in {', '.join(files)} — resolve each against BOTH sides' intent; then, when "
                  f"{abi_pin_path()} says UNMEASURED, `python scripts/abi_pin.py settle --stage`; then `git add` and `git commit`")
            return 1
        if files:
            print(f"CONFLICT in {', '.join(files)} — merge aborted, branch unchanged")
            return 1
        return 0
    ap.print_help()
    return 2


if __name__ == "__main__":
    sys.exit(main())
