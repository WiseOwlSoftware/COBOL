#!/usr/bin/env python3
"""inert_lane.py — THE INERT LANE: a change confined to paths NO TEST READS runs the audits and nothing else
(kb/Work PB2878; docs/rearchitecture/DESIGN-test-build-ci.md §3.18).

    python scripts/inert_lane.py classify --base <rev> [--head <rev-or-tree>]   # lane=inert|full, and why
    python scripts/inert_lane.py watch [--root R] [--events F] -- <command …>   # Linux: run it, report any inert read
    python scripts/inert_lane.py check                                         # every entry below names a tracked path
    python scripts/inert_lane.py --self-test

⛔ WHY (owner 2026-10-09, on a one-change landing that edited a practices file and two briefs: "Should this require a
full CI run? I would think not"). Measured over the 300 landings of 2026-10-16..23, 137 changed only files the CI
matrix's old doc-only list named, and each paid the local gate (median ~14 min: audits, a slot, a build, leg 1) and
the landing's four Linux legs; but that list was not a classification at all: `docs/` and `kb/Work/` are test INPUTS
(kb/Work PB3603: a kb/Work-only branch went RED on the Linux gate), so CI skipping the matrix for them broke the CI
invariant (kb/Work PB1957) silently. THIS MODULE IS THE ONE CLASSIFICATION, used by every runner: CI's `changes` job,
the local gate (scripts/run_gate_legs.py) and the landing's Linux gate (scripts/linux-gate.sh).

THE RULE. A path is INERT when it lies under an INERT entry and under no READ entry. A change is in the INERT LANE when
its changed paths (`git diff --name-only --no-renames`, so a rename shows BOTH names and a code file renamed into an
inert directory is not inert) are non-empty and every one is inert. The inert lane runs the audits (they always run,
on every event, in every lane; they are where a citation, a register note or a DEVLOG entry is checked) and no build
and no test leg: no test reads the change, so no test can turn red on it. Every other change is the FULL lane.

THE PROOF (`watch`, the drift test the classification needs). Every whole-assembly test run on Linux — CI's Linux test
steps and the landing's Linux gate — runs under an inotify watch of every inert path (directories included, so a test
that LISTS an inert directory is caught too), through this module: a test that opens an inert path, directly or
through a script it runs, turns the run RED naming the path. So the day a test starts reading an inert path, the
change that made it read it is red until that path leaves INERT (or gains a READ entry naming its reader). A watch
that overflows its queue proves nothing and is RED. The watch has no Windows twin (Windows has no open events); the
Linux runs are the whole population on every code change (the landing's Linux gate and CI's merge group).

What is NOT inert, and why (measured 2026-10-23 by deleting each candidate and running the three assemblies): `docs/`
(SortRuntimeDocShapeDriftTests and GateLegDriftTests read every doc; twenty Unit and Conformance classes read
CONFORMANCE.md, the rule catalog, VERSION_CHANGE_REFERENCE.md), `.claude/` and the root `*.md` files (GateLegDriftTests
walks them for mode names; ArchReviewFleetDriftTests reads the briefs; SelfTestDiscoveryDriftTests classifies the
scripts there), `kb/Work/` (the READ entry below). The conflict-marker sweep, which read every tracked file, became an
audit (scripts/conflict_markers.py) so that it no longer made every path a test input.
"""
from __future__ import annotations

import argparse
import os
import select
import struct
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]

#: ⛔ THE INERT ENTRIES — a directory prefix ending in `/`, or one exact path — each with who reads it. Nothing a test
#: reads belongs here; `watch` proves it on every Linux test run. An entry that names no tracked path is stale
#: (`check`, held by InertLaneDriftTests).
INERT: dict[str, str] = {
    "DEVLOG/": "the development log: read by scripts/devlog.py (CI's devlog step, the landing checks) and by people",
    "kb/": "the knowledge base and the work register: read by scripts/spec/work.py check (an audit) and by people",
    ".obsidian/": "the settings of the note viewer the owner reads kb/ with",
    "tools/claude-skills": "the public skills submodule's pin: read by check_practices.py (the FLEET PRACTICES audit)",
}
#: ⛔ THE READ ENTRIES — under an inert entry, but a test reads them, so a change to one is the full lane. Each names its
#: readers, so the entry leaves when they do.
READ: dict[str, str] = {
    "kb/Work/": "ClosesRowsBackLinkDriftTests and DefectiveRowCoverageDriftTests read every note through "
                "tests/_shared/WorkRegister.cs (the register-inventory cross-checks)",
}


def _under(path: str, entry: str) -> bool:
    return path.startswith(entry) if entry.endswith("/") else path == entry or path.startswith(entry + "/")


def is_inert(path: str) -> bool:
    """Whether the repo-relative POSIX `path` (a directory may end in `/`) is inert: under an INERT entry, under no READ
    entry."""
    return any(_under(path, e) for e in INERT) and not any(_under(path, e) for e in READ)


@dataclass(frozen=True)
class Lane:
    inert: bool
    reason: str

    @property
    def name(self) -> str:
        return "inert" if self.inert else "full"


def classify(paths: list[str]) -> Lane:
    """The lane of a change whose changed paths are `paths`."""
    if not paths:
        return Lane(False, "no changed path: an empty diff is not evidence that no test reads the change")
    read = [p for p in paths if not is_inert(p)]
    if read:
        return Lane(False, f"{len(read)} of {len(paths)} changed path(s) may be read by a test, e.g. {read[0]}")
    return Lane(True, f"all {len(paths)} changed path(s) are inert (under {', '.join(sorted({e for p in paths for e in INERT if _under(p, e)}))}): "
                      "no test reads them")


def changed(root: Path, base: str, head: str = "HEAD") -> list[str]:
    """The paths `head` (a commit or a tree) changes against `base`, both names of a rename. Raises CalledProcessError."""
    out = subprocess.run(["git", "-C", str(root), "diff", "--name-only", "--no-renames", "-z", base, head],
                         capture_output=True, check=True).stdout
    return [p for p in out.decode("utf-8").split("\0") if p]


def tracked(root: Path) -> list[str]:
    out = subprocess.run(["git", "-C", str(root), "ls-files", "-z"], capture_output=True, check=True).stdout
    return [p for p in out.decode("utf-8").split("\0") if p]


def check(root: Path) -> list[str]:
    """Every INERT and READ entry names at least one tracked path, and every READ entry lies under an INERT one."""
    paths = tracked(root)
    bad = [f"the {kind} entry {e!r} names no tracked path: remove it, or correct it" for kind, entries in
           (("INERT", INERT), ("READ", READ)) for e in entries if not any(_under(p, e) for p in paths)]
    bad += [f"the READ entry {e!r} lies under no INERT entry" for e in READ if not any(_under(e, i) for i in INERT)]
    return bad


# ── the watch (Linux inotify, through ctypes: no package to install on a CI runner or in WSL) ────────────────────────

IN_OPEN = 0x00000020
IN_Q_OVERFLOW = 0x00004000
IN_ISDIR = 0x40000000
EVENT = struct.Struct("iIII")
WATCH_VERDICT = "=== INERT WATCH: "


def watch_dirs(root: Path) -> list[str]:
    """The directories to watch: the directory every inert tracked FILE lies in, and an inert tracked path that is itself
    a directory (a submodule's gitlink). A watched directory reports an open of a file in it, named, and an open of the
    directory itself (a listing), unnamed; so no directory above the inert ones is watched, and the many opens there
    (the repository root's) never reach the watch."""
    dirs: set[str] = set()
    for p in tracked(root):
        if is_inert(p):
            dirs.add(p if (root / p).is_dir() else p.rpartition("/")[0])
    return sorted(d for d in dirs if (root / d).is_dir())


class Watch:
    """An inotify watch of `dirs` under `root`, recording every OPEN of an inert path (`hits`) and any queue overflow."""

    def __init__(self, root: Path, dirs: list[str]):
        import ctypes
        import ctypes.util
        self.root, self.hits, self.overflow = root, set(), False
        self.libc = ctypes.CDLL(ctypes.util.find_library("c") or "libc.so.6", use_errno=True)
        self.fd = self.libc.inotify_init1(os.O_NONBLOCK | os.O_CLOEXEC)
        if self.fd < 0:
            raise OSError(ctypes.get_errno(), "inotify_init1 failed")
        self.wd: dict[int, str] = {}
        for d in dirs:
            w = self.libc.inotify_add_watch(self.fd, str(root / d).encode(), IN_OPEN)
            if w < 0:
                raise OSError(ctypes.get_errno(), f"inotify_add_watch failed on {d or '.'} (raise fs.inotify.max_user_watches?)")
            self.wd[w] = d

    def drain(self, timeout: float) -> None:
        while select.select([self.fd], [], [], timeout)[0]:
            try:
                buf = os.read(self.fd, 1 << 16)
            except BlockingIOError:
                return
            off = 0
            while off + EVENT.size <= len(buf):
                wd, mask, _cookie, length = EVENT.unpack_from(buf, off)
                name = buf[off + EVENT.size: off + EVENT.size + length].split(b"\0", 1)[0].decode("utf-8", "replace")
                off += EVENT.size + length
                if mask & IN_Q_OVERFLOW:
                    self.overflow = True
                    continue
                d = self.wd.get(wd)
                if d is None:
                    continue
                path = "/".join(x for x in (d, name) if x)
                if mask & IN_ISDIR:
                    path += "/"
                if path and is_inert(path):
                    self.hits.add(path)
            timeout = 0

    def close(self) -> None:
        os.close(self.fd)


def watch(root: Path, command: list[str], events: Path | None, say=print) -> int:
    """Run `command` under a watch of the inert paths: its exit code when non-zero, else 1 when it opened an inert path
    or the watch overflowed, else 0. The verdict line names what was read."""
    if not sys.platform.startswith("linux"):
        say(f"{WATCH_VERDICT}NOT RUN — inotify is Linux's; this host is {sys.platform}")
        return 2
    dirs = watch_dirs(root)
    w = Watch(root, dirs)
    say(f"inert watch: {len(dirs)} director(ies) under {root} watched while `{' '.join(command)}` runs")
    if events is not None:
        events.write_text("", encoding="utf-8", newline="\n")  # the readiness mark a caller waiting on the watch reads
    child = subprocess.Popen(command)
    try:
        while child.poll() is None:
            w.drain(0.5)
        w.drain(0.2)
    finally:
        if child.poll() is None:
            child.kill()
        w.close()
    rc = child.returncode
    hits = sorted(w.hits)
    if events is not None:
        events.write_text("".join(f"{h}\n" for h in hits) + ("OVERFLOW\n" if w.overflow else ""), encoding="utf-8",
                          newline="\n")
    if w.overflow:
        say(f"{WATCH_VERDICT}RED — the inotify queue overflowed, so the watch cannot say no inert path was read "
            "(raise fs.inotify.max_queued_events) ===")
        return rc or 1
    if hits:
        for h in hits[:40]:
            say(f"    opened: {h}")
        say(f"{WATCH_VERDICT}RED — {len(hits)} inert path(s) were opened while the tests ran, so the inert lane would "
            "skip a test that reads them: give each its READ entry in scripts/inert_lane.py naming its reader, or make "
            "the reader an audit (kb/Work PB2878) ===")
        return rc or 1
    say(f"{WATCH_VERDICT}GREEN — no inert path was opened ({len(dirs)} director(ies) watched; command exit {rc}) ===")
    return rc


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────

def self_test() -> int:
    failures: list[str] = []

    def expect(label: str, got, want) -> None:
        ok = got == want
        print(f"  {'ok  ' if ok else 'FAIL'} {label}" + ("" if ok else f": got {got!r}, want {want!r}"))
        if not ok:
            failures.append(label)

    expect("a DEVLOG entry and a kb page are inert", [is_inert("DEVLOG/2428-x.md"), is_inert("kb/Spec/x.md"),
                                                      is_inert("kb/Work.base")], [True, True, True])
    expect("a register note is READ, not inert, and so is the register's directory",
           [is_inert("kb/Work/PB1.md"), is_inert("kb/Work/")], [False, False])
    expect("the pin is inert, a file under the submodule's name too; a sibling with a longer name is not",
           [is_inert("tools/claude-skills"), is_inert("tools/claude-skills/x"), is_inert("tools/claude-skills-x")],
           [True, True, False])
    expect("code, tests, docs, briefs and the root files are not inert",
           [is_inert(p) for p in ("src/a.cs", "tests/b.cs", "docs/CONFORMANCE.md", ".claude/skills/x/SKILL.md",
                                   "CLAUDE.md", "DEVLOG", "kbx/y")], [False] * 7)
    expect("an empty change is the full lane", classify([]).inert, False)
    expect("a DEVLOG-and-kb change is the inert lane", classify(["DEVLOG/pending-x.md", "kb/Spec/y.md"]).name, "inert")
    expect("one path a test may read makes the change full", classify(["DEVLOG/a.md", "kb/Work/PB1.md"]).name, "full")
    with tempfile.TemporaryDirectory(prefix="inert-lane-") as d:
        root = Path(d)

        def git(*a: str) -> str:
            return subprocess.run(["git", "-C", d, *a], capture_output=True, check=True, text=True).stdout.strip()

        git("init", "-q")
        git("config", "user.email", "self-test@example.invalid")
        git("config", "user.name", "self-test")
        for rel in ("src/a.cs", "DEVLOG/0001-x.md", "kb/Work/PB1.md", "kb/Spec/s.md"):
            (root / rel).parent.mkdir(parents=True, exist_ok=True)
            (root / rel).write_text(rel + "\n", encoding="utf-8", newline="\n")
        git("add", "-A")
        git("commit", "-q", "-m", "base")
        base = git("rev-parse", "HEAD")
        git("mv", "src/a.cs", "DEVLOG/a.cs")
        git("commit", "-q", "-m", "rename")
        expect("a code file renamed INTO an inert directory shows both names, so the change is full",
               (sorted(changed(root, base)), classify(changed(root, base)).name), (["DEVLOG/a.cs", "src/a.cs"], "full"))
        expect("the watch covers every directory an inert tracked path lies in, the register's excluded",
               watch_dirs(root), ["DEVLOG", "kb/Spec"])
        if sys.platform.startswith("linux"):
            reader = [sys.executable, "-c", "import sys; open(sys.argv[1]).read()"]
            lister = [sys.executable, "-c", "import os, sys; os.listdir(sys.argv[1])"]
            silent: list[str] = []
            expect("a command that reads an inert file is RED, naming it",
                   (watch(root, reader + [str(root / "DEVLOG" / "0001-x.md")], None, silent.append) != 0,
                    any("opened: DEVLOG/0001-x.md" in s for s in silent)), (True, True))
            silent.clear()
            expect("a command that LISTS an inert directory is RED",
                   (watch(root, lister + [str(root / "kb" / "Spec")], None, silent.append) != 0,
                    any("opened: kb/Spec/" in s for s in silent)), (True, True))
            silent.clear()
            expect("a read of a file renamed into an inert directory is caught beside a READ path's",
                   watch(root, [sys.executable, "-c", f"open({str(root / 'kb' / 'Work' / 'PB1.md')!r}).read(); "
                                f"open({str(root / 'DEVLOG' / 'a.cs')!r})"], None, silent.append), 1)
            silent.clear()
            expect("a command that reads nothing inert is GREEN and keeps its own exit code",
                   watch(root, [sys.executable, "-c", f"open({str(root / 'kb' / 'Work' / 'PB1.md')!r}).read(); "
                                "raise SystemExit(3)"], None, silent.append), 3)
        else:
            print(f"  skip the inotify arms: this host is {sys.platform} (they run in CI's audits job and the Linux gate)")
    print(f"=== INERT LANE SELF-TEST: {'FAIL — ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


def main(argv: list[str]) -> int:
    sys.stdout.reconfigure(encoding="utf-8")
    if argv[:1] == ["--self-test"]:
        return self_test()
    command: list[str] = []
    if "--" in argv:
        i = argv.index("--")
        argv, command = argv[:i], argv[i + 1:]
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = ap.add_subparsers(dest="cmd", required=True)
    c = sub.add_parser("classify")
    c.add_argument("--base", required=True)
    c.add_argument("--head", default="HEAD")
    c.add_argument("--root", type=Path, default=REPO)
    w = sub.add_parser("watch")
    w.add_argument("--root", type=Path, default=REPO)
    w.add_argument("--events", type=Path)
    k = sub.add_parser("check")
    k.add_argument("--root", type=Path, default=REPO)
    args = ap.parse_args(argv)
    if args.cmd == "classify":
        try:
            lane = classify(changed(args.root, args.base, args.head))
        except (subprocess.CalledProcessError, OSError) as e:
            lane = Lane(False, f"the change could not be listed ({e}): the full lane")
        print(f"lane={lane.name}")
        print(f"reason={lane.reason}")
        return 0
    if args.cmd == "watch":
        if not command:
            ap.error("watch needs a command after --")
        return watch(args.root.resolve(), command, args.events)
    bad = check(args.root)
    for b in bad:
        print(f"    {b}")
    n = sum(1 for p in tracked(args.root) if is_inert(p))
    print(f"=== INERT LANE CHECK: {'RED — ' + str(len(bad)) + ' stale entr(ies)' if bad else 'GREEN'} — "
          f"{n} tracked inert path(s) under {len(INERT)} INERT entr(ies), {len(READ)} READ ===")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
