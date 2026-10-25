#!/usr/bin/env python3
"""devlog.py — THE DEVLOG IS ONE FILE PER ENTRY under DEVLOG/, and this is its one tool (kb/Work PB3025;
docs/rearchitecture/DESIGN-test-build-ci.md section 3.17.4).

    python scripts/devlog.py render [--since NNNN] [--out FILE]   the descending narrative (README, then newest first)
    python scripts/devlog.py stamp <branch> [--coord DIR]        number and stamp the branch's pending entries as
                                                                   one; one commit
    python scripts/devlog.py check [--base origin/main] [--rev HEAD] the landing rule, on the trees of the range
    python scripts/devlog.py next                                  the highest entry number in DEVLOG/ (the allocator's floor)
    python scripts/devlog.py --self-test

THE SHAPE. `DEVLOG/README.md` is the front matter (what the log is, how an entry is written). Every entry is its own
file, `DEVLOG/NNNN-<slug>.md` (the number zero-padded to four digits, the slug from the title), whose FIRST LINE is its
header, `## Entry NNNN — YYYY-MM-DD HH:MM TZ — Title`, followed by the body. Entries are LF text like every tracked
file (kb/Work PB3029). The narrative a reader wants is RENDERED, never committed: `render` prints the README and then
every entry, newest (highest number) first, by concatenating the files byte for byte; `DEVLOG.rendered.md` is the
gitignored place to put it. Why one file per entry: two landings that both insert at the top of one file collide, and a
merge queue bounces the second (owner 2026-10-11, kb/Work PB3015 Q2: "Yes, one file per entry"); CLAUDE.md rule 6 is
unchanged in substance — one entry per landing on main, newest first, the only history.

THE HISTORY (every entry of the monolithic DEVLOG.md, migrated byte for byte on 2026-10-11; DEVLOG/README.md names
the range, written by the migration itself) is looser than the rule for new entries: early headers carry no time,
some a `:` after the date, twelve a double-encoded dash; 1191a-1191d carry a letter; and twenty numbers were used
twice (008-014, 171-185). Each file keeps its header exactly; a number used
twice is told apart by a letter in the FILE NAME (`0185b-…` renders above `0185a-…`, as the monolith ordered them), so
the render order is the name order and needs no list. A NEW entry is held to the strict header and a plain number.

THE FLOW. The implementer writes `DEVLOG/pending-<slug>.md` whose first line is `## <Title>` (no number, no stamp) and
whose body follows. `stamp <branch>` runs when the branch is enqueued: it allocates the number through
`scripts/orchestrator/alloc.py devlog` (so number order is STAMP order), renames the file to
`NNNN-<slug>.md`, completes the header with the number and the current stamp, and commits that in the branch's
worktree. A branch that merged an unlanded predecessor (a same-file successor, a review finisher) carries that
predecessor's pending entry too and lands it, so `stamp` folds every pending entry into the ONE new entry, oldest
first (one landing on main, one entry); a pending entry whose title is already a stamped entry of the tree (that
predecessor landed on its own, reviewed CLEAN) is dropped, never folded a second time (kb/Work PB3693). THE STAMP IS ALL
OR NOTHING (kb/Work PB3367): its git writes retry on `index.lock`, a failure puts the pending files back, resets the
index and prints git's own stderr (exit 3), and a stamp interrupted before its commit is completed by the next run.
A stamped entry keeps its number for good: GitHub orders the merge queue
AFTER the stamp (each pull request enters it when its own checks finish, and each merge group is built on every group
ahead of it), so a landing stamped earlier can reach main after one stamped later, and a bounced branch that lands
again lands under the number it was first given (kb/Work PB3123). `check` is the landing rule (owner 2026-10-08, kb/Work PB2605: it covers "Only commits landing on main"),
run where every landing passes and on every pull request and merge group in CI.

WHAT `check` REFUSES (exit 1), on the committed trees of <base> and <rev>, never an index or a working tree:
  - a tree that still has the monolithic DEVLOG.md, a `pending-` file (an unstamped entry), or a DEVLOG/ file that is
    neither README.md nor an entry name;
  - an entry whose first line is not an `## Entry` header, whose header number disagrees with its file name, or whose
    number (with its letter) another file also holds;
  - a landing that adds no NEW entry, or more than one (new = a header line the base's history does not carry);
  - a new entry without the strict header, with a letter, or whose number is not above every number of the tree it
    was STAMPED on (the first parent of the commit in <base>..<rev> that added its file, which is `stamp`'s; <base>'s
    own entries when no commit in the range added it). Never "above <base>'s newest": a merge group is built on the
    groups queued ahead of it, which may carry higher numbers stamped later (kb/Work PB3123; runs 38355538654 and
    38360587401 bounced on that rule). Uniqueness is the item above: no two files hold one number;
  - a landing that deletes an entry <base> had: an entry FILE of <base> missing from <rev>, or, when <base> still has
    the monolithic DEVLOG.md (the one landing that migrates the log), a header line of it that no entry of <rev>
    carries, counted with multiplicity (the history repeats some headers).
Exit 0: the landing carries its one entry. Exit 2: the range could not be read (NOT RUN, never a pass).
"""
from __future__ import annotations

import argparse
import collections
import contextlib
import datetime as dt
import io
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys
import time
import unicodedata

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parent
DIR = "DEVLOG"
README = "README.md"
MONOLITH = "DEVLOG.md"
DASH = "—"
FORMAT = f"## Entry NNNN {DASH} YYYY-MM-DD HH:MM TZ {DASH} Title"
ENTRY_NAME = re.compile(r"^(?P<num>\d{4,})(?P<sfx>[a-z]?)-(?P<slug>[a-z0-9]+(?:-[a-z0-9]+)*)\.md$")
PENDING_NAME = re.compile(r"^pending-(?P<slug>[a-z0-9]+(?:-[a-z0-9]+)*)\.md$")
HEADER = re.compile(r"^## Entry (?P<num>\d+)(?P<sfx>[a-z]?)(?![0-9A-Za-z])")
STAMPED = re.compile(rf"^## Entry (?P<num>\d+) {DASH} \d{{4}}-\d{{2}}-\d{{2}} \d{{2}}:\d{{2}} [A-Za-z][A-Za-z0-9+:-]* "
                     rf"{DASH} \S")
PENDING_HEADER = re.compile(r"^## (?!Entry\b)(?P<title>\S.*)$")
#: What may precede an old header's title: a separator (an em or en dash, a hyphen, a colon, or the twelve
#: double-encoded dashes of entries 1217-1228), a date, a time and a zone, another separator.
TITLE_PREFIX = re.compile(r"^\s*(?:[—–:-]|â\u0080\u0094)?\s*"
                          r"(?:\d{4}-\d{2}-\d{2}(?:\s+\d{2}:\d{2}(?:\s+[A-Za-z]{2,5})?)?)?\s*"
                          r"(?:[—–:-]|â\u0080\u0094)?\s*")
SLUG_MAX = 60


class RangeError(Exception):
    """A revision of the range could not be read (the check could not run: NOT RUN, never a pass)."""


# ── names, headers, slugs ────────────────────────────────────────────────────────────────────────────────────────
def slugify(title: str) -> str:
    """Lowercase ASCII words joined by '-', at most SLUG_MAX characters, cut at a word boundary; 'entry' if empty."""
    ascii_ = unicodedata.normalize("NFKD", title).encode("ascii", "ignore").decode("ascii").lower()
    slug = re.sub(r"[^a-z0-9]+", "-", ascii_).strip("-")
    if len(slug) > SLUG_MAX:
        cut = slug[:SLUG_MAX + 1]
        slug = cut[:cut.rfind("-")] if "-" in cut else slug[:SLUG_MAX]
    return slug or "entry"


def title_of(header: str) -> str:
    """The title of an `## Entry` header line, without its number, separators, date or stamp."""
    m = HEADER.match(header)
    rest = header[m.end():] if m else header
    return TITLE_PREFIX.sub("", rest, count=1).strip()


def entry_name(num: int, sfx: str, title: str) -> str:
    return f"{num:04d}{sfx}-{slugify(title)}.md"


def key_of(name: str) -> tuple[int, str] | None:
    """(number, letter) of an entry file name; None when the name is not an entry's."""
    m = ENTRY_NAME.match(name)
    return (int(m.group("num")), m.group("sfx")) if m else None


def first_line(text: str) -> str:
    return text.split("\n", 1)[0].rstrip("\r")


def entry_problems(name: str, text: str) -> list[str]:
    """What is wrong with one entry file (any entry, history included): its header and its name must agree."""
    head = first_line(text)
    m = HEADER.match(head)
    if not m:
        return [f"{DIR}/{name}: its first line is not an `## Entry` header ({head[:60]!r})"]
    num, sfx = key_of(name)  # the caller only passes entry names
    if int(m.group("num")) != num:
        return [f"{DIR}/{name}: the header says Entry {m.group('num')}{m.group('sfx')}, the file name {num}"]
    if m.group("sfx") and m.group("sfx") != sfx:
        return [f"{DIR}/{name}: the header's letter {m.group('sfx')!r} is not the file name's {sfx!r}"]
    return []


# ── reading a tree: the working tree (render, stamp, next) or a commit (check) ──────────────────────────────────
class GitError(Exception):
    """A git call failed: carries the command, its exit code and git's OWN stderr (kb/Work PB3367: the capture used to
    hide it, so a transient `index.lock` clash and a real fault read alike)."""


def _git(repo: pathlib.Path, *args: str, check: bool = False, inp: str | None = None) -> subprocess.CompletedProcess:
    r = subprocess.run(["git", "-C", str(repo), *args], capture_output=True, text=True, encoding="utf-8",
                       errors="replace", input=inp)
    if check and r.returncode != 0:
        raise GitError(f"git {' '.join(args)} exited {r.returncode}: {r.stderr.strip() or '(no stderr)'}")
    return r


#: The waits (seconds) between the attempts of a git call that met another git process's `index.lock`; the stamp's
#: writes retry on that one cause only (kb/Work PB3367: a landing lost a green branch to one transient lock clash).
GIT_LOCK_WAITS = (0.2, 0.5, 1.0, 2.0, 4.0)
_pause = time.sleep


def _git_write(repo: pathlib.Path, *args: str) -> subprocess.CompletedProcess:
    """A git call that WRITES the index or the history, retried while it fails on `index.lock` (another git process of
    the same worktree holds it); GitError with git's stderr when it still fails or fails for any other reason."""
    for wait in (*GIT_LOCK_WAITS, None):
        r = _git(repo, *args)
        if r.returncode == 0:
            return r
        if wait is None or "index.lock" not in r.stderr:
            break
        _pause(wait)
    raise GitError(f"git {' '.join(args)} exited {r.returncode}: {r.stderr.strip() or '(no stderr)'}")


def worktree_files(repo: pathlib.Path) -> dict[str, str]:
    """{file name: text, LF} of DEVLOG/ in the working tree (a CRLF checkout reads the same as the committed LF)."""
    d = repo / DIR
    if not d.is_dir():
        return {}
    return {p.name: p.read_bytes().decode("utf-8").replace("\r\n", "\n") for p in d.iterdir() if p.is_file()}


def listing(repo: pathlib.Path, rev: str) -> list[str] | None:
    """The paths of DEVLOG/ and the monolithic DEVLOG.md in commit `rev`'s tree, from ONE `git ls-tree` of
    `<rev>^{commit}` (a name that is no commit fails it, so no separate `rev-parse --verify`: kb/Work PB3582); None
    when `rev` names no commit."""
    listed = _git(repo, "ls-tree", "--name-only", f"{rev}^{{commit}}", f"{DIR}/", MONOLITH)
    return listed.stdout.splitlines() if listed.returncode == 0 else None


def commit_files(repo: pathlib.Path, rev: str) -> tuple[dict[str, str], bool]:
    """({file name: text} of DEVLOG/ at commit `rev`, whether that tree has the monolithic DEVLOG.md); every blob is
    read through ONE `git cat-file --batch` (two thousand `git show` processes would take a minute)."""
    paths = listing(repo, rev)
    if paths is None:
        raise RangeError(f"{rev} is not a commit")
    names = [n for n in paths if n.startswith(f"{DIR}/")]
    monolith = MONOLITH in paths
    if not names:
        return {}, monolith
    out = subprocess.run(["git", "-C", str(repo), "cat-file", "--batch"], capture_output=True, check=True,
                         input="".join(f"{rev}:{n}\n" for n in names).encode("utf-8")).stdout
    files, at = {}, 0
    for n in names:
        nl = out.index(b"\n", at)
        size = int(out[at:nl].split()[2])
        files[n[len(DIR) + 1:]] = out[nl + 1:nl + 1 + size].decode("utf-8").replace("\r\n", "\n")
        at = nl + 1 + size + 1
    return files, monolith


def monolith_headers(repo: pathlib.Path, rev: str, has_monolith: bool = True) -> list[str]:
    """The `## Entry` header lines of the monolithic DEVLOG.md at `rev`, in file order with every repeat kept (empty
    when that tree has none). The base of the one landing that migrated the log carries its history there; no later
    base does. `has_monolith` False (the caller's listing holds no DEVLOG.md) answers empty with no `git show`."""
    if not has_monolith:
        return []
    r = _git(repo, "show", f"{rev}:{MONOLITH}")
    return [ln.rstrip("\r") for ln in r.stdout.split("\n") if HEADER.match(ln)] if r.returncode == 0 else []


def entries(files: dict[str, str]) -> list[tuple[tuple[int, str], str]]:
    """[(key, name)] of the entry files, newest first: by number, then by letter (1191d above 1191a above 1191)."""
    return sorted(((k, n) for n in files if (k := key_of(n))), reverse=True)


def highest(names) -> int:
    """The highest entry number among file names (a {name: text} dict iterates its names)."""
    return max((k[0] for n in names if (k := key_of(n))), default=0)


def names_in(repo: pathlib.Path) -> list[str]:
    """The file names of DEVLOG/ in the working tree, without reading any file."""
    d = repo / DIR
    return [p.name for p in d.iterdir() if p.is_file()] if d.is_dir() else []


def newest(repo: pathlib.Path, count: int) -> list[str]:
    """The texts of the `count` newest entries of the working tree, newest first, reading only those files."""
    names = sorted(((k, n) for n in names_in(repo) if (k := key_of(n))), reverse=True)
    return [(repo / DIR / n).read_bytes().decode("utf-8").replace("\r\n", "\n") for _, n in names[:count]]


# ── render ───────────────────────────────────────────────────────────────────────────────────────────────────────
def render(files: dict[str, str], since: int | None = None) -> str:
    """The README, then every entry newest first, each file's text exactly as written (a file that does not end in a
    newline gets one). With `since`, only the entries numbered `since` and above, without the README."""
    parts = [] if since is not None or README not in files else [files[README]]
    for (num, _), name in entries(files):
        if since is None or num >= since:
            parts.append(files[name])
    return "".join(p if p.endswith("\n") else p + "\n" for p in parts)


# ── check ────────────────────────────────────────────────────────────────────────────────────────────────────────
def lint(files: dict[str, str], monolith: bool) -> list[str]:
    """What is wrong with a DEVLOG/ tree as a whole, whatever the landing: the monolith back, a stray file, an entry
    whose header disagrees with its name, two files holding one number. A pending entry is legal here (a branch
    carries one until its landing stamps it); `check` refuses it at the landing. The self-test runs this on the
    repository's own tree, so every gate holds the whole log to it."""
    reasons = []
    if monolith:
        reasons.append(f"the tree still has {MONOLITH}: the log is {DIR}/, one file per entry (kb/Work PB3025)")
    strays = sorted(n for n in files if n != README and not key_of(n) and not PENDING_NAME.match(n))
    reasons += [f"{DIR}/{n} is neither {README} nor an entry (`NNNN-<slug>.md`)" for n in strays]
    seen: dict[tuple[int, str], str] = {}
    for k, n in entries(files):
        reasons += entry_problems(n, files[n])
        if k in seen:
            reasons.append(f"{DIR}/{n} and {DIR}/{seen[k]} hold the same entry number {k[0]}{k[1]}")
        seen[k] = n
    return reasons


def new_entries(base: dict[str, str], base_extra_headers: list[str], rev: dict[str, str]) -> list[str]:
    """THE NEW ENTRIES OF A LANDING, the ONE definition (kb/Work PB3023 review R1-3): `check` refuses on it,
    `range_check` reads each one's stamp floor by it (`stamp_floor`, kb/Work PB3123) and `enqueue.py` titles the pull
    request from it (`landing_entries`). The entry files of `rev`, newest first, whose
    header line the base's history does not carry: its entry files and, on the landing that migrates the log, the
    header lines of its monolithic DEVLOG.md (`base_extra_headers`). Never a file-name test: the migrating landing adds
    every entry of the history as a new FILE."""
    history = {first_line(base[n]) for _, n in entries(base)} | set(base_extra_headers)
    return [n for _, n in entries(rev) if first_line(rev[n]) not in history]


def floor_of(base: dict[str, str], base_extra_headers: list[str]) -> int:
    """The base's newest entry number, its monolith's headers included: a new entry is numbered above it."""
    return max([highest(base)] + [int(m.group("num")) for h in base_extra_headers if (m := HEADER.match(h))])


def landing_entries(repo: pathlib.Path, base: str, rev: str) -> tuple[dict[str, str], list[str]]:
    """(the DEVLOG/ files at commit `rev`, the landing's new entries by `new_entries`) for the committed range
    base..rev. Raises RangeError when either end cannot be read."""
    base_files, base_monolith = commit_files(repo, base)
    rev_files, _ = commit_files(repo, rev)
    return rev_files, new_entries(base_files, monolith_headers(repo, base, base_monolith), rev_files)


def check(base: dict[str, str], base_extra_headers: list[str], rev: dict[str, str], rev_monolith: bool,
          stamped_on: dict[str, int] | None = None) -> tuple[list[str], list[str]]:
    """(refusal reasons, the new entries' names) for a landing from the tree `base` to the tree `rev`.
    `base_extra_headers` is the header lines of the base's monolithic DEVLOG.md (`monolith_headers`), repeats kept.
    `stamped_on` maps a new entry to the newest entry number of the tree it was STAMPED on (`stamp_floor`); a new
    entry it does not name is held to the base's newest instead, the floor of a landing built straight on <base>."""
    reasons = lint(rev, rev_monolith)
    pending = sorted(n for n in rev if PENDING_NAME.match(n))
    for n in pending:
        reasons.append(f"{DIR}/{n} is an unstamped entry: run `python scripts/devlog.py stamp <branch>` before landing")
    new = new_entries(base, base_extra_headers, rev)
    base_floor = floor_of(base, base_extra_headers)
    if not new and not pending:
        reasons.append("the landing carries no new DEVLOG entry. Every landing on main carries one (owner 2026-10-08, "
                       f"kb/Work PB2605): write {DIR}/pending-<slug>.md (first line `## <Title>`, then the body) and "
                       "stamp it with `python scripts/devlog.py stamp <branch>`")
    if len(new) > 1:
        reasons.append(f"the landing adds {len(new)} new entries ({', '.join(new)}); a landing carries exactly one")
    for n in new:
        head = first_line(rev[n])
        if not STAMPED.match(head):
            reasons.append(f"{DIR}/{n}: the new header `{head[:90]}` is not `{FORMAT}`")
        k = key_of(n)
        if k[1]:
            reasons.append(f"{DIR}/{n}: a new entry carries a plain number, never a letter")
        # NUMBER ORDER IS STAMP ORDER, NOT MERGE ORDER (kb/Work PB3123). The number is allocated when the branch is
        # enqueued, and GitHub orders the merge queue later, so a merge group may be built on a group that carries a
        # HIGHER number than its own. The rule is therefore the allocator's own guarantee: above every entry of the
        # tree the entry was stamped on, and unique on the landing's tree (`lint`: two files never hold one number).
        floor = (stamped_on or {}).get(n, base_floor)
        if k[0] <= floor:
            reasons.append(f"{DIR}/{n}: number {k[0]} is not above the newest entry {floor} of the tree it was stamped "
                           f"on (only `python scripts/devlog.py stamp <branch>` numbers an entry): make it "
                           f"{DIR}/pending-<slug>.md again, first line `## <Title>`, and let `enqueue.py land` stamp it")
    gone = sorted(n for _, n in entries(base) if n not in rev)
    reasons += [f"{DIR}/{n} is on the base and the landing deletes it: the log is the only history" for n in gone]
    # The base's monolith is history too (kb/Work PB3025, review R1-1): every header it holds, as many times as it holds
    # it, must be the first line of an entry of rev, or the migrating landing has dropped an entry main had.
    dropped = collections.Counter(base_extra_headers) - collections.Counter(first_line(rev[n]) for _, n in entries(rev))
    reasons += [f"the base's {MONOLITH} holds `{h[:90]}`{f' ({c} times)' if c > 1 else ''} and the landing has no "
                f"{DIR}/ entry with that header: the log is the only history (re-split main's {MONOLITH} "
                f"with the migrating branch's own scripts/devlog_migrate.py, which that landing deletes)"
                for h, c in sorted(dropped.items())]
    return reasons, new


def range_check(repo: pathlib.Path, base: str, rev: str) -> tuple[list[str], list[str], dict[str, str]]:
    """`check` on the committed range base..rev: (refusal reasons, the new entries' names, the DEVLOG/ files at rev).
    Raises RangeError when either end cannot be read."""
    base_files, base_monolith = commit_files(repo, base)
    rev_files, rev_monolith = commit_files(repo, rev)
    extra = monolith_headers(repo, base, base_monolith)
    stamped_on = {n: f for n in new_entries(base_files, extra, rev_files)
                  if (f := stamp_floor(repo, base, rev, n)) is not None}
    reasons, new = check(base_files, extra, rev_files, rev_monolith, stamped_on)
    return reasons, new, rev_files


def stamp_floor(repo: pathlib.Path, base: str, rev: str, name: str) -> int | None:
    """The newest entry number of the tree `DEVLOG/<name>` was STAMPED on: the first parent of the OLDEST commit in
    base..rev that added a file of its number (`devlog.py stamp`'s commit; a later retitle that renamed the file adds
    the new name on a tree that may already hold higher numbers), read with renames off, because the stamp renames
    `pending-<slug>.md`. None when no such commit is in the range (the file arrived through a merge's resolution)."""
    m = ENTRY_NAME.match(name)
    if not m:
        return None
    added = _git(repo, "log", "--no-renames", "--diff-filter=A", "--format=%H", f"{base}..{rev}", "--",
                 f":(glob){DIR}/{m.group('num')}{m.group('sfx')}-*.md").stdout.split()
    if not added:
        return None
    parent = f"{added[-1]}^1"
    listed = listing(repo, parent)
    if listed is None:
        return 0                                     # a root commit: there was no tree before it
    return max([highest(n[len(DIR) + 1:] for n in listed if n.startswith(f"{DIR}/"))]
               + [int(m.group("num")) for h in monolith_headers(repo, parent, MONOLITH in listed)
                  if (m := HEADER.match(h))])


def run_check(repo: pathlib.Path, base: str, rev: str) -> int:
    try:
        reasons, new, rev_files = range_check(repo, base, rev)
    except RangeError as e:
        print(f"⛔ devlog check: {e}")
        print("=== DEVLOG CHECK: NOT RUN (the range could not be read) ===")
        return 2
    for r in reasons:
        print(f"⛔ devlog check: {r}")
    if reasons:
        print(f"=== DEVLOG CHECK: REFUSED ({len(reasons)} reason(s)) ===")
        return 1
    print(f"=== DEVLOG CHECK: PASS (1 new entry: {first_line(rev_files[new[0]])[:80]}) ===")
    return 0


# ── stamp ────────────────────────────────────────────────────────────────────────────────────────────────────────
def zone_abbrev(now: dt.datetime) -> str:
    """The zone as `date +%Z` prints it (PDT); Windows' tzname is spelled out ('Pacific Daylight Time')."""
    name = now.tzname() or "UTC"
    return "".join(w[0] for w in name.split()) if " " in name else name


def worktree_of(repo: pathlib.Path, branch: str) -> pathlib.Path:
    """The worktree that has `branch` checked out."""
    path = None
    for line in _git(repo, "worktree", "list", "--porcelain", check=True).stdout.splitlines():
        if line.startswith("worktree "):
            path = pathlib.Path(line[len("worktree "):])
        elif line == f"branch refs/heads/{branch}" and path is not None:
            return path
    raise SystemExit(f"⛔ devlog stamp: no worktree has branch {branch} checked out")


def added_order(wt: pathlib.Path, name: str) -> tuple[int, str]:
    """When `DEVLOG/<name>` first entered the branch's history (its oldest adding commit's time; an uncommitted file
    sorts last), then the name: the order a stamp folds pending entries in, the predecessor's first."""
    times = _git(wt, "log", "--diff-filter=A", "--format=%ct", "--", f"{DIR}/{name}").stdout.split()
    return (int(times[-1]) if times else sys.maxsize, name)


def pending_titles(wt: pathlib.Path) -> dict[str, str]:
    """{pending file name: its title} of the working tree (a file whose first line is no `## <Title>` is left out)."""
    out = {}
    for n, text in worktree_files(wt).items():
        if PENDING_NAME.match(n) and (m := PENDING_HEADER.match(first_line(text))):
            out[n] = m.group("title").strip()
    return out


def half_stamp(wt: pathlib.Path) -> tuple[list[str], list[str]]:
    """What an INTERRUPTED stamp leaves (kb/Work PB3367): ([numbered entry files the working tree has and HEAD's tree
    lacks], [pending files HEAD's tree has and the working tree lacks]). Both lists non-empty is the signature of a
    stamp that wrote the entry and removed the pending file but never committed; `run_stamp` completes it."""
    listed = listing(wt, "HEAD") or []
    in_head = {n[len(DIR) + 1:] for n in listed if n.startswith(f"{DIR}/")}
    on_disk = set(names_in(wt))
    return (sorted(n for n in on_disk - in_head if key_of(n)),
            sorted(n for n in in_head - on_disk if PENDING_NAME.match(n)))


def half_stamp_paths(wt: pathlib.Path) -> set[str]:
    """The repo-relative paths (`DEVLOG/<name>`) an interrupted stamp left uncommitted: the entry it wrote and the
    pending files it removed; empty when the tree holds no such stamp. A landing's dirty check sets them aside."""
    strays, gone = half_stamp(wt)
    return {f"{DIR}/{n}" for n in (*strays, *gone)} if strays and gone else set()


def stamp_due(wt: pathlib.Path) -> bool:
    """Whether `devlog.py stamp` has work in this worktree: a pending entry, or a half-done stamp to complete."""
    if any(PENDING_NAME.match(n) for n in names_in(wt)):
        return True
    strays, gone = half_stamp(wt)
    return bool(strays and gone)


def stamp(wt: pathlib.Path, number: int, now: dt.datetime) -> tuple[str, str]:
    """Number the worktree's pending entry, complete its header and COMMIT it; returns (name, header). The caller
    allocates `number`.

    A branch normally carries ONE pending entry. A branch that merged an unlanded predecessor (a same-file successor,
    a review finisher: MANDATORY-PRACTICES O2, I10) carries the predecessor's too, and lands it: the predecessor lands
    THROUGH it (prune_worktrees.contained), so its landing is one landing on main and gets ONE entry (kb/Work PB2605).
    The entries are folded into that one, oldest first: the titles joined by `; `, each body under its own title.
    A pending entry whose title already has a stamped entry in the tree is the predecessor's that LANDED on its own and
    main has since come in: it is dropped, never folded a second time (kb/Work PB3693; an entry that would leave the
    branch nothing to stamp is kept).

    ALL OR NOTHING (kb/Work PB3367). Every git write retries on `index.lock`; the pending files are removed from disk
    only after the entry and the removals are staged, and any failure puts the tree back exactly as it was (the pending
    bytes restored, the index reset, the numbered file deleted) before it raises GitError with git's stderr, so a
    rerun stamps again instead of finding "no pending entry"."""
    raw = {n: (wt / DIR / n).read_bytes() for n in names_in(wt) if PENDING_NAME.match(n)}
    files = worktree_files(wt)
    pending = sorted(raw, key=lambda n: added_order(wt, n))
    if not pending:
        raise SystemExit(f"⛔ devlog stamp: {wt / DIR} holds no pending entry; a branch carries one, "
                         "`pending-<slug>.md` whose first line is `## <Title>`")
    parts = []
    for n in pending:
        m = PENDING_HEADER.match(first_line(files[n]))
        if not m:
            raise SystemExit(f"⛔ devlog stamp: {DIR}/{n} must start with `## <Title>` (no number, no stamp)")
        parts.append((n, m.group("title").strip(), (files[n].split("\n", 1)[1] if "\n" in files[n] else "").strip("\n")))
    stamped = {m.group("slug") for n in files if (m := ENTRY_NAME.match(n))}
    fresh = [p for p in parts if slugify(p[1]) not in stamped]
    if fresh and len(fresh) < len(parts):
        for n, title, _ in (p for p in parts if p not in fresh):
            print(f"devlog stamp: {DIR}/{n} ({title!r}) is already a stamped entry of this tree (its branch landed on "
                  "its own): dropped, not folded into this branch's entry")
        parts = fresh
    title = "; ".join(t for _, t, _ in parts)
    body = parts[0][2] if len(parts) == 1 else "\n\n".join(f"### {t}\n\n{b}" for _, t, b in parts)
    header = f"## Entry {number} {DASH} {now.strftime('%Y-%m-%d %H:%M')} {zone_abbrev(now)} {DASH} {title}"
    name = entry_name(number, "", title)
    entry, removed = f"{DIR}/{name}", [f"{DIR}/{n}" for n in pending]
    (wt / DIR / name).write_bytes((header + "\n\n" + body + "\n\n").encode("utf-8"))
    try:
        # exactly these paths: the entry, and each pending file's removal (tracked or not); nothing else in DEVLOG/
        _git_write(wt, "add", "--", entry)
        _git_write(wt, "rm", "-q", "--cached", "--ignore-unmatch", "--", *removed)
        for n in pending:
            (wt / DIR / n).unlink()
        _git_write(wt, "commit", "-q", "-m", f"DEVLOG {number}: stamp the entry ({entry})")
    except (GitError, OSError) as exc:
        stuck = ""
        try:
            _git_write(wt, "reset", "-q", "HEAD", "--", entry, *removed)
        except GitError as reset_exc:
            stuck = f" (the index could not be reset either: {reset_exc}; run `git reset -q HEAD -- {entry} {' '.join(removed)}`)"
        for n, data in raw.items():
            (wt / DIR / n).write_bytes(data)
        (wt / DIR / name).unlink(missing_ok=True)
        raise GitError(f"the stamp failed and was rolled back, the pending entry is as it was: {exc}{stuck}") from exc
    return name, header


def finish_half_stamp(wt: pathlib.Path, strays: list[str], gone: list[str]) -> str:
    """Complete a stamp that was interrupted after it wrote the entry and removed the pending file (`half_stamp`): stage
    the entry and the removals and commit, under the number the entry already holds. Returns the entry's file name."""
    if len(strays) != 1:
        raise SystemExit(f"⛔ devlog stamp: {len(strays)} numbered entry files ({', '.join(strays)}) are not in HEAD and "
                         "the pending entry is gone: an interrupted stamp leaves one; keep the right one and re-run")
    name = strays[0]
    head = first_line(worktree_files(wt)[name])
    if not STAMPED.match(head):
        raise SystemExit(f"⛔ devlog stamp: {DIR}/{name} is not a stamped entry (`{head[:60]}`): not an interrupted "
                         "stamp's; remove it or finish it by hand")
    _git_write(wt, "add", "--", f"{DIR}/{name}")
    _git_write(wt, "rm", "-q", "--cached", "--ignore-unmatch", "--", *(f"{DIR}/{n}" for n in gone))
    _git_write(wt, "commit", "-q", "-m", f"DEVLOG {key_of(name)[0]}: stamp the entry ({DIR}/{name}), completed after an "
                                         "interrupted stamp")
    return name


#: `devlog.py stamp`'s exit when a git call failed and the stamp was rolled back (or could not be completed): transient
#: by nature, so `enqueue.py land` ends the landing exit 2, which the landing lane retries once (kb/Work PB3367).
EXIT_GIT = 3


def run_stamp(repo: pathlib.Path, branch: str, coord_override: str | None) -> int:
    sys.path.insert(0, str(HERE / "orchestrator"))
    import alloc  # noqa: PLC0415
    import coord  # noqa: PLC0415
    try:
        wt = worktree_of(repo, branch)
        strays, gone = half_stamp(wt)
        if not any(PENDING_NAME.match(n) for n in names_in(wt)):
            if strays and gone:
                name = finish_half_stamp(wt, strays, gone)
                print(f"stamped {DIR}/{name}: completed a stamp that was interrupted before its commit")
                return 0
            raise SystemExit(f"⛔ devlog stamp: {wt / DIR} has no pending entry to stamp (an entry keeps the number it "
                             "was first stamped with: kb/Work PB3123)")
        if strays:
            raise SystemExit(f"⛔ devlog stamp: {', '.join(strays)} is in the working tree and not in HEAD, beside the "
                             "pending entry: an interrupted stamp left it. Delete it (and `git rm --cached` it) and re-run")
        number, _ = alloc.allocate("devlog", 1, wt, coord.coord_dir(coord_override))
        name, header = stamp(wt, number, dt.datetime.now().astimezone())
    except GitError as exc:
        print(f"⛔ devlog stamp: {exc}")
        print("=== DEVLOG STAMP: GIT FAILED (nothing stamped; a re-run stamps again) ===")
        return EXIT_GIT
    print(f"stamped {DIR}/{name}: {header}")
    return 0


# ── self-test: real repositories and real ranges ─────────────────────────────────────────────────────────────────
def self_test() -> int:
    global GIT_LOCK_WAITS, _pause
    from scratch_repo import ScratchRepo, isolate, remove_tree, scratch_dir  # noqa: PLC0415
    # HERMETIC, like landing_check's: drop git's repository-selection variables so nothing reaches the caller's repo.
    isolate()
    root = scratch_dir("devlog-self-test-")
    results: list[tuple[str, bool, str]] = []

    def ok(label: str, cond: bool, got: object = "") -> None:
        results.append((label, bool(cond), str(got)))

    # names, slugs, titles
    ok("slug: words, ASCII, lowercase", slugify("PB40: a CLASS screen — the fix") == "pb40-a-class-screen-the-fix")
    ok("slug: capped at a word boundary", len(slugify("word " * 40)) <= SLUG_MAX and not slugify("word " * 40).endswith("-"))
    ok("title of a stamped header", title_of(f"## Entry 1960 {DASH} 2026-10-11 13:38 PDT {DASH} Train 1061: x") ==
       "Train 1061: x", title_of(f"## Entry 1960 {DASH} 2026-10-11 13:38 PDT {DASH} Train 1061: x"))
    ok("title of an early header (date and colon)", title_of(f"## Entry 185 {DASH} 2026-03-30: Ledger") == "Ledger")
    ok("title of a double-encoded dash header", title_of("## Entry 1228 â\u0080\u0094 2026-08-08 13:24 PDT "
                                                         "â\u0080\u0094 R20: walk") == "R20: walk")
    ok("title of an undated header", title_of(f"## Entry 511 {DASH} COBOL.NET: IS NUMERIC") == "COBOL.NET: IS NUMERIC")

    def e(num: str, title: str = "t", body: str = "body\n\n") -> str:
        return f"## Entry {num} {DASH} 2026-10-11 10:00 PDT {DASH} {title}\n\n{body}"

    # render: README first; number, then letter, descending; a duplicate's file-name letter orders it
    files = {README: "# Log\n\n", "1191-a.md": e("1191"), "1191a-b.md": e("1191a"), "1191d-c.md": e("1191d"),
             "0185b-first.md": e("185", "first"), "0185a-second.md": e("185", "second"), "1192-x.md": e("1192")}
    order = [first_line(p) for p in render(files).split("\n") if p.startswith("## Entry")]
    ok("render: README, then 1192, 1191d, 1191a, 1191, 185 (b), 185 (a)",
       render(files).startswith("# Log\n\n## Entry 1192") and
       [h.split()[2] for h in order] == ["1192", "1191d", "1191a", "1191", "185", "185"] and
       order[4].endswith("first"), order)
    ok("render --since: the entries at or above, no README", render(files, since=1191).startswith("## Entry 1192") and
       "## Entry 185" not in render(files, since=1191))
    ok("render is byte-faithful concatenation", render({README: "a\n", "0001-x.md": "## Entry 001 x\n"}) ==
       "a\n## Entry 001 x\n")

    # check, against real commits
    scratch = ScratchRepo.init(root / "repo")
    repo = scratch.path
    scratch.write({f"{DIR}/{README}": "# Log\n", f"{DIR}/0011-older.md": e("11", "older"),
                   f"{DIR}/0010-oldest.md": e("10", "oldest"), "code.py": "x = 1\n"})
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "base")
    base = scratch.git("rev-parse", "HEAD")

    def landing(label: str, spec: dict[str, str | None], want: int, needle: str = "") -> None:
        scratch.git("checkout", "-q", "-B", "landing", base)
        scratch.write(spec)
        scratch.git("add", "-A")
        scratch.git("commit", "-qm", "WIP checkpoint: x", "--allow-empty")
        b, _ = commit_files(repo, base)
        r, mono = commit_files(repo, "HEAD")
        reasons, _ = check(b, monolith_headers(repo, base), r, mono)
        got = 1 if reasons else 0
        ok(label, got == want and (not needle or any(needle in x for x in reasons)), "; ".join(reasons) or "PASS")

    new12 = {f"{DIR}/0012-the-landing.md": e("12", "the landing")}
    landing("one new stamped entry above the base -> PASS", {**new12, "code.py": "x = 2\n"}, 0)
    landing("no new entry -> REFUSED", {"code.py": "x = 3\n"}, 1, "no new DEVLOG entry")
    landing("two new entries -> REFUSED", {**new12, f"{DIR}/0013-another.md": e("13", "another")}, 1, "exactly one")
    landing("an unstamped header -> REFUSED", {f"{DIR}/0012-x.md": f"## Entry 12 - x\n\nbody\n"}, 1, "is not `")
    landing("a number not above the tree it was stamped on -> REFUSED", {f"{DIR}/0011-again.md": e("11", "again")}, 1,
            "tree it was stamped on")
    landing("a pending entry -> REFUSED", {f"{DIR}/pending-x.md": "## X\n\nbody\n"}, 1, "unstamped")
    landing("a letter on a new entry -> REFUSED", {f"{DIR}/0012a-x.md": e("12a", "x")}, 1, "never a letter")
    landing("a header that disagrees with its name -> REFUSED", {f"{DIR}/0012-x.md": e("13", "x")}, 1, "file name")
    landing("a stray file -> REFUSED", {**new12, f"{DIR}/notes.txt": "x"}, 1, "neither")
    landing("deleting a base entry -> REFUSED", {**new12, f"{DIR}/0010-oldest.md": None}, 1, "deletes")
    landing("the monolith back in the tree -> REFUSED", {**new12, MONOLITH: e("12")}, 1, MONOLITH)
    landing("two files holding one number -> REFUSED", {**new12, f"{DIR}/0012-twin.md": e("12", "twin")}, 1,
            "same entry number")
    landing("editing an old entry's body, with the new entry -> PASS",
            {**new12, f"{DIR}/0010-oldest.md": e("10", "oldest", "body, corrected\n\n")}, 0)
    # the migrating landing: the base has only the monolith; its headers are history, one new entry is new
    scratch.git("checkout", "-q", "--orphan", "mono")
    scratch.git("rm", "-rq", "--cached", ".")
    shutil.rmtree(repo / DIR)
    scratch.write({MONOLITH: "# Log\n" + e("11", "older") + e("10", "oldest"), "code.py": "x = 1\n"})
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "monolith")
    mono_base = scratch.git("rev-parse", "HEAD")
    scratch.git("rm", "-q", MONOLITH)
    scratch.write({f"{DIR}/{README}": "# Log\n", f"{DIR}/0011-older.md": e("11", "older"),
                   f"{DIR}/0010-oldest.md": e("10", "oldest"), **new12})
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "migrate")
    b, _ = commit_files(repo, mono_base)
    r, mono = commit_files(repo, "HEAD")
    reasons, new = check(b, monolith_headers(repo, mono_base), r, mono)
    ok("the migrating landing: the monolith's headers are history, its one new entry passes",
       not reasons and new == ["0012-the-landing.md"], reasons)
    files_at, landed = landing_entries(repo, mono_base, "HEAD")
    ok("landing_entries on the migrating landing: every history file is added, ONE entry is new (the pull request's "
       "title reads it; kb/Work PB3023 review R1-3)",
       landed == ["0012-the-landing.md"] and len([n for n in files_at if key_of(n)]) == 3, landed)
    scratch.git("checkout", "-q", "mono")
    scratch.write({f"{DIR}/0011-older.md": e("11", "older, retitled")})
    scratch.git("commit", "-qam", "retitle")
    reasons, _ = check(b, monolith_headers(repo, mono_base), commit_files(repo, "HEAD")[0], False)
    ok("the migrating landing with a header changed in the split -> REFUSED (two new)", any("exactly one" in x for x in
                                                                                             reasons), reasons)
    # review R1-1: main's monolith gained an entry after the branch split it; a landing that keeps the deletion instead
    # of re-running the migration carries its own new entry above main's newest, and must still be refused.
    scratch.git("checkout", "-q", "-B", "mono-newer", mono_base)
    scratch.write({MONOLITH: "# Log\n" + e("12", "main's newer") + e("11", "older") + e("10", "oldest")})
    scratch.git("commit", "-qam", "main gains an entry")
    newer_base = scratch.git("rev-parse", "HEAD")
    scratch.git("rm", "-q", MONOLITH)
    scratch.write({f"{DIR}/{README}": "# Log\n", f"{DIR}/0011-older.md": e("11", "older"),
                   f"{DIR}/0010-oldest.md": e("10", "oldest"), f"{DIR}/0013-the-landing.md": e("13", "the landing")})
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "a stale split")
    reasons, new = check(commit_files(repo, newer_base)[0], monolith_headers(repo, newer_base),
                         *commit_files(repo, "HEAD"))
    ok("the migrating landing that drops an entry of the base's monolith -> REFUSED (deletes), though its one new "
       "entry is above the base",
       new == ["0013-the-landing.md"] and len(reasons) == 1 and f"no {DIR}/ entry with that header" in reasons[0]
       and "main's newer" in reasons[0], reasons)
    twice = e("9", "twice")
    reasons, _ = check({}, [first_line(twice), first_line(twice)],
                       {README: "# Log\n", "0009a-twice.md": twice, "0013-new.md": e("13", "new")}, False)
    ok("a header the base's monolith holds twice needs two entries -> REFUSED with one",
       len(reasons) == 1 and "twice" in reasons[0] and "times)" not in reasons[0], reasons)
    try:
        commit_files(repo, "no-such-rev")
        ok("an unreadable revision -> NOT RUN (exit 2), never a pass", False, "no RangeError")
    except RangeError:
        ok("an unreadable revision -> NOT RUN (exit 2), never a pass", True)

    # THE MERGE QUEUE STACKS (kb/Work PB3123). GitHub builds each queued pull request's merge group ON TOP of every
    # group ahead of it, in an order it picks when each pull request's checks finish, and CI runs `check` on (the
    # group's base, the group's head). Two branches stamped from one main (12, then 13) must pass in BOTH orders; until
    # PB3123 the order 13-then-12 refused the 12 as "not above the base's newest entry 13" (runs 38355538654,
    # 38360587401). The trees are built as the queue builds them: a merge commit of each branch onto the group ahead.
    scratch.git("checkout", "-q", "main")

    def stamped_branch(name: str, at: str, edits: dict[str, str | None]) -> str:
        scratch.git("checkout", "-q", "-B", name, at)
        scratch.write(edits)
        scratch.git("add", "-A")
        scratch.git("commit", "-qm", f"WIP checkpoint: {name} and its stamped entry")
        return scratch.git("rev-parse", "HEAD")

    def group(name: str, onto: str, branch: str) -> str:
        scratch.git("checkout", "-q", "-B", name, onto)
        scratch.git("merge", "-q", "--no-ff", "-m", f"Merge {branch} (the merge queue's group)", branch)
        return scratch.git("rev-parse", "HEAD")

    def range_verdict(label: str, lo: str, hi: str, want: int, needle: str = "") -> None:
        reasons, _, _ = range_check(repo, lo, hi)
        got = 1 if reasons else 0
        ok(label, got == want and (not needle or any(needle in x for x in reasons)), "; ".join(reasons) or "PASS")

    qa = stamped_branch("qa", base, {f"{DIR}/0012-queued-first.md": e("12", "queued first"), "a.py": "a = 1"})
    qb = stamped_branch("qb", base, {f"{DIR}/0013-queued-second.md": e("13", "queued second"), "b.py": "b = 1"})
    g_a = group("g-a", base, qa)
    range_verdict("stacked, queue order 12 then 13: the first group -> PASS", base, g_a, 0)
    range_verdict("stacked, queue order 12 then 13: the second group, built on the first -> PASS", g_a,
                  group("g-ab", g_a, qb), 0)
    g_b = group("g-b", base, qb)
    range_verdict("stacked, queue order 13 then 12: the first group -> PASS", base, g_b, 0)
    g_ba = group("g-ba", g_b, qa)
    range_verdict("stacked, queue order 13 then 12 (PB3123's bounce): the second group, built on the first -> PASS",
                  g_b, g_ba, 0)
    qc = stamped_branch("qc", base, {f"{DIR}/0012-a-twin.md": e("12", "a twin"), "c.py": "c = 1"})
    range_verdict("stacked: two queued branches holding ONE number (an allocator failure) -> REFUSED", g_a,
                  group("g-ac", g_a, qc), 1, "same entry number")
    retitled = stamped_branch("retitled", qa, {f"{DIR}/0012-queued-first.md": None,
                                               f"{DIR}/0012-queued-first-retitled.md": e("12", "queued first, retitled")})
    range_verdict("a stamped entry retitled (its file renamed) after its stamp keeps the stamp's floor -> PASS", g_b,
                  group("g-br", g_b, retitled), 0)
    hand = stamped_branch("hand", g_ba, {f"{DIR}/0009-hand-numbered.md": e("9", "hand numbered"), "d.py": "d = 1"})
    range_verdict("a number below every entry of the tree it was stamped on (hand-numbered) -> REFUSED", g_ba, hand, 1,
                  "tree it was stamped on")

    # stamp: a branch in a linked worktree; the number comes from the allocator; one commit
    scratch.git("checkout", "-q", "main")
    wt = root / "wt"
    scratch.git("worktree", "add", "-q", "-b", "impl", str(wt), base)
    scratch.write({f"{DIR}/pending-my-fix.md": "## PB9: the fix — and why\n\nThe body.\n"}, cwd=wt)
    scratch.git("add", "-A", cwd=wt)
    scratch.git("commit", "-qm", "WIP checkpoint: the fix and its entry", cwd=wt)
    cdir = root / "coord"
    cdir.mkdir()
    out = subprocess.run([sys.executable, str(pathlib.Path(__file__).resolve()), "stamp", "impl", "--repo", str(repo),
                          "--coord", str(cdir)], capture_output=True, text=True, encoding="utf-8")
    names = sorted(p.name for p in (wt / DIR).iterdir())
    head = first_line((wt / DIR / "0012-pb9-the-fix-and-why.md").read_text(encoding="utf-8")) if \
        (wt / DIR / "0012-pb9-the-fix-and-why.md").exists() else ""
    ok("stamp: the pending entry becomes 0012-<slug>.md with a stamped header, in one commit",
       out.returncode == 0 and "0012-pb9-the-fix-and-why.md" in names and not any(n.startswith("pending-") for n in names)
       and STAMPED.match(head) and head.endswith("PB9: the fix — and why")
       and scratch.git("log", "-1", "--format=%s", cwd=wt).startswith("DEVLOG 12:")
       and not scratch.git("status", "--porcelain", cwd=wt),
       out.stdout + out.stderr + repr(names) + head)
    ok("stamp: the stamped branch passes check", not check(commit_files(repo, base)[0], [],
                                                           *commit_files(repo, "impl"))[0])
    ok("stamp: the allocator recorded the number (the next stamp takes 13)",
       json.loads((cdir / "alloc.json").read_text(encoding="utf-8")).get("devlog") == 12)
    again = subprocess.run([sys.executable, str(pathlib.Path(__file__).resolve()), "stamp", "impl", "--repo", str(repo),
                            "--coord", str(cdir)], capture_output=True, text=True, encoding="utf-8")
    ok("stamp: a branch with no pending entry is refused, nothing allocated", again.returncode != 0 and
       json.loads((cdir / "alloc.json").read_text(encoding="utf-8")).get("devlog") == 12, again.stdout + again.stderr)
    # OVERTAKEN, KEPT (kb/Work PB3123): another landing, stamped 13 after `impl` took 12, reached main first (the queue
    # ordered it ahead, or `impl` bounced); main is merged in and `impl` keeps its 12, which still passes the landing
    # rule: it is above every entry of the tree it was stamped on, and no other file holds it.
    scratch.git("checkout", "-q", "main")
    scratch.write({f"{DIR}/0013-a-later-stamp.md": e("13", "a later stamp")})
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "a later-stamped landing reaches main first")
    scratch.git("merge", "-q", "--no-edit", "main", cwd=wt)
    kept, _, _ = range_check(repo, "main", "impl")
    names = sorted(p.name for p in (wt / DIR).iterdir())
    ok("an entry a later-stamped landing overtook on main keeps its number and passes check",
       not kept and "0012-pb9-the-fix-and-why.md" in names, "; ".join(kept) + repr(names))
    ok("next: the highest number in the tree", highest(names_in(wt)) == 13)
    # FOLD (kb/Work PB3023 review R1-3): a successor that merged an unlanded predecessor carries both pending entries
    # and lands both (the predecessor lands THROUGH it): ONE entry, the predecessor's first, each body kept
    scratch.git("checkout", "-q", "main")
    pred = root / "pred"
    scratch.git("worktree", "add", "-q", "-b", "pred", str(pred), "main")
    scratch.write({f"{DIR}/pending-pb1-pred.md": "## PB1: the predecessor\n\nThe predecessor's body.\n"}, cwd=pred)
    scratch.git("add", "-A", cwd=pred)
    scratch.git("commit", "-qm", "WIP checkpoint: the predecessor", cwd=pred,   # the predecessor's entry is the older one
                env={"GIT_COMMITTER_DATE": "2026-10-11T10:00:00"})
    succ = root / "succ"
    scratch.git("worktree", "add", "-q", "-b", "succ", str(succ), "main")
    scratch.write({f"{DIR}/pending-a-succ.md": "## PB2: the successor\n\nThe successor's body.\n"}, cwd=succ)
    scratch.git("add", "-A", cwd=succ)
    scratch.git("commit", "-qm", "WIP checkpoint: the successor", cwd=succ,
                env={"GIT_COMMITTER_DATE": "2026-10-11T11:00:00"})
    scratch.git("merge", "-q", "--no-edit", "pred", cwd=succ)
    fold = subprocess.run([sys.executable, str(pathlib.Path(__file__).resolve()), "stamp", "succ", "--repo", str(repo),
                           "--coord", str(cdir)], capture_output=True, text=True, encoding="utf-8")
    names = sorted(p.name for p in (succ / DIR).iterdir())
    folded = [n for n in names if n.startswith("0014-")]
    text = (succ / DIR / folded[0]).read_text(encoding="utf-8") if folded else ""
    ok("stamp: two pending entries (a merged predecessor's and the branch's) fold into ONE entry, the predecessor's "
       "first, both bodies kept, and the branch passes check",
       fold.returncode == 0 and folded == ["0014-pb1-the-predecessor-pb2-the-successor.md"]
       and first_line(text).endswith("PB1: the predecessor; PB2: the successor")
       and text.index("### PB1: the predecessor") < text.index("### PB2: the successor")
       and "The predecessor's body." in text and "The successor's body." in text
       and not any(n.startswith("pending-") for n in names) and not scratch.git("status", "--porcelain", cwd=succ)
       and not check(commit_files(repo, "main")[0], set(), *commit_files(repo, "succ"))[0],
       fold.stdout + fold.stderr + repr(names))
    # ATOMIC STAMP (kb/Work PB3367): a git write that fails leaves the tree as it was, says why, and a rerun stamps.
    def branch_with(name: str, entries: dict[str, str]) -> pathlib.Path:
        w = root / name
        scratch.git("worktree", "add", "-q", "-b", name, str(w), "main")
        scratch.write({f"{DIR}/{n}": t for n, t in entries.items()}, cwd=w)
        scratch.git("add", "-A", cwd=w)
        scratch.git("commit", "-qm", f"WIP checkpoint: {name}", cwd=w)
        return w

    def stamp_run(branch: str) -> tuple[int, str]:
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            rc = run_stamp(repo, branch, str(cdir))
        return rc, out.getvalue()

    saved_waits, saved_pause = GIT_LOCK_WAITS, _pause
    lock_wt = branch_with("locked", {"pending-lock.md": "## PB7: lock\n\nLock body.\n"})
    lock_file = pathlib.Path(scratch.git("rev-parse", "--absolute-git-dir", cwd=lock_wt)) / "index.lock"
    before_bytes = (lock_wt / DIR / "pending-lock.md").read_bytes()
    before_head = scratch.git("rev-parse", "HEAD", cwd=lock_wt)
    names_before = sorted(names_in(lock_wt))
    lock_file.write_text("held by another git process", encoding="utf-8")
    GIT_LOCK_WAITS = ()
    refused_rc, refused_out = stamp_run("locked")
    GIT_LOCK_WAITS = saved_waits
    after = sorted(p.name for p in (lock_wt / DIR).iterdir())
    lock_file.unlink()
    ok("stamp: a git write that fails (index.lock held) rolls the stamp back: exit 3, git's own stderr printed, the "
       "pending entry byte for byte, no numbered file, HEAD and the index unchanged",
       refused_rc == EXIT_GIT and "index.lock" in refused_out and "rolled back" in refused_out
       and after == names_before
       and (lock_wt / DIR / "pending-lock.md").read_bytes() == before_bytes
       and scratch.git("rev-parse", "HEAD", cwd=lock_wt) == before_head
       and not scratch.git("status", "--porcelain", cwd=lock_wt), refused_out + repr(after))
    again_rc, again_out = stamp_run("locked")
    locked_entries = [n for n in names_in(lock_wt) if n.endswith("-pb7-lock.md")]
    ok("stamp: the rerun after that failure stamps (the pre-fix tree said 'no pending entry' forever)",
       again_rc == 0 and len(locked_entries) == 1 and not any(PENDING_NAME.match(n) for n in names_in(lock_wt))
       and not scratch.git("status", "--porcelain", cwd=lock_wt), again_out)
    transient = branch_with("transient", {"pending-tr.md": "## PB8: transient\n\nBody.\n"})
    transient_lock = pathlib.Path(scratch.git("rev-parse", "--absolute-git-dir", cwd=transient)) / "index.lock"
    transient_lock.write_text("held briefly", encoding="utf-8")
    paused: list[float] = []

    def release(wait: float) -> None:
        paused.append(wait)
        transient_lock.unlink(missing_ok=True)
    GIT_LOCK_WAITS, _pause = (0.01, 0.02), release
    transient_rc, transient_out = stamp_run("transient")
    GIT_LOCK_WAITS, _pause = saved_waits, saved_pause
    ok("stamp: a lock another git process holds briefly is waited out: one retry, then the stamp commits",
       transient_rc == 0 and paused == [0.01] and any(n.endswith("-pb8-transient.md") for n in names_in(transient))
       and not scratch.git("status", "--porcelain", cwd=transient), f"{transient_rc} {paused} {transient_out}")
    # an INTERRUPTED stamp (the process died after it removed the pending file): a rerun completes it, keeping its number
    cut = branch_with("cut", {"pending-cut.md": "## PB6: cut\n\nBody.\n"})
    cut_number = highest(names_in(cut)) + 40
    cut_name = entry_name(cut_number, "", "PB6: cut")
    (cut / DIR / cut_name).write_bytes(f"## Entry {cut_number} {DASH} 2026-10-11 10:00 PDT {DASH} PB6: cut\n\nBody.\n\n".encode())
    (cut / DIR / "pending-cut.md").unlink()
    due = stamp_due(cut) and half_stamp_paths(cut) == {f"{DIR}/{cut_name}", f"{DIR}/pending-cut.md"}
    cut_rc, cut_out = stamp_run("cut")
    ok("stamp: a stamp interrupted before its commit (entry written, pending file gone) is completed by the rerun under "
       "the number it holds, in one commit, with a clean tree",
       due and cut_rc == 0 and scratch.git("log", "-1", "--format=%s", cwd=cut).startswith(f"DEVLOG {cut_number}:")
       and not scratch.git("status", "--porcelain", cwd=cut) and not stamp_due(cut)
       and json.loads((cdir / "alloc.json").read_text(encoding="utf-8")).get("devlog", 0) < cut_number, cut_out)
    # a predecessor that LANDED on its own (its stamped entry is in the tree) is not folded again (kb/Work PB3693)
    scratch.git("checkout", "-q", "main")
    scratch.write({f"{DIR}/0090-pb4-the-landed-predecessor.md": e("90", "PB4: the landed predecessor")})
    scratch.git("add", "-A")
    scratch.git("commit", "-qm", "the predecessor landed alone")
    carrier = branch_with("carrier", {"pending-pb4-landed.md": "## PB4: the landed predecessor\n\nIts body.\n",
                                      "pending-pb5-own.md": "## PB5: the carrier's own\n\nOwn body.\n"})
    carrier_rc, carrier_out = stamp_run("carrier")
    carrier_entries = [n for n in names_in(carrier) if n.endswith("-pb5-the-carrier-s-own.md")]
    carrier_text = (carrier / DIR / carrier_entries[0]).read_text(encoding="utf-8") if carrier_entries else ""
    ok("stamp: a pending entry whose title is already a stamped entry of the tree (its branch landed alone) is dropped, "
       "not folded: the carrier's entry holds only its own title and body",
       carrier_rc == 0 and len(carrier_entries) == 1 and "PB4" not in first_line(carrier_text) and "Its body" not in carrier_text
       and "Own body." in carrier_text and not any(PENDING_NAME.match(n) for n in names_in(carrier))
       and not scratch.git("status", "--porcelain", cwd=carrier), carrier_out)
    lone = branch_with("lone", {"pending-pb4-twice.md": "## PB4: the landed predecessor\n\nSecond body.\n"})
    lone_rc, lone_out = stamp_run("lone")
    ok("stamp: a branch whose ONLY pending entry repeats a stamped title is still stamped (nothing is dropped to nothing)",
       lone_rc == 0 and any(n.endswith("-pb4-the-landed-predecessor.md") and n.startswith("0") and n != "0090-pb4-the-landed-predecessor.md"
                            for n in names_in(lone)), lone_out)
    ok("zone: a spelled-out zone name is abbreviated as date +%Z prints it",
       zone_abbrev(dt.datetime(2026, 10, 11, tzinfo=dt.timezone(dt.timedelta(hours=-7), "Pacific Daylight Time")))
       == "PDT")

    # The PB2605 repro, kept from landing_devlog.py: the rule is the LANDING's, so a commit in a WORKTREE with nothing
    # but code staged (an implementer's WIP checkpoint) draws no DEVLOG ask or deny from ANY PreToolUse hook this
    # repository registers, in either shell and either shape (`cd <wt> && git commit`, `git -C <wt> commit`).
    scratch.write({"code.py": "x = 9\n"}, cwd=wt)
    scratch.git("add", "code.py", cwd=wt)
    settings = json.loads((REPO / ".claude" / "settings.json").read_text(encoding="utf-8"))
    hooks = [(m.get("matcher", ""), h["command"]) for m in settings["hooks"].get("PreToolUse", []) for h in m["hooks"]]
    script = re.compile(r'"\$CLAUDE_PROJECT_DIR/([^"]+)"')
    asked, ran, missing = [], 0, set()
    for tool, command in (("Bash", f"cd {wt.as_posix()} && git commit -m 'WIP checkpoint: x'"),
                          ("Bash", f"git -C {wt.as_posix()} commit -m 'WIP checkpoint: x'"),
                          ("PowerShell", f"Set-Location {wt}; git commit -m 'WIP checkpoint: x'")):
        payload = json.dumps({"hook_event_name": "PreToolUse", "tool_name": tool, "cwd": str(REPO),
                              "tool_input": {"command": command}})
        for matcher, cmd in hooks:
            if not re.fullmatch(matcher, tool) or not (m := script.search(cmd)):
                continue
            if not (REPO / m.group(1)).is_file():
                missing.add(m.group(1))
                continue
            ran += 1
            hook_out = subprocess.run([sys.executable, str(REPO / m.group(1))], input=payload, capture_output=True,
                                      text=True, encoding="utf-8", errors="replace",
                                      env=dict(os.environ, CLAUDE_PROJECT_DIR=str(REPO))).stdout
            if "DEVLOG" in hook_out:
                asked.append(f"{m.group(1)} on `{command}`")
    staged = scratch.git("diff", "--cached", "--name-only", cwd=wt).split()
    ok("the PB2605 repro: a worktree WIP commit without a DEVLOG entry draws no DEVLOG ask from any hook",
       staged == ["code.py"] and not asked and not missing and ran >= 3,
       f"staged={staged}; asked by: {asked}; hooks run: {ran}; registered but missing: {sorted(missing)}")

    # THE REPOSITORY'S OWN LOG: every gate and CI run this self-test, so the whole of DEVLOG/ is held to `lint`.
    own_files = worktree_files(REPO)
    own = lint(own_files, (REPO / MONOLITH).exists())
    ok(f"this repository's {DIR}/ passes lint ({len(entries(own_files))} entries)", not own, "; ".join(own[:5]))

    scratch.git("worktree", "remove", "--force", str(wt))
    remove_tree(root)
    bad = [r for r in results if not r[1]]
    for label, good, got in results:
        print(f"  {'ok ' if good else 'BAD'} {label}" + ("" if good else f": {got}"))
    print(f"=== DEVLOG SELF-TEST: {'PASS' if not bad else 'FAIL'} ({len(results) - len(bad)}/{len(results)}) ===")
    return 1 if bad else 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split(chr(10))[0])
    ap.add_argument("--self-test", action="store_true")
    sub = ap.add_subparsers(dest="cmd")
    r = sub.add_parser("render", help="print the descending narrative")
    r.add_argument("--since", type=int, help="only the entries numbered this and above, without the README")
    r.add_argument("--out", help="write it here (DEVLOG.rendered.md is gitignored) instead of stdout")
    s = sub.add_parser("stamp", help="number and stamp a branch's pending entry, in one commit in its worktree")
    s.add_argument("branch")
    s.add_argument("--coord", help="the coordination directory alloc.py reserves the number in")
    c = sub.add_parser("check", help="the landing rule on the committed range base..rev")
    c.add_argument("--base", default="origin/main")
    c.add_argument("--rev", default="HEAD")
    sub.add_parser("next", help="the highest entry number in DEVLOG/")
    for p in (r, s, c, sub.choices["next"]):
        p.add_argument("--repo", default=None, help="a checkout of the repository (default: the current one)")
    a = ap.parse_args(argv)
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except AttributeError:
        pass
    if a.self_test:
        return self_test()
    if not a.cmd:
        ap.error("a command is required")
    if a.repo:
        repo = pathlib.Path(a.repo)
    else:
        top = subprocess.run(["git", "rev-parse", "--show-toplevel"], capture_output=True, text=True)
        if top.returncode != 0:
            print("=== DEVLOG: NOT RUN (not inside a git work tree) ===")
            return 2
        repo = pathlib.Path(top.stdout.strip())
    if a.cmd == "check":
        return run_check(repo, a.base, a.rev)
    if a.cmd == "stamp":
        return run_stamp(repo, a.branch, a.coord)
    if a.cmd == "next":
        print(highest(names_in(repo)))
        return 0
    files = worktree_files(repo)
    text = render(files, a.since)
    if a.out:
        pathlib.Path(a.out).write_bytes(text.encode("utf-8"))
        print(f"wrote {a.out} ({text.count(chr(10)):,} lines, {len(entries(files)):,} entries)")
    else:
        sys.stdout.write(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
