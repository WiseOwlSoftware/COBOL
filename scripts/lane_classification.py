"""lane_classification.py — THE LANES AND THE ONE CLASSIFICATION OF A PATH (the declaration and the
classifier; kb/Work PB5398, docs/rearchitecture/DESIGN-orchestrator-loop.md section 16.12) —
THE LANES: a change confined to paths NO TEST READS runs the audits and nothing else, and a change
confined to tooling NO COMPILER TEST OR BUILD READS runs the audits and the script self-tests, with no build (kb/Work
PB2878, PB4395; docs/rearchitecture/DESIGN-test-build-ci.md §3.18).

                                           # and every compiler test that runs a script is CODE_READS

⛔ WHY (owner 2026-10-09, on a one-change landing that edited a practices file and two briefs: "Should this require a
full CI run? I would think not"). Measured over the 300 landings of 2026-10-16..23, 137 changed only files the CI
matrix's old doc-only list named, and each paid the local gate (median ~14 min: audits, a slot, a build, the tests) and
the landing's four Linux legs; but that list was not a classification at all: `docs/` and `kb/Work/` were test INPUTS
then (kb/Work PB3603: a kb/Work-only branch went RED on the Linux gate), so CI skipping the matrix for them broke the CI
invariant (kb/Work PB1957) silently; the fix made them non-inputs (kb/Work PB2878, PB4453), never a list. THIS MODULE IS THE ONE CLASSIFICATION, used by every runner: CI's `changes` job,
the local gate (scripts/gate.py) and the Linux gate (scripts/linux-gate.sh).

THE RULE. A path is INERT when it lies under an INERT entry and under no READ entry. A change is in the INERT LANE when
its changed paths (`git diff --name-only --no-renames`, so a rename shows BOTH names and a code file renamed into an
inert directory is not inert) are non-empty and every one is inert. The inert lane runs the audits (the local gate runs
them in every lane, CI's merge group once per landing; they are where a citation, a register note or a DEVLOG entry is checked) and no build
and no test leg: no test reads the change, so no test can turn red on it. Every other change is the FULL lane.

THE TOOLING FLAGS (kb/Work PB4326, step 3 of PB4323; PB4409). A FULL-lane change also says two things. `reads_tooling`:
it touches a path a script self-test reads, which is TOOLING (`is_tooling`): a path under `TOOLING_UNDER` (scripts/,
.github/, tools/, .claude/, and the two root files the script self-tests read through the checkout, `.gitattributes` and
`toolchain.env`), ending in a `TOOLING_SUFFIXES` extension (.py, .ps1, .sh) anywhere, or under a `TOOLING_READS` entry: a
path outside the scripts that a script self-test reads AS DATA, each entry naming its readers (tests/_shared/TestRepo.cs
for scripts/spec/drift_rules.py, the .g4 grammar for scripts/spec/grammar_source.py, .agent-fleet.json for the
orchestrator's self-tests, the rule catalog for the register audits, ...). The script self-tests
(scripts/self_tests.py) read tooling and those entries, so only such a change can turn a self-test red, and CI's
`windows-self-tests` job runs only then. `tooling`: it touches a SCRIPT (`is_script_tree`: the trees and suffixes, never a
TOOLING_READS data file), and only then does the LOCAL gate run the self-tests (its SELF-TESTS audit and the one self-test
that reads the built assemblies): owner 2026-11-01, "I see no reason why a compiler change should need to run script
self-tests", so a compiler change's data file reaches its self-tests first in the merge group (CLAUDE.md "Testing", THE
RULE (d)). `tooling` implies `reads_tooling`; the tooling lane sets both, the inert lane neither, and an empty or
unlistable change both (nothing proves it touches neither). Like the inert lane, the flags are ONE classification used by
every runner.

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
a `.sh` under tests/, docs/…/evidence/) and a `TOOLING_READS` entry (the grammar, a csproj, the rule catalog,
tests/_shared/TestRepo.cs) lie in trees the compiler's tests and build read, so they are never tooling-lane paths.
THE PROOF OF THE TOOLING LANE is the same watch as the inert lane's (below): every Linux test run also watches every
tooling-lane path, so a compiler test that opens one, directly or through a script it runs, is RED naming it, until the
path gets a CODE_READS entry naming its reader (or the reader becomes a script's self-test, kb/Work PB4395 item 1). The
`check` audit is its static twin, on every host: every csproj `Exec` of a script, every script whose self-test needs a
build, every script a compiler test names through `TestRepo.Scripts(…)` (or runs by a computed name under a literal
directory), every pattern a compiler test LISTS a tooling directory by (a CODE_READS entry may be that pattern,
`scripts/guard*.sh`, so a file added later that it matches is code too) and every Python module one of those scripts
IMPORTS, transitively (the import closure: resolved against the importer's directory and the `sys.path` edits the
chain makes, read statically through `ast`, a lazy import included; kb/Work PB4395 review R2-1) lies under a CODE_READS
entry. A LISTING the watch sees is judged by it only when every name the directory can list is watched (an inert
directory, or a tooling-lane one holding no CODE_READS entry): the import system lists scripts/ to find
scripts/standard_streams.py, a CODE_READS module, and only the static twin knows which name a lister wants. The residue,
named: a Windows-only compiler test that reads a tooling path through a computed name (the watch runs on Linux only), and
a tooling DATA file a script reads (model_rules.json, rr.war), which only the watch sees.

THE PROOF OF THE TOOLING FLAG (`Classification.unclassified_reads`, run by scripts/self_tests.py on every self-test it
runs and records; kb/Work PB4326, review round 1 finding R1-1). The PASS store (kb/Work PB2914) records what each
self-test OBSERVABLY read, so "a script self-test reads only tooling and the TOOLING_READS entries" is checked, not
asserted: a self-test that passes but read a tracked path that is neither is RED, naming the path and the self-test, until
the path gets a TOOLING_READS entry naming its reader. A path-prefix list alone was refuted by those stored inputs:
tests/_shared/TestRepo.cs is read by drift_rules.py's self-test, the .g4 files by grammar_source.py's, and a change to only
one of them turned a self-test red in CI's merge group after a green gate.
THE ACCEPTED RESIDUE, named (owner 2026-10-31, kb/Work PB4323: a merge-group red may be one of five first-run classes, and
the CI invariant is weakened knowingly): (a) a DECLARED POPULATION reader: a self-test whose first
lines say `# SELF-TEST-POPULATION: tree — <why>` reads the tracked tree as a population (the plan_wave cases walk
every markdown file), so no set of paths bounds it and the proof
does not apply to it. The declaration is checked against the trace (scripts/self_tests.py): one that ran no git command over
the whole repository (the tracer records a read of directory "") is RED, and a self-test that ran such a command WITHOUT
the declaration is judged like any other, so its specific reads (abi_pin.py's ABI pin) are
TOOLING_READS entries, not a blind spot (review round 2, finding R2-1); (b) the INERT corpora, DEVLOG/ and kb/, which devlog.py and the register's self-tests read as
fixtures: every landing adds to both, the audits that govern an entry (`devlog.py check`, `work.py check`) run in every
lane, and a change to how they are read is a script edit; and, since PB4453, the Markdown under docs/ that four self-tests
read as data (differential_fuzz.py: the CONFORMANCE.md section 5 table; ci_bounds.py: the DESIGN-test-build-ci.md section its
docstring cites; landing_measure.py and fuzz_triage.py: the fleet-optimization evidence directory): a docs-only edit to one of those runs none of
them in the gate, and CI's Linux audits job, which runs every self-test in each merge group, is where it is first seen; (c) a directory LISTING (`entries:`) records names, which
change when a file is added or removed and not when any listed file is edited, so the proof checks the files a self-test
read and its `paths:` and `content:` reads of a directory.

THE PROOF (`watch`, the drift test the classification needs). Every whole-assembly `dotnet test` run on Linux — CI's
Linux test steps and the Linux gate — runs under an inotify watch of every inert path and every tooling-lane path
(directories included, so a test that LISTS a watched directory is caught too, `Classification.is_watched` saying which
listings judge a lane; a submodule's whole checked-out tree), through
this module: a test that opens one, directly or through a script it runs, turns the run RED naming the path. So the day a
test starts reading an inert path, the change that made it read it is red until that path leaves INERT (or gains a READ
entry naming its reader); a tooling-lane one, until it gains a CODE_READS entry. The watched command runs with a fresh
Python bytecode cache (`PYTHONPYCACHEPREFIX`), so a script a test runs is opened as source at least once per run, never
served unseen from a `__pycache__` an earlier run left. A watch that overflows its queue proves nothing and is RED. The watch has no Windows twin (Windows has no open events); the
Linux runs are the whole population on every code change (the Linux gate and CI's merge group).

What is inert, and what is NOT, and how each came to be (the watch over the whole Linux population decides). Inert:
`DEVLOG/`; `kb/`, once the register drift tests (which read every `kb/Work/` note through a C# twin of `work.py`'s
reader) became the audit `scripts/spec/work_inventory.py`, the retired-name sweep (then LandingPipelineDriftTests, now
landing_rules.py --self-test) set the vault aside as its siblings do, and EcRaiseSiteRegistryDriftTests stopped reading a
note; and, since kb/Work PB4453 (owner 2026-11-02: "There is no compelling reason a compiler change needs to read an
markdown note"), `docs/` and EVERY `*.md` (`MARKDOWN_ENTRY`, a pattern over a file name at any depth): the thirty-odd
Unit and Conformance classes that read CONFORMANCE.md, the rule catalog's prose, VERSION_CHANGE_REFERENCE.md, the user manual,
the README and the design docs became the DOC AUDITS gate audit (scripts/doc_audits.py: what a document says is checked
there, against the compiler's facts in tests/doc-facts/compiler-facts.json, never by a test), two register audits
(audit_annex_a1.py, audit_derivations.py) and ARCH SUBSYSTEMS; a datum a test needs that a document also states is an
extract the document is held to (tests/version-matrix/conformance-register.json, tests/doc-facts/). `check` holds every
compiler test to opening no Markdown file (`markdown_readers`), and the watch proves it. NOT inert, as READ entries: the
ISO text (specs/ISO_COBOL_2023.md, the oracle ~45 classes derive expected tables from: CLAUDE.md rule 1) and the data files a test
reads under docs/ (the rule catalog, the architecture edges, the census records). NOT inert, by being code: the
`tools/claude-skills` pin, and everything under `src/`, `tests/`, `conformance-suite/` that is not Markdown. The
conflict-marker sweep, which read every tracked file, became an audit (scripts/conflict_markers.py) so that it no
longer made every path a test input.

THE MODULES (section 16.12): this one declares the lanes and classifies a path and a change; `source_reads.py` reads statically what a build file, a compiler test's source or a script reads; `inert_watch.py` is the inotify watch of a whole-population test run; `inert_lane.py` is the CLI (`classify`, `watch`, `check`) and the INERT LANE audit.
"""
from __future__ import annotations

import os
import re
import subprocess
from collections.abc import Iterable
from dataclasses import dataclass, field
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]


def listing_glob(pattern: str, recursive: bool) -> re.Pattern[str]:
    """A .NET search pattern (`*`, `?`; case-insensitive, as on Windows, so the scan never reads less than a test can)
    as a regex over the path below the listed directory."""
    body = "".join("[^/]*" if ch == "*" else "[^/]" if ch == "?" else re.escape(ch) for ch in pattern)
    return re.compile(("(?:[^/]+/)*" if recursive else "") + body, re.IGNORECASE)


def under(path: str, entry: str) -> bool:
    """`path` lies under `entry`: a directory prefix ending in `/`, one exact path (and anything below it), or a
    LISTING PATTERN (`scripts/guard*.sh`: `*` and `?` within one directory, case-insensitive as a .NET listing matches),
    which covers every path it matches, a file added later included, and the pattern itself (`code_readers`). An entry
    that begins `**/` is a pattern over a file NAME at any depth (`**/*.md` is every Markdown file in the repository)."""
    if entry.startswith("**/"):
        return path == entry or listing_glob(entry[3:], True).fullmatch(path) is not None
    if "*" in entry or "?" in entry:
        return path == entry or listing_glob(entry, False).fullmatch(path) is not None
    return path.startswith(entry) if entry.endswith("/") else path == entry or path.startswith(entry + "/")


#: The INERT entry that makes every Markdown file inert (kb/Work PB4453), and the one `Classification.check` holds the
#: compiler tests to (`markdown_readers`).
MARKDOWN_ENTRY = "**/*.md"


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
    #: The change touches a SCRIPT (module docstring, THE TOOLING FLAGS): the script self-tests are the LOCAL gate's to
    #: run. Never set on the inert lane, which runs the audits and nothing else; always set on the tooling lane.
    tooling: bool
    #: The change touches anything a script self-test reads (a script, or a TOOLING_READS data file): CI's
    #: `windows-self-tests` job runs then. Implied by `tooling`; never set on the inert lane.
    reads_tooling: bool

    def __post_init__(self) -> None:
        if (self.name not in LANES or (self.name == "inert" and self.reads_tooling) or (self.name == "tooling" and not self.tooling)
                or (self.tooling and not self.reads_tooling)):
            raise ValueError(f"no such lane: {self.name!r} with tooling={self.tooling}, reads_tooling={self.reads_tooling}")

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
        return any(under(path, e) for e in self.tooling_under) or path.endswith(self.tooling_suffixes)

    def in_tooling_tree(self, path: str) -> bool:
        """Under a TOOLING_UNDER entry: the trees that hold nothing but tooling."""
        return any(under(path, e) for e in self.tooling_under)

    def in_tooling_lane(self, path: str) -> bool:
        """Whether a change to `path` can stay in the tooling lane: it lies in a tooling tree and no compiler test or the
        build reads it (under no CODE_READS entry). A script file elsewhere (conformance-suite/harness/, tests/, src/,
        docs/) and a TOOLING_READS entry lie in trees the compiler's tests and build read, so they never are (module
        docstring)."""
        return self.in_tooling_tree(path) and not any(under(path, e) for e in self.code_reads)

    def is_population_input(self, path: str) -> bool:
        """Whether a compiler test or the build MAY read `path`: it is neither inert nor a tooling-lane path. THE ONE
        DEFINITION of the code lane's paths (`classify`) and of the inputs of the Unit, Conformance and Characterization
        populations (scripts/gate.py `Host.population_key`, kb/Work PB5205): a gate's population passes stand for another
        tree exactly when every such path, and the toolchain, are the same."""
        return not self.is_inert(path) and not self.in_tooling_lane(path)

    def is_watched(self, path: str) -> bool:
        """What the watch reports when a compiler test opens it: an inert path or a tooling-lane path. A DIRECTORY
        (`path` ends in `/`: the watch saw it LISTED) is a read of its entries' names, which judges a lane when every
        name it can list is a watched path's: an inert directory, or a tooling-lane directory that holds no CODE_READS
        entry. One that holds one (scripts/: the import system lists it to resolve scripts/standard_streams.py, and
        CorpusManifestTests to find scripts/guard*.sh) may be listed for that entry's name, so its listing is judged by
        the static twin, which knows which name the lister wants (`code_readers`: the import closure's module file, a
        literal listing's pattern), never by the watch, which cannot (module docstring, THE PROOF)."""
        if path.endswith("/") and not self.is_inert(path):
            return self.in_tooling_lane(path) and not any(under(e, path) for e in self.code_reads)
        return self.is_inert(path) or self.in_tooling_lane(path)

    def is_tooling(self, path: str) -> bool:
        """Whether the repo-relative POSIX `path` is tooling: under a TOOLING_UNDER or TOOLING_READS entry, or of a
        TOOLING_SUFFIXES extension."""
        return self.is_script_tree(path) or any(under(path, e) for e in self.tooling_reads)

    def unclassified_reads(self, files: Iterable[str], trees: Iterable[str]) -> list[str]:
        """THE PROOF OF THE TOOLING FLAG (module docstring): of what one self-test read, the tracked paths a change to
        which the flag would NOT see. `files` are the paths it opened; `trees` are its recorded directory reads, each
        `<kind>:<directory>` (self_test_inputs.Deps). A path is seen when it is tooling or lies under an INERT entry (the
        accepted corpora); a directory listing (`entries:`) and the whole tree (`paths:`/`content:` of "") are not
        judged here (the residue, module docstring)."""
        def seen(p: str) -> bool:
            return self.is_tooling(p) or any(under(p, e) for e in self.inert)

        bad = [p for p in files if not seen(p)]
        for key in trees:
            kind, _, d = key.partition(":")
            if kind in ("paths", "content") and d and not seen(d + "/"):
                bad.append(d + "/")
        return sorted(set(bad))

    def is_inert(self, path: str) -> bool:
        """Whether the repo-relative POSIX `path` (a directory may end in `/`) is inert: under an INERT entry, under no
        READ entry."""
        return any(under(path, e) for e in self.inert) and not any(under(path, e) for e in self.read)

    def classify(self, paths: list[str]) -> Lane:
        """The lane of a change whose changed paths are `paths`."""
        if not paths:
            return Lane("full", "no changed path: an empty diff is not evidence that no test reads the change",
                        tooling=True, reads_tooling=True)
        read = [p for p in paths if not self.is_inert(p)]
        if not read:
            entries = sorted({e for p in paths for e in self.inert if under(p, e)})
            return Lane("inert", f"all {len(paths)} changed path(s) are inert (under {', '.join(entries)}): no test reads "
                        "them", tooling=False, reads_tooling=False)
        code = [p for p in read if self.is_population_input(p)]
        if not code:
            return Lane("tooling", f"{len(read)} of {len(paths)} changed path(s) are tooling no compiler test or build "
                        f"reads, e.g. {read[0]}, and the rest inert: the audits and the script self-tests",
                        tooling=True, reads_tooling=True)
        # ⛔ A COMPILER CHANGE RUNS NO SCRIPT SELF-TESTS LOCALLY (owner 2026-11-01: "I see no reason why a compiler change
        # should need to run script self-tests"). In the full lane `tooling` counts only a changed SCRIPT (a TOOLING_UNDER
        # tree or a TOOLING_SUFFIXES file: the change's own tooling half), never a TOOLING_READS data file a self-test
        # happens to read (the rule catalog, the grammar, TestRepo.cs): compiler fixes change those routinely, and their
        # self-tests then cost the compiler gate 10 to 38 minutes. `reads_tooling` counts both, so CI's merge group still
        # runs the Windows self-tests such a data file reaches: their first run (CLAUDE.md "Testing", THE RULE (d)).
        scripts = [p for p in paths if self.is_script_tree(p)]
        reads = [p for p in paths if self.is_tooling(p)]
        return Lane("full", f"{len(code)} of {len(paths)} changed path(s) may be read by a compiler test or the build, "
                    f"e.g. {code[0]}"
                    + (f"; {len(scripts)} touch(es) a script, e.g. {scripts[0]}" if scripts else "; none touches a script")
                    + (f"; {len(reads)} touch(es) what a script self-test reads, e.g. {reads[0]}" if reads and not scripts
                       else ""),
                    tooling=bool(scripts), reads_tooling=bool(reads))

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

    def order_findings(self) -> list[str]:
        """The categories of this classification whose entries are NOT in key order, one finding each (kb/Work PB5398): every
        category is kept sorted, so two branches that each declare a path add it at different places and plain git merges them."""
        found = []
        for kind, entries in (("INERT", self.inert), ("READ", self.read), ("TOOLING_READS", self.tooling_reads),
                              ("CODE_READS", self.code_reads)):
            keys = list(entries)
            at = next((i for i in range(1, len(keys)) if keys[i] < keys[i - 1]), None)
            if at is not None:
                found.append(f"the {kind} entries are not in key order: {keys[at]!r} sorts before {keys[at - 1]!r}; "
                             "keep each category sorted (`sorted(...)` of its keys) so branches merge")
        return found


#: ⛔ THE CLASSIFICATION. Nothing a test reads is INERT; `watch` proves it on every Linux test run, and an entry that
#: names no tracked path is stale (`check`, THE INERT LANE audit of every gate). A READ entry, when one is needed, lies under
#: an INERT entry and names the tests that read it: the path is then the full lane until they stop.
CLASSIFICATION = Classification(inert={
    MARKDOWN_ENTRY: "every Markdown file: a note, a design, a brief, a skill, a README; read by people, the harness and the "
               "audits (scripts/, run in every lane), never by a compiler test (kb/Work PB4453; the one exception is the "
               "ISO text, a READ entry)",
    "DEVLOG/": "the development log: read by scripts/devlog.py (CI's devlog step and the landing checks: `devlog.py check` "
               "lints every entry; its self-test reads only planted trees since kb/Work PB5227) and by people",
    "docs/": "the documentation tree: read by people and by the doc audits (scripts/, run in every lane); no compiler "
             "test reads a Markdown file (kb/Work PB4453), and the data files a compiler test reads are READ entries",
    "kb/": "the knowledge base and THE work register (kb/Work): read by scripts/spec/work.py and work_inventory.py (the "
           "gate's WORK REGISTER audit, CI's audits job: the register against the inventory), the audits that cite a "
           "note, and by people; no test reads it since kb/Work PB2878 (the register drift tests became "
           "work_inventory.py; the retired-name sweep and the activation-boundary registry stopped reading it)",
}, read={
    "docs/rearchitecture/architecture-edges.json": "ArchitectureEdgesDriftTests",
    "docs/rearchitecture/evidence/arch-census/": "ArchCensusDriftTests (CensusRecord)",
    # Every held edition's rule catalog (inventory-schema.json editions.of `catalog`): a pattern, so an edition that gains
    # a catalog is a READ path the day it lands (kb/Work PB4994's older-edition drift tests first read the 1985 one).
    "docs/rearchitecture/spec-rule-catalog*.json": "CatalogCoverageDriftTests and the traceability-inventory drift tests "
                                                   "(TraceabilityInventory.RowsOf and inventory_view.py --edition read "
                                                   "every held edition's catalog)",
    "specs/ANSI_COBOL_1985.md": "RecordClauseRuleDriftTests (the Since column's 1985 side, kb/Work PB5196)",  # spec_pdf: names-the-file
    "specs/ISO_COBOL_2002.md": "RecordClauseRuleDriftTests (the Since column's 2002 side, kb/Work PB5196)",  # spec_pdf: names-the-file
    "specs/ISO_COBOL_2023.md": "the standard's text, the ORACLE (CLAUDE.md rule 1): ~45 Unit and Conformance classes derive their "  # spec_pdf: names-the-file
                          "expected tables from it",
}, tooling_under=("scripts/", ".github/", "tools/", ".claude/", ".gitattributes", "toolchain.env"),
   tooling_suffixes=(".py", ".ps1", ".sh"),
   # ⛔ TOOLING_READS: what the script self-tests read outside the scripts (module docstring), measured from the PASS store's
   # recorded inputs and held complete by `unclassified_reads` on every run. An entry names its readers; a directory
   # entry is a directory the readers read as data (its files, or its names through git). Narrow by design: a new
   # entry makes every change to its path a tooling change, which runs the self-tests.
   tooling_reads={
       ".agent-fleet.json": "scripts/spec/ledger_plan.py, scripts/orchestrator/test_orchestrate_adopt.ps1 and _ledger.ps1",
       ".gitmodules": "scripts/hooks/tooling_check.py (its PB2605 repro runs every registered hook), "
                     "scripts/hooks/test_forbidden_commands.py",
       "Directory.Build.props": "scripts/abi_pin.py (settling the ABI pin)",
       "LICENSE": "scripts/arch/census.py (read on Linux)",
       "WiseOwl.Cobol.sln": "scripts/arch/census.py, scripts/linux-gate.sh, scripts/orchestrator/test_orchestrate_account.ps1",
       "conformance-suite/adapters/": "scripts/differential_fuzz.py (the GnuCOBOL and WiseOwl adapter files)",
       "conformance-suite/fuzz-history.json": "scripts/test_fuzz_history.py (every record of the tracked history carries the "
                                              "night's date, kb/Work PB5371)",
       "conformance-suite/harness/": "scripts/line_endings.py, live_tools.py, temp_names.py and the harness self-tests (the "
                                     "names of the harness files)",
       "docs/rearchitecture/spec-rule-catalog.json": "scripts/spec/audit_annex_a1.py, audit_code_citations.py",
       "global.json": "scripts/toolchain_audit.py (the SDK pin global.json's version and rollForward must accept)",
       "specs/ISO_COBOL_2023.md": "scripts/spec/cite.py, audit_code_citations.py, audit_doc_citations.py, "  # spec_pdf: names-the-file
                             "scripts/differential_fuzz.py",
       "src/WiseOwl.Cobol.Cli/WiseOwl.Cobol.Cli.csproj": "scripts/guard-compiler.sh, guard-verify.sh, linux-gate.sh",
       "src/WiseOwl.Cobol.Frontend/Grammar/": "scripts/spec/grammar_source.py (the grammar files, whole)",
       "src/WiseOwl.Cobol.Runtime/Control/RuntimeAbi.cs": "scripts/abi_pin.py (settling the ABI pin)",
       "src/WiseOwl.Cobol.Runtime/RuntimeAbi.pin": "scripts/abi_pin.py (settling the ABI pin)",
       "tests/WiseOwl.Cobol.Benchmarks/GeneratedCode/GeneratedProgramBenchmarks.cs": "scripts/arch/perf_baseline.py",
       "tests/WiseOwl.Cobol.Benchmarks/Programs/": "scripts/arch/perf_baseline.py",
       "tests/WiseOwl.Cobol.Tests.Characterization/WiseOwl.Cobol.Tests.Characterization.csproj": "scripts/linux-gate.sh",
       "tests/WiseOwl.Cobol.Tests.Conformance/WiseOwl.Cobol.Tests.Conformance.csproj": "scripts/linux-gate.sh",
       "tests/WiseOwl.Cobol.Tests.Unit/CarrierLandingFormTests.cs": "scripts/spec/audit_annex_a1.py",
       "tests/WiseOwl.Cobol.Tests.Unit/WiseOwl.Cobol.Tests.Unit.csproj": "scripts/linux-gate.sh",
       "tests/_shared/SpecClauseText.cs": "scripts/spec/spec_headings.py",
       "tests/_shared/TestRepo.cs": "scripts/spec/drift_rules.py",
       "tests/external/gnucobol-verdict-baseline.tsv": "scripts/corpus_sweep.py (the differential's committed cases)",
       "tests/nist/corpus.tsv": "scripts/guard-nist-audit.sh, guard-verify.sh, linux-gate.sh, "
                                "scripts/orchestrator/test_orchestrate_account.ps1",
       "tests/version-matrix/": "the inventory and its schema: scripts/spec/audit_*.py, ledger_plan.py, "
                                "scripts/orchestrator/land_lane.py, test_ledger_state.py, the rename audits and "
                                "the eleven test_orchestrate_*.ps1 parts",
   },
   # ⛔ CODE_READS: the tooling the compiler's build or tests read (module docstring, THE TOOLING LANE), held by the
   # watch on every Linux test run and by `check` (the INERT LANE audit) on every host. An entry names its readers: the
   # exact file a compiler test or the build reads, or a directory a compiler test reads below; a fact about the tooling
   # alone lives in that tooling's self-test, never here (kb/Work PB4395 item 1).
   code_reads={
       ".claude/skills/workstream/templates/wf_r2_review.js": "scripts/arch/test_wf_r2_review.mjs, the workflow's dry "
                                                               "run ArchReviewFleetDriftTests runs",
       ".gitattributes": "every compiler test: the checkout's bytes (line endings) of every file it reads",
       ".github/": "CiArtifactDriftTests, ReadyToRunRuntimeDriftTests (every workflow and the results actions)",
       "scripts/arch/": "ArchCensusDriftTests (census.py), ArchReviewFleetDriftTests (the R2 review workflow scripts)",
       "scripts/ci_workflow.py": "imported by scripts/test_population.py",
       "scripts/detached.py": "imported by scripts/orchestrator/coord.py",
       "scripts/filter_population.py": "its self-test reads the built test assemblies",
       "scripts/gen-cobol-words.ps1": "a generator whose output a compiler drift test checks",
       "scripts/gen-constructs.ps1": "a generator whose output a compiler drift test checks",
       "scripts/gen-diagnostics-doc.ps1": "a generator whose output a compiler drift test checks",
       "scripts/gen-grammar-diagrams.ps1": "the build (the Cli project's Exec step) and GrammarDiagramGeneratorDriftTests",
       "scripts/gen-reserved-words.ps1": "a generator whose output a compiler drift test checks",
       "scripts/gen-vault-reference.ps1": "the build (the Cli project's Exec step) and VaultReferenceGeneratorDriftTests",
       "scripts/git_batch.py": "imported by scripts/spec/inventory_schema.py and inventory_store.py (the inventory view "
                               "conformance-suite/harness/generate.py reads, SuiteManifest; a partial clone's batched "
                               "blob reads, kb/Work PB4992)",
       "scripts/guard*.sh": "CorpusManifestTests, which LISTS scripts/ for every guard*.sh: a pattern, so a guard script "
                            "added later is the code lane too",
       "scripts/live_tools.py": "imported by scripts/standard_streams.py, scratch_repo.py and test_population.py",
       "scripts/orchestrator/account.py": "imported by scripts/orchestrator/budget.py",
       "scripts/orchestrator/budget.py": "imported by scripts/arch/r2_cost.py (ArchReviewFleetDriftTests)",
       "scripts/orchestrator/coord.py": "imported by scripts/arch/r2_inputs.py, r2_cost.py and scripts/spec/work.py "
                                        "(ArchReviewFleetDriftTests)",
       # Data the scripts above read, seen only by the watch (measured on Linux 2026-11-01, kb/Work PB4395 review R2-1).
       "scripts/orchestrator/model_rules.json": "scripts/orchestrator/coord.py (RULES_PATH) for scripts/arch/r2_cost.py's "
                                                "self-test, which ArchReviewFleetDriftTests runs",
       "scripts/rendered_views.py": "imported by scripts/spec/work.py (scripts/arch/r2_inputs.py, ArchReviewFleetDriftTests)",
       "scripts/scratch_repo.py": "imported by conformance-suite/harness/generate.py (SuiteManifest) and "
                                  "test_population.py",
       "scripts/selftest_layout.py": "imported by scripts/scratch_repo.py (its audit reads a file under a selftests/ tree as "
                                     "self-test code by the path rule, kb/Work PB5428)",
       "scripts/semgrep/verify.py": "imported by scripts/arch/r2_inputs.py (its semgrep step reads errors[] through verify's "
                                    "helpers, kb/Work PB4462 review R1-1; ArchReviewFleetDriftTests runs the self-test)",
       "scripts/sharedfile.py": "imported by scripts/orchestrator/coord.py",
       "scripts/spec/": "the spec extractors and audits a compiler test runs or reads (SpecTraceabilityInventoryDriftTests, "
                      "AnnexA2UndefinedListDriftTests, DerivedVerdictDriftTests, "
                      "TrackedTreeScopeDriftTests, CatalogCoverageDriftTests, TestRepoDriftTests)",
       # The modules the scripts above IMPORT (`code_readers`' import closure, kb/Work PB4395 review R2-1).
       "scripts/standard_streams.py": "imported at startup by every script a compiler test runs: conformance-suite/"
                                      "harness/run.py and generate.py, scripts/spec/drift_rules.py, audit_annex_a1.py, "
                                      "extract_annex_a2.py, audit_derivations.py, derive_verdict_batch.py, "
                                      "scripts/arch/census.py and r2_*.py, test_population.py",
       "scripts/telemetry/otlp_sink.py": "imported by scripts/telemetry/usage_report.py",
       "scripts/telemetry/usage_report.py": "imported by scripts/orchestrator/budget.py",
       "scripts/test_population.py": "every gate leg: it plans the population the compiler tests run",
       "toolchain.env": "every compiler test and the build: the SDK they run on (the toolchain manifest, kb/Work PB4007)",
       "tools/rr/rr.war": "scripts/gen-grammar-diagrams.ps1 (the build's Exec and GrammarDiagramGeneratorDriftTests)",
   })


def changed(root: Path, base: str, head: str = "HEAD") -> list[str]:
    """The paths `head` (a commit or a tree) changes against `base`, both names of a rename. Raises CalledProcessError."""
    out = subprocess.run(["git", "-C", str(root), "diff", "--name-only", "--no-renames", "-z", base, head],
                         capture_output=True, check=True).stdout
    return [p for p in out.decode("utf-8").split("\0") if p]


def tracked(root: Path) -> list[str]:
    out = subprocess.run(["git", "-C", str(root), "ls-files", "-z"], capture_output=True, check=True).stdout
    return [p for p in out.decode("utf-8").split("\0") if p]
