## PB3478: seventeen test classes' private temp-path and delete helpers collapse into FileHarness, and a drift test keeps them there

PB3459 and PB3471 made `tests/_shared/FileHarness.cs` the one place a test registers, seeds and reads a file. They left
its two smallest helpers behind: `Tmp` (a fresh path in the temp directory) and `TryDelete` (delete it, ignoring the
host's refusal). The note counted seventeen Unit classes with a private copy; the re-probe on this build
(`grep "static string Tmp(\|static void TryDelete"` over `tests/`) found exactly those seventeen, plus a private
`Delete(params string[])` in `RunUnitImplicitCloseDriftTests`, and the sibling sweep found the same job written a
second way in eleven further places, so the fix covers the pattern and not the list.

**The copies had drifted.** Five deleted only `IOException`, three also `UnauthorizedAccessException`, and
`SharedExtendOpenDriftTests` skipped a file name that was empty. `FileHarness.TryDelete` swallows both refusals, which
is the superset, so no test lost a guard it needed. The tag prefixes (`pb2748-`, `pb1587-`...) became the tag argument
(`file-<tag>-<guid>.dat`); the four classes whose `Tmp()` took no tag now pass their former prefix. `ReportHeldLineTests`
and `InstanceFileFinalizerTests` used a `.txt` extension; nothing reads it, so they take `.dat` like every other host.

**What changed.** Every private `Tmp` and `TryDelete` is deleted and its callers call `FileHarness.Tmp` and
`FileHarness.TryDelete` (no forwarder): the 17 classes, `RunUnitImplicitCloseDriftTests`'s `Delete(a, b, c)` as
three-line `foreach` calls, the Conformance `PerformFormat3IoConflictAndSortTests.HostPath` and the inline unique `.dat`
paths in `PerformFormat3IoStatementTests`, `RelativeEnginePermanentErrorWitnessTests`, `FrameDecoderDriftTests`,
`RecordLayoutNoticeTests`, `InstanceFileFinalizerTests`, `KeyedStorePerformanceWitnessTests` and `PageLayoutGoldenTests`,
and `CompiledProgramCache.TryDeleteFile` (an exact copy of `FileHarness.TryDelete`).

**The drift test.** `FileHarnessOneHomeDriftTests` (Sweep, tree) fails on a line that builds a unique `.dat`/`.txt`
name from `Guid.NewGuid()` and on any `try` statement whose block is only `File.Delete` calls and whose catches are all
empty, whether it is a private helper's body or sits inline in a `finally`; a directory delete, or a try or catch that
does other work, is a different job and is not matched. A fixture test proves both detectors still trip on the shapes
they exist for (helper, inline, multi-line with two catches).

**Review round 1.** The first draft matched only the helper-method shape, so 85 inline
`finally { try { File.Delete(x); } catch ... { } }` copies in 45 files (three drifted catch sets: bare, IOException,
IOException plus UnauthorizedAccessException) passed the drift test while its summary claimed one home. All 85 now call
`FileHarness.TryDelete`, the detector is syntactic over every `try`, and the tree has no remaining copy. `DESIGN-runtime-library.md` names the one home.

**Not changed.** The `.cob` and temp-directory builders (`CobolNet_*` directories, `cn_*.cob` sources) are a different
job with their own helpers (`CutRunner`, `CobolNetTestBase.TempDir`). `PageLayoutGoldenTests`'s three bare
`File.Delete(path)` calls in `finally` blocks became `FileHarness.TryDelete(path)`, since a refused delete there would
mask the test's own verdict.

No compiler behaviour changed, so there is no golden and no inventory row; the note closes none.
