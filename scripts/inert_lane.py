#!/usr/bin/env python3
"""inert_lane.py — THE INERT LANE: a change confined to paths NO TEST READS runs the audits and nothing else
(kb/Work PB2878; docs/rearchitecture/DESIGN-test-build-ci.md §3.18).

    python scripts/inert_lane.py classify --base <rev> [--head <rev-or-tree>]   # lane=inert|full, tooling=true|false, why
    python scripts/inert_lane.py watch [--root R] [--events F] -- <command …>   # Linux: run it, report any inert read
    python scripts/inert_lane.py check                                         # every entry below names a tracked path
    python scripts/inert_lane.py --self-test

⛔ WHY (owner 2026-10-09, on a one-change landing that edited a practices file and two briefs: "Should this require a
full CI run? I would think not"). Measured over the 300 landings of 2026-10-16..23, 137 changed only files the CI
matrix's old doc-only list named, and each paid the local gate (median ~14 min: audits, a slot, a build, leg 1) and
the landing's four Linux legs; but that list was not a classification at all: `docs/` and `kb/Work/` are test INPUTS
(kb/Work PB3603: a kb/Work-only branch went RED on the Linux gate), so CI skipping the matrix for them broke the CI
invariant (kb/Work PB1957) silently. THIS MODULE IS THE ONE CLASSIFICATION, used by every runner: CI's `changes` job,
the local gate (scripts/run_gate_legs.py) and the Linux gate (scripts/linux-gate.sh).

THE RULE. A path is INERT when it lies under an INERT entry and under no READ entry. A change is in the INERT LANE when
its changed paths (`git diff --name-only --no-renames`, so a rename shows BOTH names and a code file renamed into an
inert directory is not inert) are non-empty and every one is inert. The inert lane runs the audits (the local gate runs
them in every lane, CI's merge group once per landing; they are where a citation, a register note or a DEVLOG entry is checked) and no build
and no test leg: no test reads the change, so no test can turn red on it. Every other change is the FULL lane.

THE TOOLING FLAG (kb/Work PB4326, step 3 of PB4323). A FULL-lane change also says whether it TOUCHES TOOLING: a path under
`TOOLING_UNDER` (scripts/, .github/, tools/, .claude/, and the two root files the script self-tests read through the
checkout, `.gitattributes` and `toolchain.env`), ending in a `TOOLING_SUFFIXES` extension (.py, .ps1, .sh) anywhere, or
under a `TOOLING_READS` entry: a path outside the scripts that a script self-test reads AS DATA, each entry naming its
readers (tests/_shared/TestRepo.cs for scripts/spec/drift_rules.py, the .g4 grammar for scripts/spec/grammar_source.py,
.agent-fleet.json for the orchestrator's self-tests, docs/CONFORMANCE.md for the witness-loss audit, ...). The script
self-tests (scripts/self_tests.py) read tooling and those entries, so only a change that touches one of them can turn a
self-test red: the local gate runs them (and the one self-test that reads the built assemblies) only then, and CI's
`windows-self-tests` job runs only then. An empty or unlistable change touches tooling (nothing proves it does not). Like
the inert lane, the flag is ONE classification used by every runner.

THE PROOF OF THE TOOLING FLAG (`Classification.unclassified_reads`, run by scripts/self_tests.py on every self-test it
runs and records; kb/Work PB4326, review round 1 finding R1-1). The PASS store (kb/Work PB2914) records what each
self-test OBSERVABLY read, so "a script self-test reads only tooling and the TOOLING_READS entries" is checked, not
asserted: a self-test that passes but read a tracked path that is neither is RED, naming the path and the self-test, until
the path gets a TOOLING_READS entry naming its reader. A path-prefix list alone was refuted by those stored inputs:
tests/_shared/TestRepo.cs is read by drift_rules.py's self-test, the .g4 files by grammar_source.py's, and a change to only
one of them turned a self-test red in CI's merge group after a green gate.
THE ACCEPTED RESIDUE, named (owner 2026-10-31, kb/Work PB4323: a merge-group red may be one of five first-run classes, and
the CI invariant is weakened knowingly): (a) a WHOLE-TREE reader, a self-test that ran a git command over the whole
repository (the tracer records a read of directory ""), reads the tree as a population (test_plan_wave.py and
test_dispatch_guard.py walk every markdown file, impacted_tests.py every path), so no set of paths bounds it and the proof
does not apply to it; (b) the INERT corpora, DEVLOG/ and kb/, which devlog.py and the register's self-tests read as
fixtures: every landing adds to both, the audits that govern an entry (`devlog.py check`, `work.py check`) run in every
lane, and a change to how they are read is a script edit; (c) a directory LISTING (`entries:`) records names, which
change when a file is added or removed and not when any listed file is edited, so the proof checks the files a self-test
read and its `paths:` and `content:` reads of a directory.

THE PROOF (`watch`, the drift test the classification needs). Every whole-assembly `dotnet test` run on Linux — CI's
Linux test steps and the Linux gate — runs under an inotify watch of every inert path (directories included,
so a test that LISTS an inert directory is caught too), through this module: a test that opens an inert path, directly
or through a script it runs, turns the run RED naming the path. So the day a test starts reading an inert path, the
change that made it read it is red until that path leaves INERT (or gains a READ entry naming its reader). A watch
that overflows its queue proves nothing and is RED. The watch has no Windows twin (Windows has no open events); the
Linux runs are the whole population on every code change (the Linux gate and CI's merge group).

What is NOT inert, and why, measured 2026-10-23 (deleting each candidate and running the three assemblies on Windows;
then the watch over the whole Linux population): `docs/` (SortRuntimeDocShapeDriftTests and GateLegDriftTests read every
doc; twenty-odd Unit and Conformance classes read CONFORMANCE.md, the rule catalog, VERSION_CHANGE_REFERENCE.md);
`.claude/` and the root `*.md` files (GateLegDriftTests walks them for mode names, ArchReviewFleetDriftTests reads the
briefs, SelfTestDiscoveryDriftTests classifies the scripts there); the `tools/claude-skills` pin. The conflict-marker
sweep, which read every tracked file, became an audit (scripts/conflict_markers.py) so that it no longer made every
path a test input; and `kb/` became inert once the register drift tests (which read every `kb/Work/`
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
from collections.abc import Iterable
from dataclasses import dataclass, field
from pathlib import Path
from standard_streams import ensure_utf8

REPO = Path(__file__).resolve().parents[1]


def _under(path: str, entry: str) -> bool:
    """`path` lies under `entry`: a directory prefix ending in `/`, or one exact path (and anything below it)."""
    return path.startswith(entry) if entry.endswith("/") else path == entry or path.startswith(entry + "/")


@dataclass(frozen=True)
class Lane:
    inert: bool
    reason: str
    #: The change touches tooling (module docstring): the script self-tests are the gate's to run. Never set on the inert
    #: lane, which runs the audits and nothing else.
    tooling: bool

    @property
    def name(self) -> str:
        return "inert" if self.inert else "full"


@dataclass(frozen=True)
class Classification:
    """INERT entries (each: who reads it, never a test) and READ entries under them (each: the tests that read it)."""
    inert: dict[str, str]
    read: dict[str, str] = field(default_factory=dict)
    #: What counts as TOOLING (module docstring): directory prefixes and exact paths, and file extensions.
    tooling_under: tuple[str, ...] = ()
    tooling_suffixes: tuple[str, ...] = ()
    #: Paths outside the scripts that a script self-test reads as data (module docstring): entry -> its readers.
    tooling_reads: dict[str, str] = field(default_factory=dict)

    def is_tooling(self, path: str) -> bool:
        """Whether the repo-relative POSIX `path` is tooling: under a TOOLING_UNDER or TOOLING_READS entry, or of a
        TOOLING_SUFFIXES extension."""
        return (any(_under(path, e) for e in self.tooling_under) or path.endswith(self.tooling_suffixes)
                or any(_under(path, e) for e in self.tooling_reads))

    def unclassified_reads(self, files: Iterable[str], trees: Iterable[str]) -> list[str]:
        """THE PROOF OF THE TOOLING FLAG (module docstring): of what one self-test read, the tracked paths a change to
        which the flag would NOT see. `files` are the paths it opened; `trees` are its recorded directory reads, each
        `<kind>:<directory>` (self_test_inputs.Deps). A path is seen when it is tooling or lies under an INERT entry (the
        accepted corpora); a directory listing (`entries:`) and the whole tree (`paths:`/`content:` of "") are not
        judged here (the residue, module docstring)."""
        def seen(p: str) -> bool:
            return self.is_tooling(p) or any(_under(p, e) for e in self.inert)

        bad = [p for p in files if not seen(p)]
        for key in trees:
            kind, _, d = key.partition(":")
            if kind in ("paths", "content") and d and not seen(d + "/"):
                bad.append(d + "/")
        return sorted(set(bad))

    def is_inert(self, path: str) -> bool:
        """Whether the repo-relative POSIX `path` (a directory may end in `/`) is inert: under an INERT entry, under no
        READ entry."""
        return any(_under(path, e) for e in self.inert) and not any(_under(path, e) for e in self.read)

    def classify(self, paths: list[str]) -> Lane:
        """The lane of a change whose changed paths are `paths`."""
        if not paths:
            return Lane(False, "no changed path: an empty diff is not evidence that no test reads the change", True)
        read = [p for p in paths if not self.is_inert(p)]
        if read:
            tooling = [p for p in paths if self.is_tooling(p)]
            return Lane(False, f"{len(read)} of {len(paths)} changed path(s) may be read by a test, e.g. {read[0]}"
                        + (f"; {len(tooling)} touch(es) tooling, e.g. {tooling[0]}" if tooling else "; none touches tooling"),
                        bool(tooling))
        under = sorted({e for p in paths for e in self.inert if _under(p, e)})
        return Lane(True, f"all {len(paths)} changed path(s) are inert (under {', '.join(under)}): no test reads them", False)

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
        """Every INERT, READ and TOOLING_READS entry names at least one tracked path, and every READ entry lies under an
        INERT one."""
        paths = tracked(root)
        bad = [f"the {kind} entry {e!r} names no tracked path: remove it, or correct it" for kind, entries in
               (("INERT", self.inert), ("READ", self.read), ("TOOLING_READS", self.tooling_reads)) for e in entries
               if not any(_under(p, e) for p in paths)]
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
           "note, and by people; no test reads it since kb/Work PB2878 (the register drift tests became "
           "work_inventory.py; the retired-name sweep and the activation-boundary registry stopped reading it)",
}, tooling_under=("scripts/", ".github/", "tools/", ".claude/", ".gitattributes", "toolchain.env"),
   tooling_suffixes=(".py", ".ps1", ".sh"),
   # ⛔ TOOLING_READS: what the script self-tests read outside the scripts (module docstring), measured from the PASS store's
   # recorded inputs and held complete by `unclassified_reads` on every run. An entry names its readers; a directory
   # entry is a directory the readers read as data (its files, or its names through git). Narrow by design: a new
   # entry makes every change to its path a tooling change, which runs the self-tests.
   tooling_reads={
       "WiseOwl.Cobol.sln": "scripts/arch/census.py, scripts/linux-gate.sh, scripts/orchestrator/test_orchestrate_account.ps1",
       "LICENSE": "scripts/arch/census.py (read on Linux)",
       ".gitmodules":"scripts/devlog.py, scripts/orchestrator/landing_devlog.py, scripts/hooks/test_forbidden_commands.py",
       ".agent-fleet.json": "scripts/spec/ledger_plan.py, scripts/orchestrator/test_orchestrate_adopt.ps1 and _ledger.ps1",
       "conformance-suite/adapters/": "scripts/differential_fuzz.py (the GnuCOBOL and WiseOwl adapter files)",
       "conformance-suite/harness/": "scripts/line_endings.py, live_tools.py, temp_names.py and the harness self-tests (the "
                                     "names of the harness files)",
       "docs/CONFORMANCE.md": "scripts/spec/audit_witness_loss.py, scripts/differential_fuzz.py",
       "docs/VERSION_CHANGE_REFERENCE.md": "scripts/orchestrator/test_orchestrate_account.ps1",
       "docs/rearchitecture/spec-rule-catalog.json": "scripts/spec/audit_annex_a1.py, audit_code_citations.py",
       "docs/rearchitecture/evidence/fleet-optimization/": "scripts/orchestrator/landing_measure.py, scripts/test_fuzz_triage.py",
       "specs/ISO_COBOL.md": "scripts/spec/cite.py, audit_code_citations.py, audit_doc_citations.py, "
                             "scripts/differential_fuzz.py",
       "src/WiseOwl.Cobol.Cli/WiseOwl.Cobol.Cli.csproj": "scripts/guard-compiler.sh, guard-verify.sh, linux-gate.sh",
       "src/WiseOwl.Cobol.Frontend/Grammar/": "scripts/spec/grammar_source.py (the grammar files, whole)",
       "tests/WiseOwl.Cobol.Benchmarks/GeneratedCode/GeneratedProgramBenchmarks.cs": "scripts/arch/perf_baseline.py",
       "tests/WiseOwl.Cobol.Benchmarks/Programs/": "scripts/arch/perf_baseline.py",
       "tests/WiseOwl.Cobol.Tests.Characterization/WiseOwl.Cobol.Tests.Characterization.csproj": "scripts/linux-gate.sh",
       "tests/WiseOwl.Cobol.Tests.Conformance/WiseOwl.Cobol.Tests.Conformance.csproj": "scripts/linux-gate.sh",
       "tests/WiseOwl.Cobol.Tests.Unit/WiseOwl.Cobol.Tests.Unit.csproj": "scripts/linux-gate.sh",
       "tests/WiseOwl.Cobol.Tests.Unit/CarrierLandingFormTests.cs": "scripts/spec/audit_annex_a1.py",
       "tests/_shared/TestRepo.cs": "scripts/spec/drift_rules.py",
       "tests/_shared/SpecClauseText.cs": "scripts/spec/spec_headings.py",
       "tests/nist/corpus.tsv": "scripts/guard-nist-audit.sh, guard-verify.sh, linux-gate.sh, "
                                "scripts/orchestrator/test_orchestrate_account.ps1",
       "tests/version-matrix/": "the inventory and its schema: scripts/spec/audit_*.py, ledger_plan.py, "
                                "scripts/orchestrator/land_lane.py, test_ledger_state.py, the rename audits and "
                                "the eleven test_orchestrate_*.ps1 parts",
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
    expect("the classification: a DEVLOG entry and a note of the knowledge base (the register included) are "
           "inert; code, tests, docs, briefs, the root files, the skills pin and a name that only starts "
           "like an inert directory are not",
           [real.is_inert(p) for p in ("DEVLOG/2428-x.md", "kb/Work/PB1.md", "kb/Compiler/Pipeline.md",
                                        "src/a.cs", "tests/b.cs", "docs/CONFORMANCE.md", ".claude/skills/x/SKILL.md",
                                        "CLAUDE.md", "tools/claude-skills", "DEVLOG", "DEVLOGX/a.md", "kb", "kbx/a.md")],
           [True] * 3 + [False] * 10)
    # The mechanics, on a planted classification with a READ entry.
    c = Classification(inert={"DEVLOG/": "", "kb/": "", "tools/pin": ""}, read={"kb/Work/": ""})
    expect("a path under an INERT entry is inert; under a READ entry it is not, nor its directory",
           [c.is_inert("DEVLOG/a.md"), c.is_inert("kb/Spec/x.md"), c.is_inert("kb/Workshop.md"),
            c.is_inert("kb/Work/PB1.md"), c.is_inert("kb/Work/")], [True, True, True, False, False])
    expect("an exact-path entry covers that path and below it, never a sibling with a longer name",
           [c.is_inert("tools/pin"), c.is_inert("tools/pin/x"), c.is_inert("tools/pin-x")], [True, True, False])
    expect("an empty change is the full lane", c.classify([]).inert, False)
    expect("an all-inert change is the inert lane", c.classify(["DEVLOG/pending-x.md", "kb/Spec/y.md"]).name, "inert")
    expect("one path a test may read makes the change full", c.classify(["DEVLOG/a.md", "kb/Work/PB1.md"]).name, "full")
    # The tooling flag (PB4326), on the real classification.
    expect("tooling: scripts, workflows, tools, the agent definitions, the two root files the self-tests read, any "
           "Python, PowerShell or shell file anywhere, and the data the self-tests read (TOOLING_READS: the test helper "
           "TestRepo.cs, the grammar, the agent-fleet settings, the inventory, docs/CONFORMANCE.md) are tooling; "
           "code, tests, the other docs and a name that only starts like a tooling directory or a read file are not",
           [real.is_tooling(p) for p in ("scripts/gate_slot.py", ".github/workflows/build-and-test.yml", "tools/claude-skills",
                                         ".claude/agents/x.md", ".gitattributes", "toolchain.env", "conformance-suite/harness/x.py",
                                         "tests/x/run.sh", "tests/_shared/TestRepo.cs",
                                         "src/WiseOwl.Cobol.Frontend/Grammar/Core/Data.g4", ".agent-fleet.json",
                                         "tests/version-matrix/inventory/GR-4.4-1.json", "docs/CONFORMANCE.md",
                                         "src/a.cs", "tests/b.cs", "docs/DESIGN.md", "scriptsx/a.md", "toolchain.env.bak",
                                         "tests/_shared/TestRepoX.cs", ".agent-fleet.json.bak", "tests/_shared/Other.cs")],
           [True] * 13 + [False] * 8)
    expect("a full-lane change that touches tooling says so, one that does not says so, and an inert one never does; "
           "a change to only a file a script self-test reads is a tooling change (review R1-1)",
           [real.classify(["src/a.cs", "scripts/x.py"]).tooling, real.classify(["src/a.cs", "docs/x.md"]).tooling,
            real.classify(["kb/x.py", "DEVLOG/a.md"]).tooling, real.classify(["kb/x.py", "DEVLOG/a.md"]).inert,
            real.classify(["tests/_shared/TestRepo.cs"]).tooling, real.classify(["src/a.cs", "tests/b.cs"]).tooling],
           [True, False, False, True, True, False])
    tc = Classification(inert={"kb/": ""}, tooling_under=("scripts/",), tooling_suffixes=(".py",),
                        tooling_reads={"data/a.json": "x", "lib/": "y"})
    expect("the proof of the flag: of what a self-test read, the paths neither tooling, nor a TOOLING_READS entry, nor "
           "inert are named, a directory read by git too; a directory listing and the whole tree are not judged",
           tc.unclassified_reads(["scripts/a.py", "data/a.json", "lib/x/y", "kb/Work/PB1.md", "docs/z.md", "src/q.cs"],
                                 ["paths:lib", "paths:other", "content:kb/Work", "entries:docs", "content:", "paths:"]),
           ["docs/z.md", "other/", "src/q.cs"])
    expect("the proof of the flag finds nothing in what a self-test read when every path is classified",
           tc.unclassified_reads(["scripts/a.py", "data/a.json"], ["paths:scripts", "content:lib"]), [])
    expect("an empty change touches tooling: nothing proves it does not", real.classify([]).tooling, True)
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
            lane = Lane(False, f"the change could not be listed ({e}): the full lane", True)
        print(f"lane={lane.name}")
        print(f"tooling={'true' if lane.tooling else 'false'}")
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
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
