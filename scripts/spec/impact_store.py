#!/usr/bin/env python3
"""impact_store.py — THE ONE HOME OF THE IMPACT-MAP STORE (kb/Work PB1683, PB3663; DESIGN-test-build-ci.md §3.13).

    python scripts/spec/impact_store.py --self-test

Three programs touch the store `<git common dir>/cobol-impact/` that every worktree of the repository shares, and each
used to carry its own copy of the rules: the recorder (`record_impact_map.py`) writes `<sha>.json.gz`, the lookup
(`impacted_tests.py`) reads the one that describes a change's base, and the loop's recorder lane
(`scripts/orchestrator/impact_lane.py`, kb/Work PB3663) decides what to record next and what to prune. This module owns
what they share: where the store is, the map file's name and schema, the bitset encoding, which paths are
documentation, and WHICH MAP SERVES A BASE.

WHICH MAP SERVES A BASE (owner decision 2026-10-24, kb/Work PB3663). A map is a statement about the tree it was recorded
at: per source file, the line ranges each method owns and the tests that executed them. It therefore describes a
base EXACTLY for every file that is the same in both trees, and not at all for a file that differs. So the question is
never "is the map older than the base" (the rule this replaces called any map older by more than documentation STALE
and put every test in tier 1, while main moves about 1.6 landings an hour and a recording takes 18-25 minutes, so
nearly every base would have been stale) but "which source files differ between the map's commit and the base":
`drift`. A change to a file outside `drift` is attributed by line range as before; a change to a file in `drift` is
taken at the file level (`impacted_tests.reach_cs`). Ancestry plays no part: a map recorded at a commit NEWER than
the base serves it the same way, because the comparison is of the two trees. `nearest` picks the map with the fewest
drifted source files.

Run as a script it only runs its self-test."""
from __future__ import annotations

import base64
import fnmatch
import os
import re
import shutil
import subprocess
import sys
import time
import zlib
from pathlib import Path
from typing import Iterable, NamedTuple

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

#: The map file's schema, written by the recorder and read by the lookup.
SCHEMA = 2
STORE_DIR = "cobol-impact"
SUFFIX = ".json.gz"
#: How many of the newest maps a prune keeps whatever any worktree needs (kb/Work PB3663 bar 3). MEASURED on
#: 7d769bcc1 (2026-10-25): a map is 25.2 MB compressed (57,270 tests, 48,426 probe entries; DESIGN-test-build-ci.md
#: §3.13 said about 5 MB of an older, smaller population) and a recording took 13.6 minutes at BelowNormal. The loop
#: records at most one per adopted commit, so four maps are 100 MB and span at least four adopted commits; each live
#: worktree's base adds the map serving it. A larger number buys little: a map further behind a base differs from it
#: in more files, and `nearest` already prefers the one that differs least.
KEEP_NEWEST = 4
#: A temporary file the recorder's atomic write leaves when it dies between the write and the rename.
TMP_MARK = ".tmp"
TMP_STALE_S = 3600.0
#: The recorder's work directory is `<store>/rec-<sha12>-<epoch>/` (its logs, raw hits and instrumenter; its detached
#: worktree is removed when it ends). One older than this is a dead recording's; a younger one may be a recording a
#: person is running by hand beside the loop's, which a tidy must not break.
WORK_PREFIX = "rec-"
WORK_STALE_S = 24 * 3600.0

# Paths no test EXECUTES: they can only be READ by a test, which the lookup's reader search finds.
DOC_GLOBS = ["docs/*", "kb/*", "*.md", ".claude/*", "adjudication/*", "specs/*", "scripts/*", "tools/impact/*",
             ".github/*", "samples/*"]


def is_doc(path: str) -> bool:
    return any(fnmatch.fnmatch(path, g) for g in DOC_GLOBS) and not path.startswith(("src/", "tests/"))


# ── the bitset encoding (the recorder writes it, the lookup reads it) ─────────────────────────────────────────────────
def pack(n: int) -> str:
    """A bitset (bit i = probe entry i) in the map's own encoding: little-endian bytes, zlib, base64."""
    return base64.b64encode(zlib.compress(n.to_bytes((n.bit_length() + 7) // 8, "little"))).decode("ascii")


def unpack(b64: str) -> int:
    """A recorded bitset as a Python int: bit i set = probe entry i was reached."""
    return int.from_bytes(zlib.decompress(base64.b64decode(b64)), "little") if b64 else 0


def bit_ids(n: int):
    """The set bits of `n`, ascending — byte-wise, since shifting a 45,000-bit int bit by bit is quadratic."""
    for bi, byte in enumerate(n.to_bytes((n.bit_length() + 7) // 8, "little")):
        if byte:
            for k in range(8):
                if byte >> k & 1:
                    yield bi * 8 + k


# ── where the store is, and what is in it ─────────────────────────────────────────────────────────────────────────────
def git(repo: Path, *args: str, check: bool = True) -> str:
    p = subprocess.run(["git", *args], cwd=repo, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if check and p.returncode != 0:
        raise subprocess.CalledProcessError(p.returncode, ["git", *args], p.stdout, p.stderr)
    return p.stdout.strip() if p.returncode == 0 else ""


def default_store(repo: Path) -> Path:
    """`<git common dir>/cobol-impact`: the directory every worktree of `repo`'s repository shares."""
    common = Path(git(repo, "rev-parse", "--git-common-dir"))
    return (common if common.is_absolute() else (repo / common).resolve()) / STORE_DIR


def map_file(store: Path, sha: str) -> Path:
    return store / f"{sha}{SUFFIX}"


def mtime(p: Path) -> float | None:
    """When `p` was last written, or None when it is gone. The store is shared with the lane worker, which prunes and
    tidies it at any moment (kb/Work PB3807): a path a glob listed can vanish before it is stat'ed, and that is no
    failure of the reader, so every stat of a globbed store path goes through here."""
    try:
        return p.stat().st_mtime
    except OSError:
        return None


def stale(p: Path, now: float, limit: float) -> bool:
    """Whether `p` was last written more than `limit` seconds before `now`; a path that has vanished is not stale (there
    is nothing left to remove)."""
    written = mtime(p)
    return written is not None and now - written > limit


def maps(store: Path) -> list[tuple[str, Path]]:
    """(commit, file) of every COMPLETE map in the store, newest recorded first. A trial map (`<sha>.partial.json.gz`)
    and a recorder's temporary file are never one: a trial covers a filter's tests only. A map pruned between the glob
    and its stat is no map (`mtime`)."""
    out = []
    for p in store.glob(f"*{SUFFIX}") if store.is_dir() else []:
        sha = p.name[: -len(SUFFIX)]
        if re.fullmatch(r"[0-9a-f]{40}", sha) and (written := mtime(p)) is not None:
            out.append((written, sha, p))
    return [(sha, p) for _, sha, p in sorted(out, key=lambda t: (-t[0], t[1]))]


class Choice(NamedTuple):
    """The map that serves a base: where it is and its `drift`, the non-documentation files that differ between the
    map's commit and the base (empty: it describes the base exactly, as far as any test can tell)."""
    sha: str
    path: Path
    drift: tuple[str, ...]


def drift_between(repo: Path, sha: str, base: str) -> tuple[str, ...] | None:
    """The non-documentation files that differ between the trees of `sha` and `base`, or None when either commit is
    unknown to the repository (a map of a commit git no longer holds serves nobody)."""
    p = subprocess.run(["git", "diff", "--name-only", "-z", "--no-renames", sha, base], cwd=repo, capture_output=True,
                       text=True, encoding="utf-8", errors="replace")
    if p.returncode != 0:
        return None
    return tuple(f for f in p.stdout.split("\0") if f and not is_doc(f))


def nearest(store: Path, base: str, repo: Path) -> Choice | None:
    """The map that serves `base`: the one with the fewest drifted source files, an exact one first among equals, then
    the most recently recorded. None when the store holds no usable map."""
    best: tuple[tuple[int, int, int], Choice] | None = None
    for rank, (sha, path) in enumerate(maps(store)):
        drift = drift_between(repo, sha, base)
        if drift is None:
            continue
        key = (len(drift), 0 if sha == base else 1, rank)
        if best is None or key < best[0]:
            best = (key, Choice(sha, path, drift))
    return best[1] if best else None


# ── bounding the store ────────────────────────────────────────────────────────────────────────────────────────────────
def live_bases(repo: Path) -> list[str]:
    """The base (merge-base with origin/main, the gate's own cut point) of every worktree of the repository: the commits
    whose maps a gate may still ask for. A worktree git cannot read is skipped; a recording's own detached worktree
    (under the store) is no gate's."""
    out: list[str] = []
    for line in git(repo, "worktree", "list", "--porcelain", check=False).splitlines():
        if not line.startswith("worktree "):
            continue
        wt = Path(line[len("worktree "):])
        if STORE_DIR in wt.parts or not wt.is_dir():
            continue
        base = git(wt, "merge-base", "HEAD", "origin/main", check=False)
        if base and base not in out:
            out.append(base)
    return out


class Pruned(NamedTuple):
    """What a prune did, for the caller's log: the files it removed, and the ones it could not (a Windows reader holds
    a map it is planning from open: the next prune, after the next recording, tries again)."""
    removed: list[str]
    busy: list[str]


def prune(store: Path, repo: Path, wanted: Iterable[str], keep_newest: int = KEEP_NEWEST,
          now: float | None = None) -> Pruned:
    """Remove every complete map but the `keep_newest` most recently recorded and the one that serves each commit in
    `wanted` (the live worktrees' bases), and any temporary file a dead recorder left. Never removes a map a live base
    could use: that is the bound's whole meaning (kb/Work PB3663 bar 3)."""
    now = time.time() if now is None else now
    held = maps(store)
    keep = {sha for sha, _ in held[:keep_newest]}
    for base in wanted:
        if (c := nearest(store, base, repo)) is not None:
            keep.add(c.sha)
    out = Pruned([], [])
    doomed = [path for sha, path in held if sha not in keep]
    doomed += [p for p in store.glob(f"*{SUFFIX}*{TMP_MARK}") if stale(p, now, TMP_STALE_S)] if store.is_dir() else []
    for path in doomed:
        try:
            path.unlink(missing_ok=True)
        except OSError:
            out.busy.append(path.name)
            continue
        out.removed.append(path.name)
    return out


def work_name(sha: str, epoch: int) -> str:
    return f"{WORK_PREFIX}{sha[:12]}-{epoch}"


def clean_work(store: Path, repo: Path, sha: str, recorded: bool, now: float | None = None) -> list[str]:
    """Tidy the recorder's work directories after the recording of `sha` ended: that recording's own is removed when it
    RECORDED (a failed one's logs are kept for its diagnosis), and any other older than WORK_STALE_S is a dead
    recording's. Returns the names removed; a directory that cannot be removed stays and is not an error (the next
    tidy tries again). `git worktree prune` first, so a recording killed before it removed its detached worktree
    leaves no registration behind."""
    now = time.time() if now is None else now
    git(repo, "worktree", "prune", check=False)
    removed = []
    for d in sorted(store.glob(f"{WORK_PREFIX}*")) if store.is_dir() else []:
        own = d.name.startswith(f"{WORK_PREFIX}{sha[:12]}-")
        if (own and recorded) or (not own and stale(d, now, WORK_STALE_S)):
            try:
                shutil.rmtree(d)
            except OSError:
                continue
            removed.append(d.name)
    return removed


def self_test() -> int:
    from scratch_repo import ScratchRepo, remove_tree, scratch_dir  # noqa: PLC0415  (scripts/: the one scratch fixture)
    fails: list[str] = []

    def check(label: str, got, want) -> None:
        ok = got == want
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + ("" if ok else f"  (got {got!r}, want {want!r})"))
        if not ok:
            fails.append(label)

    check("a bitset round-trips through pack and unpack", unpack(pack(0b1010_0001 << 40)), 0b1010_0001 << 40)
    check("bit_ids lists the set bits ascending", list(bit_ids(0b1000_0101)), [0, 2, 7])
    check("docs and scripts are documentation; src and tests never are",
          [is_doc(p) for p in ("docs/a.md", "scripts/x.py", "src/WiseOwl.Cobol.Compiler/A.cs", "tests/a/b.md", "README.md")],
          [True, True, False, False, True])

    tmp = scratch_dir("impact-store-")
    try:
        repo = ScratchRepo.init(tmp / "repo")
        repo.commit("a", {"src/A.cs": "a1\n", "src/B.cs": "b1\n", "docs/d.md": "d1\n"})
        sha_a = repo.git("rev-parse", "HEAD")
        repo.commit("docs only", {"docs/d.md": "d2\n", "scripts/t.py": "t\n"})
        sha_docs = repo.git("rev-parse", "HEAD")
        repo.commit("code", {"src/A.cs": "a2\n"})
        sha_code = repo.git("rev-parse", "HEAD")
        repo.commit("more code", {"src/B.cs": "b2\n", "src/C.cs": "c\n"})
        sha_more = repo.git("rev-parse", "HEAD")
        store = default_store(repo.path)
        check("the store is under the repository's git common dir", store.name, STORE_DIR)
        store.mkdir(parents=True)
        check("an empty store serves nobody", nearest(store, sha_more, repo.path), None)

        def put(sha: str, age: float = 0.0) -> Path:
            p = map_file(store, sha)
            p.write_bytes(b"x")
            t = time.time() - age
            os.utime(p, (t, t))
            return p

        put(sha_a, 300)
        c = nearest(store, sha_docs, repo.path)
        check("a map older than the base by documentation only describes it exactly", (c.sha, c.drift), (sha_a, ()))
        c = nearest(store, sha_more, repo.path)
        check("an older map's drift is the source files that differ, not the documentation",
              (c.sha, c.drift), (sha_a, ("src/A.cs", "src/B.cs", "src/C.cs")))
        put(sha_code, 200)
        c = nearest(store, sha_more, repo.path)
        check("the map with the fewest drifted files serves the base", (c.sha, c.drift), (sha_code, ("src/B.cs", "src/C.cs")))
        put(sha_more, 100)
        c = nearest(store, sha_more, repo.path)
        check("a map at the base itself is exact", (c.sha, c.drift), (sha_more, ()))
        c = nearest(store, sha_a, repo.path)
        check("a map NEWER than the base serves it by comparing trees, ancestry plays no part",
              (c.sha, c.drift), (sha_a, ()))
        # sha_code's tree differs from sha_a's in A.cs only; sha_more's in A.cs, B.cs, C.cs
        store_only_new = tmp / "only-new"
        store_only_new.mkdir()
        map_file(store_only_new, sha_more).write_bytes(b"x")
        c = nearest(store_only_new, sha_a, repo.path)
        check("a newer-only store still serves an older base, its drift being both directions",
              (c.sha, c.drift), (sha_more, ("src/A.cs", "src/B.cs", "src/C.cs")))
        (store / f"{sha_a}.partial.json.gz").write_bytes(b"x")
        (store / "not-a-sha.json.gz").write_bytes(b"x")
        check("a trial map and a file that is no commit's are never complete maps",
              sorted(s for s, _ in maps(store)), sorted([sha_a, sha_code, sha_more]))
        # kb/Work PB3807: the lane worker prunes between a gate's glob and its stat. Path.stat is made to find a named
        # file gone exactly when the glob has already listed it.
        real_stat = Path.stat
        vanished: set[str] = set()

        def stat_unless_vanished(self, *args, **kwargs):
            if self.name in vanished:
                raise FileNotFoundError(2, "pruned between the glob and the stat", str(self))
            return real_stat(self, *args, **kwargs)

        Path.stat = stat_unless_vanished
        try:
            vanished.add(map_file(store, sha_code).name)
            check("a map pruned between the glob and its stat is no map, and the others still serve",
                  sorted(s for s, _ in maps(store)), sorted([sha_a, sha_more]))
            check("... nearest goes on without it, taking the fewest-drift map left", nearest(store, sha_code, repo.path).sha, sha_a)
            vanished.update(map_file(store, s).name for s in (sha_a, sha_more))
            check("... a store whose every map vanished serves nobody, and nearest raises nothing",
                  (maps(store), nearest(store, sha_more, repo.path)), ([], None))
            vanished.clear()
        finally:
            Path.stat = real_stat
        put("f" * 40, 400)
        check("a map of a commit git does not hold serves nobody", nearest(store, sha_more, repo.path).sha, sha_more)

        # prune: the newest N and each wanted base's map stay, the rest go, a dead recorder's temp file goes
        stale_tmp = store / f"{sha_a}.json.gz.123{TMP_MARK}"
        stale_tmp.write_bytes(b"x")
        os.utime(stale_tmp, (time.time() - 2 * TMP_STALE_S,) * 2)
        fresh_tmp = store / f"{sha_code}.json.gz.456{TMP_MARK}"
        fresh_tmp.write_bytes(b"x")
        gone = prune(store, repo.path, [sha_a], keep_newest=1)
        left = sorted(s for s, _ in maps(store))
        check("prune keeps the newest map and the map serving each live base",
              left, sorted([sha_more, sha_a]))
        check("... removes the others and a dead recorder's temp file, never a fresh temp file",
              (sorted(gone.removed) == sorted([f"{sha_code}.json.gz", f"{'f' * 40}.json.gz", stale_tmp.name]),
               gone.busy, fresh_tmp.exists()), (True, [], True))
        check("a prune with nothing to remove removes nothing", prune(store, repo.path, [sha_a], keep_newest=1), ([], []))
        # a path that cannot be unlinked (a directory named like a map; on Windows a reader's open handle does the same)
        # is reported busy, never an exception that loses the rest of the prune
        stuck = store / f"{'e' * 40}{SUFFIX}"
        (stuck / "held").mkdir(parents=True)
        os.utime(stuck, (time.time() - 900,) * 2)
        done = prune(store, repo.path, [sha_a], keep_newest=1)
        check("a map that cannot be removed is reported busy and the prune goes on",
              (done.removed, done.busy), ([], [stuck.name]))
        shutil.rmtree(stuck)
        check("live_bases of a repository with no origin/main reads none", live_bases(repo.path), [])

        # the recorder's work directories: a finished recording's own goes, a failed one's stays, an old dead one's goes
        mine, failed, dead, young = (store / work_name(sha_a, 1), store / work_name(sha_code, 2),
                                     store / work_name(sha_more, 3), store / work_name(sha_docs, 4))
        for d in (mine, failed, dead, young):
            (d / "logs").mkdir(parents=True)
        os.utime(dead, (time.time() - 2 * WORK_STALE_S,) * 2)
        gone = clean_work(store, repo.path, sha_a, recorded=True)
        check("tidy removes a RECORDED recording's work directory and a dead recording's, not a young stranger's",
              (sorted(gone), young.exists(), failed.exists()), (sorted([mine.name, dead.name]), True, True))
        gone = clean_work(store, repo.path, sha_code, recorded=False)
        check("tidy keeps the failed recording's work directory for its diagnosis", (gone, failed.exists()), ([], True))
        # kb/Work PB3807: a work directory or a temporary file the worker removed between the glob and the stat
        Path.stat = stat_unless_vanished
        try:
            vanished.update((young.name, stale_tmp.name))
            os.utime(young, (time.time() - 2 * WORK_STALE_S,) * 2)
            check("tidy skips a work directory that vanished between the glob and the stat",
                  (clean_work(store, repo.path, sha_code, recorded=False), young.exists()), ([], True))
            stale_tmp.write_bytes(b"x")
            os.utime(stale_tmp, (time.time() - 2 * TMP_STALE_S,) * 2)
            check("prune skips a temporary file that vanished between the glob and the stat",
                  (prune(store, repo.path, [sha_a], keep_newest=1), stale_tmp.exists()), (Pruned([], []), True))
        finally:
            Path.stat = real_stat
            vanished.clear()
        check("a path that is not there has no mtime and is never stale",
              (mtime(store / "absent"), stale(store / "absent", time.time(), 0.0)), (None, False))
    finally:
        remove_tree(tmp)
    print("impact_store self-test: all checks OK" if not fails else f"⛔ {len(fails)} FAILURE(S)")
    return 1 if fails else 0


if __name__ == "__main__":
    ensure_utf8()
    if "--self-test" in sys.argv[1:]:
        sys.exit(self_test())
    print(__doc__)
