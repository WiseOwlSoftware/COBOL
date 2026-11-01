#!/usr/bin/env python3
"""inert_lane.py — THE LANES: a change confined to paths NO TEST READS runs the audits and nothing else, and a change
confined to tooling NO COMPILER TEST OR BUILD READS runs the audits and the script self-tests, with no build (kb/Work
PB2878, PB4395; docs/rearchitecture/DESIGN-test-build-ci.md §3.18).

    python scripts/inert_lane.py classify --base <rev> [--head <rev-or-tree>]   # lane=inert|tooling|full, tooling=…, why
    python scripts/inert_lane.py watch [--root R] [--events F] -- <command …>   # Linux: run it, report any watched read
    python scripts/inert_lane.py check     # THE INERT LANE audit: every entry names a tracked path, every build step
                                           # and every compiler test that runs a script is CODE_READS
    python scripts/inert_lane.py --self-test

⛔ WHY (owner 2026-10-09, on a one-change landing that edited a practices file and two briefs: "Should this require a
full CI run? I would think not"). Measured over the 300 landings of 2026-10-16..23, 137 changed only files the CI
matrix's old doc-only list named, and each paid the local gate (median ~14 min: audits, a slot, a build, leg 1) and
the landing's four Linux legs; but that list was not a classification at all: `docs/` and `kb/Work/` are test INPUTS
(kb/Work PB3603: a kb/Work-only branch went RED on the Linux gate), so CI skipping the matrix for them broke the CI
invariant (kb/Work PB1957) silently. THIS MODULE IS THE ONE CLASSIFICATION, used by every runner: CI's `changes` job,
the local gate (scripts/gate.py) and the Linux gate (scripts/linux-gate.sh).

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

THE TOOLING LANE (kb/Work PB4395; PIPELINE-REVIEW.md §13, owner-approved: "a tooling change (`scripts/`, `.github/`,
`tools/`, `.claude/`, any `*.py`/`*.ps1`/`*.sh`) runs the audits and the self-tests whose recorded inputs it touches";
owner 2026-11-01: "Build the approved lane"). A change is in the TOOLING LANE when every changed path that is not inert is
a TOOLING-LANE path: in a tooling tree (`TOOLING_UNDER`) and under no `CODE_READS` entry. Its
gate runs the audits with the script self-tests and nothing else: no corpus fetch, no slot, no build, no test host, since
no compiler test reads the change. `CODE_READS` is the tooling the compiler's BUILD or its TESTS read (each entry naming
its readers): the scripts a csproj `Exec` runs, a generator or extractor whose output a C# test checks, the one script
whose self-test reads the built assemblies (`SELF-TEST-NEEDS: build`), and `.gitattributes` and `toolchain.env` (new
checkout bytes or a new SDK change what every compiler test runs on). A change to one of them is a compiler change, so it
is the FULL (code) lane, with the self-tests beside it. A script file outside the tooling trees (conformance-suite/harness/,
a `.sh` under tests/, docs/…/evidence/) and a `TOOLING_READS` entry (the grammar, a csproj, docs/CONFORMANCE.md,
tests/_shared/TestRepo.cs) lie in trees the compiler's tests and build read, so they are never tooling-lane paths.
THE PROOF OF THE TOOLING LANE is the same watch as the inert lane's (below): every Linux test run also watches every
tooling-lane path, so a compiler test that opens one, directly or through a script it runs, is RED naming it, until the
path gets a CODE_READS entry naming its reader (or the reader becomes a script's self-test, kb/Work PB4395 item 1). The
`check` audit is its static twin, on every host: every csproj `Exec` of a script, every script whose self-test needs a
build and every script a compiler test names through `TestRepo.Scripts(…)` lies under a CODE_READS entry. The residue,
named: a Windows-only compiler test that reads a tooling path through a computed name (the watch runs on Linux only).

THE PROOF OF THE TOOLING FLAG (`Classification.unclassified_reads`, run by scripts/self_tests.py on every self-test it
runs and records; kb/Work PB4326, review round 1 finding R1-1). The PASS store (kb/Work PB2914) records what each
self-test OBSERVABLY read, so "a script self-test reads only tooling and the TOOLING_READS entries" is checked, not
asserted: a self-test that passes but read a tracked path that is neither is RED, naming the path and the self-test, until
the path gets a TOOLING_READS entry naming its reader. A path-prefix list alone was refuted by those stored inputs:
tests/_shared/TestRepo.cs is read by drift_rules.py's self-test, the .g4 files by grammar_source.py's, and a change to only
one of them turned a self-test red in CI's merge group after a green gate.
THE ACCEPTED RESIDUE, named (owner 2026-10-31, kb/Work PB4323: a merge-group red may be one of five first-run classes, and
the CI invariant is weakened knowingly): (a) a DECLARED POPULATION reader: a self-test whose first
lines say `# SELF-TEST-POPULATION: tree — <why>` reads the tracked tree as a population (test_plan_wave.py and
test_dispatch_guard.py walk every markdown file), so no set of paths bounds it and the proof
does not apply to it. The declaration is checked against the trace (scripts/self_tests.py): one that ran no git command over
the whole repository (the tracer records a read of directory "") is RED, and a self-test that ran such a command WITHOUT
the declaration is judged like any other, so its specific reads (work.py's docs/DESIGN.md, merge_drivers.py's ABI pin) are
TOOLING_READS entries, not a blind spot (review round 2, finding R2-1); (b) the INERT corpora, DEVLOG/ and kb/, which devlog.py and the register's self-tests read as
fixtures: every landing adds to both, the audits that govern an entry (`devlog.py check`, `work.py check`) run in every
lane, and a change to how they are read is a script edit; (c) a directory LISTING (`entries:`) records names, which
change when a file is added or removed and not when any listed file is edited, so the proof checks the files a self-test
read and its `paths:` and `content:` reads of a directory.

THE PROOF (`watch`, the drift test the classification needs). Every whole-assembly `dotnet test` run on Linux — CI's
Linux test steps and the Linux gate — runs under an inotify watch of every inert path and every tooling-lane path
(directories included, so a test that LISTS a watched directory is caught too; a submodule's whole checked-out tree), through
this module: a test that opens one, directly or through a script it runs, turns the run RED naming the path. So the day a
test starts reading an inert path, the change that made it read it is red until that path leaves INERT (or gains a READ
entry naming its reader); a tooling-lane one, until it gains a CODE_READS entry. The watched command runs with a fresh
Python bytecode cache (`PYTHONPYCACHEPREFIX`), so a script a test runs is opened as source at least once per run, never
served unseen from a `__pycache__` an earlier run left. A watch that overflows its queue proves nothing and is RED. The watch has no Windows twin (Windows has no open events); the
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
import re
import select
import shutil
import struct
import subprocess
import sys
import tempfile
from collections.abc import Callable, Iterable
from dataclasses import dataclass, field
from pathlib import Path
from standard_streams import ensure_utf8

REPO = Path(__file__).resolve().parents[1]


def _under(path: str, entry: str) -> bool:
    """`path` lies under `entry`: a directory prefix ending in `/`, or one exact path (and anything below it)."""
    return path.startswith(entry) if entry.endswith("/") else path == entry or path.startswith(entry + "/")


#: ⛔ THE LANES, cheapest first (module docstring): `inert` runs the audits; `tooling` the audits with the script
#: self-tests; `full` (the code lane) the build and the three whole populations beside the audits.
LANES = ("inert", "tooling", "full")
#: The lanes whose gate runs no compiler test, so its record keeps no test result: no compiler test reads the change.
TEST_FREE_LANES = frozenset({"inert", "tooling"})


@dataclass(frozen=True)
class Lane:
    #: One of LANES.
    name: str
    reason: str
    #: The change touches tooling (module docstring): the script self-tests are the gate's to run. Never set on the inert
    #: lane, which runs the audits and nothing else; always set on the tooling lane.
    tooling: bool

    def __post_init__(self) -> None:
        if self.name not in LANES or (self.name == "inert" and self.tooling) or (self.name == "tooling" and not self.tooling):
            raise ValueError(f"no such lane: {self.name!r} with tooling={self.tooling}")

    @property
    def inert(self) -> bool:
        return self.name == "inert"

    @property
    def builds(self) -> bool:
        """The code lane: a build and the three whole populations."""
        return self.name == "full"


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
    #: Tooling the compiler's build or tests read (module docstring, THE TOOLING LANE): entry -> its readers.
    code_reads: dict[str, str] = field(default_factory=dict)

    def is_script_tree(self, path: str) -> bool:
        """Under a TOOLING_UNDER entry or of a TOOLING_SUFFIXES extension: the scripts and their trees."""
        return any(_under(path, e) for e in self.tooling_under) or path.endswith(self.tooling_suffixes)

    def in_tooling_tree(self, path: str) -> bool:
        """Under a TOOLING_UNDER entry: the trees that hold nothing but tooling."""
        return any(_under(path, e) for e in self.tooling_under)

    def in_tooling_lane(self, path: str) -> bool:
        """Whether a change to `path` can stay in the tooling lane: it lies in a tooling tree and no compiler test or the
        build reads it (under no CODE_READS entry). A script file elsewhere (conformance-suite/harness/, tests/, src/,
        docs/) and a TOOLING_READS entry lie in trees the compiler's tests and build read, so they never are (module
        docstring)."""
        return self.in_tooling_tree(path) and not any(_under(path, e) for e in self.code_reads)

    def is_watched(self, path: str) -> bool:
        """What the watch reports when a compiler test opens it: an inert path or a tooling-lane path."""
        return self.is_inert(path) or self.in_tooling_lane(path)

    def is_tooling(self, path: str) -> bool:
        """Whether the repo-relative POSIX `path` is tooling: under a TOOLING_UNDER or TOOLING_READS entry, or of a
        TOOLING_SUFFIXES extension."""
        return self.is_script_tree(path) or any(_under(path, e) for e in self.tooling_reads)

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
            return Lane("full", "no changed path: an empty diff is not evidence that no test reads the change", True)
        read = [p for p in paths if not self.is_inert(p)]
        if not read:
            under = sorted({e for p in paths for e in self.inert if _under(p, e)})
            return Lane("inert", f"all {len(paths)} changed path(s) are inert (under {', '.join(under)}): no test reads "
                        "them", False)
        code = [p for p in read if not self.in_tooling_lane(p)]
        if not code:
            return Lane("tooling", f"{len(read)} of {len(paths)} changed path(s) are tooling no compiler test or build "
                        f"reads, e.g. {read[0]}, and the rest inert: the audits and the script self-tests", True)
        tooling = [p for p in paths if self.is_tooling(p)]
        return Lane("full", f"{len(code)} of {len(paths)} changed path(s) may be read by a compiler test or the build, "
                    f"e.g. {code[0]}"
                    + (f"; {len(tooling)} touch(es) tooling, e.g. {tooling[0]}" if tooling else "; none touches tooling"),
                    bool(tooling))

    def watch_dirs(self, root: Path) -> list[str]:
        """The directories to watch: the directory every watched tracked FILE lies in, and a watched tracked path that
        is itself a directory (a submodule's gitlink) with every directory of its checked-out tree (inotify does not
        recurse). A watched directory reports an open of a file in it, named, and an open of the directory itself (a
        listing), unnamed; so no directory above the watched ones is watched, and the many opens there (the repository
        root's) never reach the watch."""
        dirs: set[str] = set()
        for p in tracked(root):
            if not self.is_watched(p):
                continue
            if (root / p).is_dir():
                for d, subdirs, _files in os.walk(root / p):
                    subdirs[:] = [s for s in subdirs if s != ".git"]
                    dirs.add(Path(d).relative_to(root).as_posix())
            else:
                dirs.add(p.rpartition("/")[0])
        return sorted(d for d in dirs if d and (root / d).is_dir())

    def check(self, root: Path) -> list[str]:
        """THE INERT LANE audit (`check`): every INERT, READ, TOOLING_READS and CODE_READS entry names at least one
        tracked path; every READ entry lies under an INERT one and every CODE_READS entry is a script-tree path; and
        every tooling the build or a compiler test is SEEN to read (`code_readers`) lies under a CODE_READS entry."""
        paths = tracked(root)
        bad = [f"the {kind} entry {e!r} names no tracked path: remove it, or correct it" for kind, entries in
               (("INERT", self.inert), ("READ", self.read), ("TOOLING_READS", self.tooling_reads),
                ("CODE_READS", self.code_reads)) for e in entries
               if not any(_under(p, e) for p in paths)]
        bad += [f"the READ entry {e!r} lies under no INERT entry" for e in self.read
                if not any(_under(e, i) for i in self.inert)]
        bad += [f"the CODE_READS entry {e!r} lies in no tooling tree (TOOLING_UNDER), so it changes no lane: remove it"
                for e in self.code_reads if not self.in_tooling_tree(e)]
        for path, reader in code_readers(root, paths, self.in_tooling_tree):
            if self.in_tooling_lane(path):
                bad.append(f"{reader} reads {path}, which is in the tooling lane: a change to it would gate with no build "
                           "and no compiler test; give it a CODE_READS entry naming that reader, or make the reader a "
                           "script's self-test (kb/Work PB4395)")
        return bad


#: ⛔ THE CLASSIFICATION. Nothing a test reads is INERT; `watch` proves it on every Linux test run, and an entry that
#: names no tracked path is stale (`check`, THE INERT LANE audit of every gate). A READ entry, when one is needed, lies under
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
       "Directory.Build.props": "scripts/merge_drivers.py (settling the ABI pin)",
       "src/WiseOwl.Cobol.Runtime/Control/RuntimeAbi.cs": "scripts/merge_drivers.py (settling the ABI pin)",
       "src/WiseOwl.Cobol.Runtime/RuntimeAbi.pin": "scripts/merge_drivers.py (settling the ABI pin)",
       "docs/DESIGN.md": "scripts/spec/work.py (the site rule: a Markdown file named by its section number)",
       "docs/REARCHITECTURE_PLAN.md": "scripts/orchestrator/test_orchestrate_steer.ps1 and test_orchestrate_stop.ps1",
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
   },
   # ⛔ CODE_READS: the tooling the compiler's build or tests read (module docstring, THE TOOLING LANE), held by the
   # watch on every Linux test run and by `check` (the INERT LANE audit) on every host. An entry names its readers. A
   # whole-tree entry names the pipeline classes still in the compiler's test assembly that walk it (kb/Work PB4395
   # item 1 moves each into a script's self-test, and the entry narrows when its last walker leaves).
   code_reads={
       ".gitattributes": "every compiler test: the checkout's bytes (line endings) of every file it reads",
       "toolchain.env": "every compiler test and the build: the SDK they run on (the toolchain manifest, kb/Work PB4007)",
       "scripts/": "the pipeline classes that walk every script (GateLegDriftTests, FilterPopulationGuardDriftTests, "
                   "PythonInterpreterDriftTests, TestLegReportDriftTests, ExternalCorpusFetchTests) and the "
                   "Conformance assembly's CorpusManifestTests; scripts/test_population.py runs every Linux gate leg; "
                   "the generators and extractors whose output a compiler test checks (gen-reserved-words.ps1, "
                   "gen-cobol-words.ps1, gen-constructs.ps1, gen-diagnostics-doc.ps1, scripts/spec/, arch/census.py); "
                   "the build's Exec steps (gen-vault-reference.ps1, gen-grammar-diagrams.ps1); filter_population.py, "
                   "whose self-test reads the built assemblies",
       ".claude/": "GateLegDriftTests (it walks every skill, agent and workflow for mode names)",
       ".github/": "GateLegDriftTests, DotnetSdkInstallDriftTests (every workflow and action), the CI drift classes",
   })


#: A build step that runs a script: an MSBuild `<Exec Command="…">`, and a property it names.
EXEC_RE = re.compile(r"<Exec\b[^>]*\bCommand=\"(?P<command>[^\"]*)\"")
PROPERTY_RE = re.compile(r"<(?P<name>[A-Za-z_][\w.]*)>(?P<value>[^<]*)</(?P=name)>")
MSBUILD_REF_RE = re.compile(r"\$\((?P<name>[\w.]+)\)")
SCRIPT_TOKEN_RE = re.compile(r"[\w./\\-]+\.(?:ps1|py|sh)\b")
#: A compiler test naming a repository path through the one helper (tests/_shared/TestRepo.cs): the method, then its
#: arguments up to the closing parenthesis (a nested call ends the match, and its literal prefix is what is read).
TEST_REPO_RE = re.compile(r"\bTestRepo\.(?P<method>Scripts|At|Src|Tests|Docs|Suite)\((?P<args>[^()]*)\)?")
TEST_REPO_PREFIX = {"Scripts": "scripts", "At": "", "Src": "src", "Tests": "tests", "Docs": "docs",
                    "Suite": "conformance-suite"}
STRING_LITERAL_RE = re.compile(r'^\s*"(?P<s>[^"\\{}]*)"\s*$')


def _build_readers(root: Path, paths: list[str]) -> Iterable[tuple[str, str]]:
    """Every script an MSBuild `Exec` of a tracked project or props/targets file runs, resolved against the properties
    of the same file and the project's directory (`$(MSBuildProjectDirectory)`, `$(MSBuildThisFileDirectory)`)."""
    for rel in paths:
        if not rel.endswith((".csproj", ".props", ".targets")):
            continue
        try:
            text = (root / rel).read_text(encoding="utf-8")
        except OSError:
            continue
        here = rel.rpartition("/")[0]
        props = {m.group("name"): m.group("value").strip() for m in PROPERTY_RE.finditer(text)}
        props |= {"MSBuildProjectDirectory": here, "MSBuildThisFileDirectory": here + "/" if here else ""}
        for m in EXEC_RE.finditer(text):
            command = m.group("command").replace("&quot;", '"')
            for _ in range(4):  # a property may name another
                command = MSBUILD_REF_RE.sub(lambda r: props.get(r.group("name"), r.group(0)), command)
            for token in SCRIPT_TOKEN_RE.findall(command):
                yield os.path.normpath(token.replace("\\", "/")).replace("\\", "/"), f"the build ({rel}, an Exec)"


def _test_readers(root: Path, paths: list[str]) -> Iterable[tuple[str, str]]:
    """Every repository path a compiler test NAMES through `TestRepo.<method>(…)` with string-literal arguments only: the
    file, or the directory (every path below it), the literals spell; with none, the method's own directory
    (`TestRepo.Scripts()` is all of scripts/). A call with a computed argument names no path
    this scan can know; what such a test reads is the watch's to see (module docstring), and its literal prefix alone
    would make whole trees code (measured 2026-11-01: 26 classes' computed calls spelled the whole repository)."""
    for rel in paths:
        if not (rel.startswith("tests/") and rel.endswith(".cs")):
            continue
        try:
            text = (root / rel).read_text(encoding="utf-8")
        except OSError:
            continue
        code = "\n".join(x for x in text.splitlines() if not x.lstrip().startswith("//"))
        for m in TEST_REPO_RE.finditer(code):
            if not m.group(0).endswith(")"):
                continue  # a nested call: a computed argument
            parts = [TEST_REPO_PREFIX[m.group("method")]]
            literals = [STRING_LITERAL_RE.match(a) for a in m.group("args").split(",") if a.strip()]
            if not all(literals) or not (literals or parts[0]):
                continue  # computed, or `TestRepo.At()`: the root itself, whose walks the watch sees
            path = "/".join(p for p in parts + [lit.group("s") for lit in literals] if p)
            if path:
                yield path, rel


def code_readers(root: Path, paths: list[str], keep: Callable[[str], bool] = lambda p: True) -> list[tuple[str, str]]:
    """THE STATIC TWIN OF THE TOOLING LANE'S WATCH (module docstring): (tracked path, its reader) for every tooling path
    the build or a compiler test is seen to read: a csproj `Exec`'s script, the script of every self-test that reads the
    built assemblies (`SELF-TEST-NEEDS: build`, scripts/self_tests.py), and every path or directory a compiler test names
    through `TestRepo`. A directory read is every tracked path below it that `keep` keeps."""
    import self_tests  # noqa: PLC0415 — it imports this module; the build marker's one home is its NEEDS_RE

    found: list[tuple[str, str]] = list(_build_readers(root, paths))
    found += [(t.path, "its own post-build self-test (SELF-TEST-NEEDS: build)") for t in self_tests.discover(root)
              if t.needs_build]
    found += list(_test_readers(root, paths))
    tracked_set, kept = set(paths), [p for p in paths if keep(p)]
    out: list[tuple[str, str]] = []
    for path, reader in found:
        if path.endswith("/") or path not in tracked_set:
            prefix = path if path.endswith("/") else path + "/"
            out += [(p, reader) for p in kept if p.startswith(prefix) or not prefix.strip("/")]
        else:
            out.append((path, reader))
    return sorted(set(out))


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
                if self.c.is_watched(path):
                    self.hits.add(path)
            timeout = 0

    def close(self) -> None:
        os.close(self.fd)


def watch(root: Path, command: list[str], events: Path | None, say=print, c: Classification = CLASSIFICATION) -> int:
    """Run `command` under a watch of the inert and the tooling-lane paths (`Classification.is_watched`): its exit code
    when non-zero, else 1 when it opened a watched path or the watch overflowed, else 0. The verdict line names what was
    read; `events` (written empty once the watch is installed: the readiness mark a caller waiting on it reads) receives
    every path read, one per line. The command runs with a fresh Python bytecode cache (module docstring)."""
    if not sys.platform.startswith("linux"):
        say(f"{WATCH_VERDICT}NOT RUN — inotify is Linux's; this host is {sys.platform} ===")
        return 2
    dirs = c.watch_dirs(root)
    w = Watch(root, dirs, c)
    say(f"inert watch: {len(dirs)} director(ies) under {root} watched while `{' '.join(command)}` runs")
    if events is not None:
        events.write_text("", encoding="utf-8", newline="\n")
    pycache = tempfile.mkdtemp(prefix="inert-watch-pycache-")
    try:
        child = subprocess.Popen(command, env={**os.environ, "PYTHONPYCACHEPREFIX": pycache})
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
        shutil.rmtree(pycache, ignore_errors=True)
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
        inert = sum(1 for h in hits if c.is_inert(h))
        say(f"{WATCH_VERDICT}RED — {len(hits)} watched path(s) were opened while the tests ran ({inert} inert, "
            f"{len(hits) - inert} in the tooling lane), so a lane that runs no compiler test would skip a test that reads "
            "them: give an inert one a READ entry and a tooling one a CODE_READS entry in scripts/inert_lane.py naming "
            "its reader, or make the reader an audit or a script's self-test (kb/Work PB2878, PB4395) ===")
        return rc or 1
    say(f"{WATCH_VERDICT}GREEN — no inert or tooling-lane path was opened ({len(dirs)} director(ies) watched; command "
        f"exit {rc}) ===")
    return rc


# ── the self-test ────────────────────────────────────────────────────────────────────────────────────────────────

def wiring(root: Path) -> list[str]:
    """⛔ ONE CLASSIFICATION DECIDES THE LANES, AND EVERY LINUX TEST RUN PROVES IT (kb/Work PB2878, PB4326, PB4395): the
    WIRING a refactor of a workflow step or a gate script would silently drop, read from the real tree's scripts and
    workflow (all tooling). A `dotnet test` step that loses its watch stops proving the classification, and a runner that
    grows its own path list stops agreeing with the others (the CI invariant, kb/Work PB1957). Returns the findings."""
    import ci_workflow  # noqa: PLC0415

    def text(rel: str) -> str:
        return (root / rel).read_text(encoding="utf-8").replace("\r\n", "\n")

    bad: list[str] = []

    def need(ok: bool, finding: str) -> None:
        if not ok:
            bad.append(finding)

    jobs = {j.id: j for j in ci_workflow.jobs(text(".github/workflows/build-and-test.yml"))}
    cls = "scripts/inert_lane.py"
    changes = jobs["changes"].code
    need(f"python3 {cls} classify --base" in changes, f"CI's `changes` job no longer decides the lane through {cls}")
    need(not re.search(r"""grep -[A-Za-z]*E[A-Za-z]* "\$IGNORE"|IGNORE='|\^\((DEVLOG|docs|kb)/""", changes),
         f"the `changes` job carries a path list of its own again; the lanes have ONE classification, {cls} (PB2878)")
    steps = [(j.id, m.group(0).strip()) for j in jobs.values() if (j.value("runs-on") or "").startswith("ubuntu")
             for m in re.finditer(r"^.*\bdotnet test tests/.*$", j.code, re.MULTILINE)]
    need(len(steps) >= 4, f"found only {len(steps)} `dotnet test` step(s) in the Linux jobs: the scan is broken")
    bad += [f"a Linux `dotnet test` step runs outside the watch, so a test that starts reading an inert or tooling-lane "
            f"path is seen by no one (PB2878): {jid}: {line}" for jid, line in steps
            if f"{cls} watch -- dotnet test" not in line]
    linux_gate, driver, runner = text("scripts/linux-gate.sh"), text("scripts/gate.py"), text("scripts/self_tests.py")
    need('inert_lane.py" classify' not in linux_gate, "the Linux gate classifies again (it classifies nothing, PB4327)")
    need("scripts/inert_lane.py watch" in linux_gate, "the Linux gate's test legs no longer run under the watch")
    for want in ("import inert_lane", "inert_lane.CLASSIFICATION.classify(", "return self._inert(out, run, lane)",
                 "return self._tooling(out, run, lane)", 'TOOLING_ONLY = ("SELF-TESTS",)',
                 "if tooling or name not in TOOLING_ONLY", "self.host.audits(_no_slot, lines.append, tooling)",
                 'self._beside(out, "audits", lambda: self._audits(out, lane.tooling))',
                 'if out.tooling:\n            self._beside(out, "post_build_self_tests"'):
        need(want in driver, f"scripts/gate.py no longer says `{want}`")
    for lane in ("_inert", "_tooling"):
        at = driver.find(f"    def {lane}(")
        body = driver[at:driver.find("\n    def ", at + 1)] if at >= 0 else ""
        need(bool(body) and "take_slot" not in body and "self.host.build(" not in body and "linux" not in body.lower(),
             f"gate.py's {lane} lane takes a slot, builds or reaches Linux: it runs the audits and nothing else")
    audits = jobs["audits"]
    need("python3 scripts/self_tests.py" in audits.code, "CI's `audits` job no longer runs the Linux self-tests")
    need(audits.value("if") == "github.event_name == 'merge_group' || needs.changes.outputs.run_matrix == 'true'",
         f"CI's `audits` job's `if` changed: {audits.value('if')!r} (PB4330: every merge group, no pull request)")
    need("pull_request" not in audits.code, "CI's `audits` job runs on a pull request again (PB4330)")
    need(f"python3 {cls} check" in audits.code, "CI's `audits` job no longer runs THE INERT LANE audit (check)")
    for want in ("tooling: ${{ steps.decide.outputs.tooling }}", "grep -qx 'tooling=false'", "tooling=true"):
        need(want in changes, f"CI's `changes` job no longer says `{want}` (the tooling flag, PB4326)")
    need(jobs["windows-self-tests"].value("if")
         == "needs.changes.outputs.run_matrix == 'true' && needs.changes.outputs.tooling == 'true'",
         "CI's `windows-self-tests` job no longer runs exactly when the change touches tooling")
    # The tooling flag is PROVED against what every self-test read (PB4326 R1-1), before its PASS is recorded; the one
    # exemption is a DECLARED population, held to the trace.
    for want in ("import inert_lane", "classification: inert_lane.Classification = inert_lane.CLASSIFICATION",
                 "self.classification.unclassified_reads(", "if not test.population:",
                 "if test.population and not whole:"):
        need(want in runner, f"scripts/self_tests.py no longer says `{want}`")
    need("if not whole:" not in runner, "scripts/self_tests.py exempts a whole-tree reader without its declaration")
    need(runner.find("self.classification.unclassified_reads(") < runner.find("self.store.record(test.path"),
         "scripts/self_tests.py records a PASS before the tooling flag's proof")
    for rel in ("scripts/hooks/test_dispatch_guard.py", "scripts/orchestrator/test_plan_wave.py"):
        need("# SELF-TEST-POPULATION: tree" in text(rel), f"{rel} no longer declares its population")
    return bad


def _refused(make: Callable[[], object]) -> bool:
    try:
        make()
    except ValueError:
        return True
    return False


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
           "TestRepo.cs, the grammar, the agent-fleet settings, the inventory, docs/CONFORMANCE.md, docs/DESIGN.md) are tooling; "
           "code, tests, the other docs and a name that only starts like a tooling directory or a read file are not",
           [real.is_tooling(p) for p in ("scripts/gate_slot.py", ".github/workflows/build-and-test.yml", "tools/claude-skills",
                                         ".claude/agents/x.md", ".gitattributes", "toolchain.env", "conformance-suite/harness/x.py",
                                         "tests/x/run.sh", "tests/_shared/TestRepo.cs",
                                         "src/WiseOwl.Cobol.Frontend/Grammar/Core/Data.g4", ".agent-fleet.json",
                                         "tests/version-matrix/inventory/GR-4.4-1.json", "docs/CONFORMANCE.md",
                                         "docs/DESIGN.md", "docs/REARCHITECTURE_PLAN.md",
                                         "src/WiseOwl.Cobol.Runtime/RuntimeAbi.pin",
                                         "src/a.cs", "tests/b.cs", "docs/DOC_INDEX.md", "scriptsx/a.md", "toolchain.env.bak",
                                         "tests/_shared/TestRepoX.cs", ".agent-fleet.json.bak", "tests/_shared/Other.cs")],
           [True] * 16 + [False] * 8)
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
    # THE TOOLING LANE (kb/Work PB4395), on a planted classification: scripts/ and .github/ are tooling trees, one script
    # and one workflow are CODE_READS, and a .py file elsewhere is tooling by its suffix but in a compiler tree.
    tl = Classification(inert={"DEVLOG/": "", "kb/": ""}, tooling_under=("scripts/", ".github/", "toolchain.env"),
                        tooling_suffixes=(".py",), tooling_reads={"docs/CONFORMANCE.md": "x"},
                        code_reads={"scripts/gen.ps1": "the build", "toolchain.env": "every test", "scripts/spec/": "y"})
    expect("a change to tooling no compiler test reads is the tooling lane, inert paths beside it included; the lane "
           "runs the self-tests and no build",
           [(lane.name, lane.tooling, lane.builds) for lane in
            (tl.classify(["scripts/steer.ps1"]), tl.classify(["scripts/a.py", ".github/w.yml", "DEVLOG/x.md", "kb/W/P.md"]))],
           [("tooling", True, False)] * 2)
    expect("a CODE_READS path, a directory entry's path, a script file outside the tooling trees, a TOOLING_READS data "
           "file and any code path each make the change the full lane, the tooling flag still set",
           [(tl.classify(p).name, tl.classify(p).tooling) for p in
            (["scripts/gen.ps1"], ["scripts/spec/cite.py", "scripts/a.py"], ["toolchain.env"],
             ["conformance-suite/harness/run.py"], ["docs/CONFORMANCE.md"], ["scripts/a.py", "src/a.cs"])],
           [("full", True)] * 6)
    expect("an exact CODE_READS entry covers that path, never a sibling with a longer name",
           [tl.in_tooling_lane(p) for p in ("scripts/gen.ps1", "scripts/gen.ps1x", "scripts/specx/a.py")],
           [False, True, True])
    expect("the watch reports an inert path and a tooling-lane path, never a CODE_READS one or code",
           [tl.is_watched(p) for p in ("DEVLOG/a.md", "scripts/a.py", "scripts/", "scripts/gen.ps1", "scripts/spec/x.py",
                                       "src/a.cs", "docs/CONFORMANCE.md")],
           [True, True, True, False, False, False, False])
    expect("a lane is one of the three, and the tooling lane always runs the self-tests while the inert one never does",
           [_refused(lambda n=n, t=t: Lane(n, "", t)) for n, t in
            (("inert", True), ("tooling", False), ("code", True), ("full", False))], [True, True, True, False])
    expect("the lanes whose gate runs no compiler test", sorted(TEST_FREE_LANES), ["inert", "tooling"])
    expect("THE WIRING (the real tree): CI's changes job and the gate decide through this classification and carry no "
           "path list; every Linux dotnet test step and the Linux gate's legs run under the watch; the gate's inert and "
           "tooling lanes take no slot and build nothing; CI's audits job runs the self-tests and the check; the tooling "
           "flag reaches windows-self-tests and the gate; self_tests.py proves the flag before it records a PASS",
           wiring(REPO), [])
    expect("the data the self-tests are known to read are TOOLING_READS entries (PB4326 R1-1, R2-1)",
           [e for e in ("tests/_shared/TestRepo.cs", "src/WiseOwl.Cobol.Frontend/Grammar/", ".agent-fleet.json",
                        "tests/version-matrix/", "docs/CONFORMANCE.md", "docs/DESIGN.md", "docs/REARCHITECTURE_PLAN.md",
                        "src/WiseOwl.Cobol.Runtime/RuntimeAbi.pin") if e not in real.tooling_reads], [])
    expect("the real classification: the build's and the compiler tests' tooling is CODE_READS (.gitattributes, "
           "toolchain.env, the generators under scripts/), while a tools/ pin is the tooling lane",
           [real.classify([p]).name for p in (".gitattributes", "toolchain.env", "scripts/gen-reserved-words.ps1",
                                              "tools/claude-skills")], ["full", "full", "full", "tooling"])
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
    # The static twin of the tooling lane's watch (`code_readers`), on a planted tree: a csproj Exec of a script through
    # a property, a compiler test naming a script and a directory through TestRepo (and one through a computed name,
    # which only the watch can see), and a script whose self-test needs a build.
    repo.commit("tooling", {
        "src/P/P.csproj": "<Project><PropertyGroup><Gen>Gen.ps1</Gen></PropertyGroup><Target Name=\"G\">"
                          "<Exec Command=\"pwsh -File &quot;$(MSBuildProjectDirectory)/../../scripts/$(Gen)&quot;\" />"
                          "</Target></Project>\n",
        "scripts/Gen.ps1": "x\n", "scripts/a.py": "x\n", "scripts/spec/b.py": "x\n", "scripts/free.py": "x\n",
        "scripts/built.py": "#!/usr/bin/env python3\n# SELF-TEST-NEEDS: build — reads the built assemblies\n"
                            "import argparse\nargparse.ArgumentParser().add_argument('--self-test')\n",
        "tests/T.cs": "class T { string a = File.ReadAllText(TestRepo.Scripts(\"a.py\")); string d = TestRepo.Scripts(\"spec\");\n"
                      "  // TestRepo.Scripts(\"free.py\") in a comment reads nothing\n"
                      "  string e = TestRepo.Scripts(name); }\n"})
    tc2 = Classification(inert={"DEVLOG/": "", "kb/": ""}, tooling_under=("scripts/",))
    expect("code_readers: the build's Exec (its property resolved), a test's literal TestRepo names (a directory is every "
           "path below it), the build-needing self-test; never a comment or a computed name",
           sorted(p for p, _ in code_readers(root, tracked(root), tc2.in_tooling_tree)),
           ["scripts/Gen.ps1", "scripts/a.py", "scripts/built.py", "scripts/spec/b.py"])
    expect("check: each tooling path the build or a compiler test reads must lie under a CODE_READS entry; with them "
           "it is clean, and a CODE_READS entry outside the tooling trees is refused",
           (len(tc2.check(root)),
            Classification(inert={"DEVLOG/": "", "kb/": ""}, tooling_under=("scripts/",), code_reads={
                "scripts/Gen.ps1": "", "scripts/a.py": "", "scripts/built.py": "", "scripts/spec/": "",
                "src/P/": ""}).check(root)),
           (4, ["the CODE_READS entry 'src/P/' lies in no tooling tree (TOOLING_UNDER), so it changes no lane: remove it"]))
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
        out.clear()
        tw = Classification(inert={"DEVLOG/": ""}, tooling_under=("scripts/",), code_reads={"scripts/a.py": "x"})
        (root / "scripts" / "mod.py").write_text("X = 1\n", encoding="utf-8")
        repo.commit("a module", {"scripts/mod.py": "X = 1\n"})
        imp = py + [f"import sys; sys.path.insert(0, {str(root / 'scripts')!r}); import mod"]
        subprocess.run(imp, check=True)  # leaves scripts/__pycache__/mod.*.pyc: the watched run must open the source anyway
        expect("a command that imports a tooling-lane module is RED naming it, though a bytecode cache of it exists; one "
               "that reads only a CODE_READS script is GREEN (kb/Work PB4395)",
               (watch(root, imp, None, out.append, tw) != 0, any("opened: scripts/mod.py" in s for s in out),
                watch(root, py + [f"open({str(root / 'scripts' / 'a.py')!r}).read()"], None, out.append, tw)),
               (True, True, 0))
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
            lane = Lane("full", f"the change could not be listed ({e}): the full lane", True)
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
