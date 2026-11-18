## 2026-11-18: PR #1864 (pb4950-sweep) bounced on a backslash ProjectReference path, class (b), first run of Linux

Pull request #1864 (branch pb4950-sweep at aef7dad2a2) failed merge-group run 44268547019, job "Linux shard 1/5", and was dequeued
with the reason failed_checks. Both tests of `tests/WiseOwl.Cobol.Tests.Unit/EditionCiteOneDeciderDriftTests.cs` failed with
`KeyNotFoundException: The given key '..\WiseOwl.Cobol.Runtime\WiseOwl.Cobol.Runtime' was not present in the dictionary`
(`BuildSourceCompilations`, line 97). Reviewers had read the branch clean twice, and the Windows gate was GREEN.

**This is a class (b) red: Linux, first run.** The merge group was the first run of that test on Linux. Under THE RULE, every case
the merge group runs on Windows has already run green on this host, but Linux runs only in the merge group, so a Linux-only fault
is first seen there. It is a finding against this note's record of that class.

**Cause.** The test read each `ProjectReference Include` of a csproj and named the referenced project with
`Path.GetFileNameWithoutExtension`. The csproj files write `..\WiseOwl.Cobol.Runtime\WiseOwl.Cobol.Runtime.csproj`, with backslashes.
On Windows `\` is a directory separator, so the call answered `WiseOwl.Cobol.Runtime`. On Linux `\` is an ordinary character, so the
whole string was one file name and the dictionary keyed by project name missed it. MSBuild itself accepts either separator on every
OS, so the build was fine; only the test's reading of the file was not.

**Why no Windows run could see it.** The value is correct on Windows for every input the repository holds. No Windows test, gate
or review run executes the Linux path semantics, and the test reads only the committed csproj files. The only checks that could
catch it were a Linux run or a test that feeds the helper a backslash path and asserts what it resolves to.

**Fix (pb4950-sweep, round 5).** `TestRepo.MsBuildPath(fileDirectory, written)` is now the one resolution of a path written in an
MSBuild file. It normalizes both separators and returns the full path resolved against the writing file's directory, as MSBuild
does. The drift test keys projects by that full path. `TestRepoMsBuildPathTests` feeds backslash, slash and mixed Includes through
it on the current OS.

The repository's own `TestRepo.MsBuildPath` was then compiled and run on Linux under WSL (Ubuntu, dotnet 10.0.404; a scratch
console linking `tests/_shared/TestRepo.cs`):
- `..\WiseOwl.Cobol.Runtime\WiseOwl.Cobol.Runtime.csproj` resolved to `/repo/src/WiseOwl.Cobol.Runtime/WiseOwl.Cobol.Runtime.csproj`.
- The old `Path.GetFileNameWithoutExtension` call answered `..\WiseOwl.Cobol.Runtime\WiseOwl.Cobol.Runtime`, the exact key of the
  failure.

**Sweep.** The other tests that read MSBuild or solution paths already normalize the separators before they use them:
- `ArchitectureEdgesDriftTests` and `ArchCensusDriftTests` read solution project paths with `Replace('\\', '/')`.
- `AntlrGenerationDriftTests` compares a normalized Include.
- `GateLegDriftTests` asserts literal text.

None resolves a csproj Include the way this test did.
