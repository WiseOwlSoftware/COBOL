# PB5230 review round 2 (E:/COBOL-coord/scratch/review-w20261997-a.jsonl): three findings, fixed; the audit is the early warning

The reviewer made a Unit test read outside Unit's key three new ways while the BUILD TARGETS audit stayed GREEN. Each is
fixed in `scripts/buildgraph.py` and `scripts/build_targets.json`.

**R2-1 (high): a literal `..` made the audit skip a read silently.** Planted: `TestRepo.Src("..", "tests", "differential",
…)`. The audit read `src/../tests/differential/…`, which names no tracked path, and DROPPED it as a scratch or build name.
Fix: `_resolved` resolves every `.` and `..` segment before the read is judged, so that read is judged at `tests/differential/…`
(outside Unit's inputs: RED). A read that climbs above the repository (`TestRepo.At("..", "x")`) is a finding by name in
any target, since no declaration can cover it. And the pattern behind it: a read that names no tracked path was dropped by
default. It is now dropped only when it is provably no input, (a) a build output, recognised by a `bin` or `obj` path
segment, or (b) a path git ignores, recognised by `git check-ignore --no-index` (the repository's ignore rules alone,
whether the path exists or not: `.cache/`, `TestResults/`, `docs/DIAGNOSTICS.md`). Anything else is a finding in a narrow
target: a typo, a stale path or a file not yet made. Today that reports 14 reads, each judged with its reason:
Conformance's CompiledProgramCacheDriftTests names `src/prog.cob` (11 lines) and `src/PB985CPY.cpy` (2) INSIDE its own scratch
directory, and Unit's SharedReadCoherenceDriftTests asserts `src/WiseOwl.Cobol.Runtime/IO/IndexedConnector.WholeStore.cs`
stays absent (PB3334 deleted it). `repo-drift` (broad) has 40 such reads (planted fixture paths and `#symbol` anchors),
counted by no rule because it is refused nothing.

**R2-2 (medium): a path formed around TestRepo was invisible.** Planted: `Path.Combine(AppContext.BaseDirectory,
"../../../../differential/…")` and `Path.Combine(Environment.GetEnvironmentVariable("WISEOWL_COBOL_REPO")!, "kb", "x")`.
Decision (the controller's): in a narrowly declared target a test source reaches the file system only through TestRepo and
ScratchDirectory. The existing one-home rule is RepoDrift's `TestRepoDriftTests` (the repository has ONE locator; its
`Anchors` list names the base directories a hand-rolled locator starts from), with `ScratchDirectoryDriftTests` for
scratch space. It is EXTENDED, not duplicated: `buildgraph.root_anchors` reads the anchors from `TestRepoDriftTests.Anchors`
itself (a rule with no list is a failure naming it), so an anchor added there is refused by the audit too. Beyond those
anchors the audit refuses `Environment.CurrentDirectory`, an assembly's `Location`/`CodeBase`, an environment variable read
on a line that forms a path, a string literal handed to a System.IO call as its path (`File.*`, `Directory.*`,
`Path.GetFullPath`, `new FileInfo/DirectoryInfo/FileStream/StreamReader/StreamWriter`, bare or `System.IO.`-qualified), and a
rooted literal handed to `Path.Combine`/`Join` (a relative fragment there is completed by some other base, a TestRepo
helper's). Comments, literals' text and raw string literals are not code. A statement is let through only by a hand
judgment, `<file>|<its line, trimmed>`, with a true reason.

What it refused today, before any judgment, and how each was settled (all judged; none was a read of the repository):

| target | refused | own | shared | settled |
|---|---|---|---|---|
| unit | 21 | 15 | 6 | 6 Roslyn compilation references to a loaded assembly (`MetadataReference.CreateFromFile(typeof(…).Assembly.Location)`); 2 BinderMemberReaders statements on the host's own build output (the compiler dll beside it, and a WRITE of the measured json); RepositoryLocatorTests copying the loaded runtime dll into its scratch directory; 2 `/proc/self/*` boundary witnesses (FileBoundary, SortWorkBound); 4 statements comparing the working directory as EXPECTED text (PhysicalFileIdentity ×2, ProcessObservation ×2) |
| conformance | 8 | 2 | 6 | NonCobolElement's compilation reference; CompiledProgramCache's working directory as key TEXT |
| characterization | 6 | 0 | 6 | the shared six |
| repo-drift | 12 | 6 | 6 | broad: counted, not refused (it declares every population input) |

The shared six, judged once under `build.judged`: TestRepo's three (`Configuration` and `TargetFramework`, the names of the
host's output directory, and `Locate`, the one locator's walk), HostLoad's `RunDirectory` default (the working directory as
text) and its two `/proc` counters (`/proc/loadavg`, `/proc/stat`).

⛔ **This static audit is the EARLY WARNING; the SOUND net is the dynamic proof** (kb/Work PB5235: a CI shard fails when a
test opens a tracked path outside its target's declared inputs). The audit reads source text, so it sees a path a source
SPELLS. Only the dynamic proof sees a path computed in a CHILD PROCESS (a script or a compiled program a test runs: every
`PythonInstrument.Run` caller's script reads what it reads), in NATIVE code, through REFLECTION, or formed at run time (read
from a file, built in a loop, passed through a helper the audit does not follow), and what `repo-drift`'s bare roots read.
The audit's docstring and DESIGN-test-build-ci.md say so.

**R2-3 (low): the PythonInstrument judgment's reason was false.** It said "the one caller is SuiteManifest"; there are nine
other `PythonInstrument.Run` call sites in Conformance and RepoDrift. The judgment covers one statement, the working
directory every caller's script runs in (the repository root), never a read; its reason now says that, and that each
caller's script is that caller's own read, judged at its call: SuiteManifest's `generate.py` at the conformance-suite
hand-off site, ConformanceSuiteRunnerDriftTests' `harness/run.py` (declared by conformance), RepoDrift's (every population
input); what a script reads is the dynamic proof's.

**Proof.** `python scripts/buildgraph.py check`: GREEN. `buildgraph.py --self-test`: PASS, 31 arms, of which three are
new: `..` and `.` resolved and a climb kept; a planted narrow target refusing, with each line, the resolved read, the climb,
the untracked read, and nine bypass statements (the one-home anchors, the working directory, literal and rooted System.IO
paths, an environment variable forming a path, `System.IO.`-qualified forms), while a build output, an ignored path, a
judged statement, a comment, a raw literal and a member named `File` pass, and a broad target is refused only the climb; and
`root_anchors` reading a planted rule's list and refusing a rule without one. The reviewer's files planted in Unit
(round 1's seven reads in PlantedReviewReads.cs, round 2's PlantedDotDot, PlantedNew, PlantedOld and PlantedRelative): RED,
22 findings, every planted line named with its file (PlantedRelative.cs:5, the `System.IO.Path.Combine` + environment
variable form, was missed by the first cut of the rule, which required an unqualified `Path`; the qualified form is now
covered and armed). Removed after.
