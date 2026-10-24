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

THE PROOF (`watch`, the drift test the classification needs). Every whole-assembly `dotnet test` run on Linux — CI's
Linux test steps and the landing's Linux gate — runs under an inotify watch of every inert path (directories included,
so a test that LISTS an inert directory is caught too), through this module: a test that opens an inert path, directly
or through a script it runs, turns the run RED naming the path. So the day a test starts reading an inert path, the
change that made it read it is red until that path leaves INERT (or gains a READ entry naming its reader). A watch
that overflows its queue proves nothing and is RED. The watch has no Windows twin (Windows has no open events); the
Linux runs are the whole population on every code change (the landing's Linux gate and CI's merge group).

What is NOT inert, and why, measured 2026-10-23 (deleting each candidate and running the three assemblies on Windows;
then the watch over the whole Linux population): `docs/` (SortRuntimeDocShapeDriftTests and GateLegDriftTests read every
doc; twenty-odd Unit and Conformance classes read CONFORMANCE.md, the rule catalog, VERSION_CHANGE_REFERENCE.md);
`.claude/` and the root `*.md` files (GateLegDriftTests walks them for mode names, ArchReviewFleetDriftTests reads the
briefs, SelfTestDiscoveryDriftTests classifies the scripts there); the `tools/claude-skills` pin. The conflict-marker
sweep, which read every tracked file, became an audit (scripts/conflict_markers.py) so that it no longer made every
path a test input; and `kb/` with `.obsidian/` became inert once the register drift tests (which read every `kb/Work/`
note through a C# twin of `work.py`'s reader) became the audit `scripts/spec/work_inventory.py`, the retired-name sweep
of LandingPipelineDriftTests set the vault aside as its siblings do, and EcRaiseSiteRegistryDriftTests stopped reading
a note.
"""
from __future__ import annotations

import argparse
import os
import select
import struct
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]


def _under(path: str, entry: str) -> bool:
    """`path` lies under `entry`: a directory prefix ending in `/`, or one exact path (and anything below it)."""
    return path.startswith(entry) if entry.endswith("/") else path == entry or path.startswith(entry + "/")


@dataclass(frozen=True)
class Lane:
    inert: bool
    reason: str

    @property
    def name(self) -> str:
        return "inert" if self.inert else "full"


@dataclass(frozen=True)
class Classification:
    """INERT entries (each: who reads it, never a test) and READ entries under them (each: the tests that read it)."""
    inert: dict[str, str]
    read: dict[str, str] = field(default_factory=dict)

    def is_inert(self, path: str) -> bool:
        """Whether the repo-relative POSIX `path` (a directory may end in `/`) is inert: under an INERT entry, under no
        READ entry."""
        return any(_under(path, e) for e in self.inert) and not any(_under(path, e) for e in self.read)

    def classify(self, paths: list[str]) -> Lane:
        """The lane of a change whose changed paths are `paths`."""
        if not paths:
            return Lane(False, "no changed path: an empty diff is not evidence that no test reads the change")
        read = [p for p in paths if not self.is_inert(p)]
        if read:
            return Lane(False, f"{len(read)} of {len(paths)} changed path(s) may be read by a test, e.g. {read[0]}")
        under = sorted({e for p in paths for e in self.inert if _under(p, e)})
        return Lane(True, f"all {len(paths)} changed path(s) are inert (under {', '.join(under)}): no test reads them")

    def watch_dirs(self, root: Path) -> list[str]:
        """The directories to watch: the directory every inert tracked FILE lies in, and an inert tracked path that is
        itself a directory (a submodule's gitlink). A watched directory reports an open of a file in it, named, and an
        open of the directory itself (a listing), unnamed; so no directory above the inert ones is watched, and the many
        opens there (the repository root's) never reach the watch."""
        dirs: set[str] = set()
        for p in tracked(root):
            if self.is_inert(p):
                dirs.add(p if (root / p).is_dir() else p.rpartition("/")[0])
        return sorted(d for d in dirs if (root / d).is_dir())

    def check(self, root: Path) -> list[str]:
        """Every INERT and READ entry names at least one tracked path, and every READ entry lies under an INERT one."""
        paths = tracked(root)
        bad = [f"the {kind} entry {e!r} names no tracked path: remove it, or correct it" for kind, entries in
               (("INERT", self.inert), ("READ", self.read)) for e in entries if not any(_under(p, e) for p in paths)]
        bad += [f"the READ entry {e!r} lies under no INERT entry" for e in self.read
                if not any(_under(e, i) for i in self.inert)]
        return bad


#: ⛔ THE CLASSIFICATION. Nothing a test reads is INERT; `watch` proves it on every Linux test run, and an entry that
#: names no tracked path is stale (`check`, held by InertLaneDriftTests). A READ entry, when one is needed, lies under
#: an INERT entry and names the tests that read it: the path is then the full lane until they stop.
CLASSIFICATION = Classification(inert={
    "DEVLOG/": "the development log: read by scripts/devlog.py (CI's devlog step, the landing checks, its self-test's "
               "lint of every entry) and by people",
    "kb/": "the knowledge base and THE work register (kb/Work): read by scripts/spec/work.py and work_inventory.py (the "
           "gate's WORK REGISTER audit, CI's audits job: the register against the inventory), the audits that cite a "
           "note, and by people in Obsidian; no test reads it since kb/Work PB2878 (the register drift tests became "
           "work_inventory.py; the retired-name sweep and the activation-boundary registry stopped reading it)",
    ".obsidian/": "the Obsidian vault's settings: read by Obsidian alone",
})


def changed(root: Path, base: str, head: str = "HEAD") -> list[str]:
    """The paths `head` (a commit or a tree) changes against `base`, both names of a rename. Raises CalledProcessError."""
    out = subprocess.run(["git", "-C", str(root), "diff", "--name-only", "--no-renames", "-z", base, head],
                         capture_output=True, check=True).stdout
    return [p for p in out.decode("utf-8").split("\0") if p]


def tracked(root: Path) -> list[str]:
    out = subprocess.run(["git", "-C", str(root), "ls-files", "-z"], capture_output=True, check=True).stdout
    return [p for p in out.decode("utf-8").split("\0") if p]


# ── the watch (Linux inotify, through ctypes: no package to install on a CI runner or in WSL) ────────────────────────

IN_OPEN = 0x00000020
IN_Q_OVERFLOW = 0x00004000
IN_ISDIR = 0x40000000
EVENT = struct.Struct("iIII")
WATCH_VERDICT = "=== INERT WATCH: "


class Watch:
    """An inotify watch of `dirs` under `root`, recording every OPEN of a path `c` calls inert (`hits`) and any queue
    overflow (`overflow`), which leaves the watch unable to say nothing was read."""

    def __init__(self, root: Path, dirs: list[str], c: Classification):
        import ctypes
        import ctypes.util
        self.c, self.hits, self.overflow = c, set(), False
        self.libc = ctypes.CDLL(ctypes.util.find_library("c") or "libc.so.6", use_errno=True)
        self.fd = self.libc.inotify_init1(os.O_NONBLOCK | os.O_CLOEXEC)
        if self.fd < 0:
            raise OSError(ctypes.get_errno(), "inotify_init1 failed")
        self.wd: dict[int, str] = {}
        for d in dirs:
            w = self.libc.inotify_add_watch(self.fd, str(root / d).encode(), IN_OPEN)
            if w < 0:
                os.close(self.fd)
                raise OSError(ctypes.get_errno(), f"inotify_add_watch failed on {d} (raise fs.inotify.max_user_watches?)")
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
                path = "/".join(x for x in (d, name) if x) + ("/" if mask & IN_ISDIR else "")
                if self.c.is_inert(path):
                    self.hits.add(path)
            timeout = 0

    def close(self) -> None:
        os.close(self.fd)


def watch(root: Path, command: list[str], events: Path | None, say=print, c: Classification = CLASSIFICATION) -> int:
    """Run `command` under a watch of the inert paths: its exit code when non-zero, else 1 when it opened an inert path
    or the watch overflowed, else 0. The verdict line names what was read; `events` (written empty once the watch is
    installed: the readiness mark a caller waiting on it reads) receives every path read, one per line."""
    if not sys.platform.startswith("linux"):
        say(f"{WATCH_VERDICT}NOT RUN — inotify is Linux's; this host is {sys.platform} ===")
        return 2
    dirs = c.watch_dirs(root)
    w = Watch(root, dirs, c)
    say(f"inert watch: {len(dirs)} director(ies) under {root} watched while `{' '.join(command)}` runs")
    if events is not None:
        events.write_text("", encoding="utf-8", newline="\n")
    try:
        child = subprocess.Popen(command)
        try:
            while child.poll() is None:
                w.drain(0.5)
            w.drain(0.2)
        finally:
            if child.poll() is None:
                child.kill()
                child.wait()
    finally:
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
        if len(hits) > 40:
            say(f"    … and {len(hits) - 40} more{f' (all in {events})' if events else ''}")
        say(f"{WATCH_VERDICT}RED — {len(hits)} inert path(s) were opened while the tests ran, so the inert lane would "
            "skip a test that reads them: give each a READ entry in scripts/inert_lane.py naming its reader, or make "
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

    real = CLASSIFICATION
    expect("the classification: a DEVLOG entry, a note of the knowledge base (the register included) and the vault's "
           "settings are inert; code, tests, docs, briefs, the root files, the skills pin and a name that only starts "
           "like an inert directory are not",
           [real.is_inert(p) for p in ("DEVLOG/2428-x.md", "kb/Work/PB1.md", "kb/Compiler/Pipeline.md", ".obsidian/app.json",
                                        "src/a.cs", "tests/b.cs", "docs/CONFORMANCE.md", ".claude/skills/x/SKILL.md",
                                        "CLAUDE.md", "tools/claude-skills", "DEVLOG", "DEVLOGX/a.md", "kb", "kbx/a.md")],
           [True] * 4 + [False] * 10)
    # The mechanics, on a planted classification with a READ entry.
    c = Classification(inert={"DEVLOG/": "", "kb/": "", "tools/pin": ""}, read={"kb/Work/": ""})
    expect("a path under an INERT entry is inert; under a READ entry it is not, nor its directory",
           [c.is_inert("DEVLOG/a.md"), c.is_inert("kb/Spec/x.md"), c.is_inert("kb/Work.base"),
            c.is_inert("kb/Work/PB1.md"), c.is_inert("kb/Work/")], [True, True, True, False, False])
    expect("an exact-path entry covers that path and below it, never a sibling with a longer name",
           [c.is_inert("tools/pin"), c.is_inert("tools/pin/x"), c.is_inert("tools/pin-x")], [True, True, False])
    expect("an empty change is the full lane", c.classify([]).inert, False)
    expect("an all-inert change is the inert lane", c.classify(["DEVLOG/pending-x.md", "kb/Spec/y.md"]).name, "inert")
    expect("one path a test may read makes the change full", c.classify(["DEVLOG/a.md", "kb/Work/PB1.md"]).name, "full")
    from scratch_repo import ScratchRepo, scratch_dir  # noqa: PLC0415

    root = scratch_dir("inert-lane-")
    repo = ScratchRepo.init(root)
    repo.commit("base", {rel: rel + "\n" for rel in ("src/a.cs", "DEVLOG/0001-x.md", "kb/Work/PB1.md", "kb/Spec/s.md")})
    base = repo.git("rev-parse", "HEAD")
    repo.git("mv", "src/a.cs", "DEVLOG/a.cs")
    repo.commit("rename")
    expect("a code file renamed INTO an inert directory shows both names, so the change is full",
           (sorted(changed(root, base)), c.classify(changed(root, base)).name),
           (["DEVLOG/a.cs", "src/a.cs"], "full"))
    expect("the watch covers the directory of every inert tracked file, none above them, the READ ones excluded",
           c.watch_dirs(root), ["DEVLOG", "kb/Spec"])
    expect("check: an entry naming no tracked path is stale, and so is a READ entry under no INERT one",
           len(Classification(inert={"DEVLOG/": "", "gone/": ""}, read={"elsewhere/": ""}).check(root)), 3)
    if sys.platform.startswith("linux"):
        py = [sys.executable, "-c"]
        out: list[str] = []
        expect("a command that reads an inert file is RED, naming it",
               (watch(root, py + [f"open({str(root / 'DEVLOG' / '0001-x.md')!r}).read()"], None, out.append, c) != 0,
                any("opened: DEVLOG/0001-x.md" in s for s in out)), (True, True))
        out.clear()
        expect("a command that LISTS an inert directory is RED",
               (watch(root, py + [f"import os; os.listdir({str(root / 'kb' / 'Spec')!r})"], None, out.append, c) != 0,
                any("opened: kb/Spec/" in s for s in out)), (True, True))
        out.clear()
        events = root / "events.txt"
        expect("a command that reads only a READ path and code is GREEN, keeps its own exit code, and leaves an "
               "empty events file",
               (watch(root, py + [f"open({str(root / 'kb' / 'Work' / 'PB1.md')!r}).read(); raise SystemExit(3)"],
                      events, out.append, c), events.read_text(encoding="utf-8")), (3, ""))
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
            lane = CLASSIFICATION.classify(changed(args.root, args.base, args.head))
        except (subprocess.CalledProcessError, OSError) as e:
            lane = Lane(False, f"the change could not be listed ({e}): the full lane")
        print(f"lane={lane.name}")
        print(f"reason={lane.reason}")
        return 0
    if args.cmd == "watch":
        if not command:
            ap.error("watch needs a command after --")
        return watch(args.root.resolve(), command, args.events)
    bad = CLASSIFICATION.check(args.root)
    for b in bad:
        print(f"    {b}")
    n = sum(1 for p in tracked(args.root) if CLASSIFICATION.is_inert(p))
    print(f"=== INERT LANE CHECK: {'RED — ' + str(len(bad)) + ' stale entr(ies)' if bad else 'GREEN'} — "
          f"{n} tracked inert path(s) under {len(CLASSIFICATION.inert)} INERT entr(ies), "
          f"{len(CLASSIFICATION.read)} READ ===")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
