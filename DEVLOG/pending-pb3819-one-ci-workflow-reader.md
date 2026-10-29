## One reader splits a CI workflow into jobs and steps: eight private splitters collapsed into `CiWorkflow` (PB3819)

PB3819 named six drift tests that each split `.github/workflows/build-and-test.yml` into jobs their own way. The re-probe
(`git grep -nE "Jobs\(|SplitJobs" -- tests` on the base) found eight readers, not six: the note's five classes,
`SparseCheckoutAudit.Jobs` and the inline regex of `LandingPipelineDriftTests`, plus `ToolchainManifestDriftTests.Jobs` and
`Steps` (a copy of the SDK test's reader with a step reader of its own) and two `Regex.Split` readers in
`LinuxGateDriftTests`. They disagreed on the `jobs:` anchor (one ran over the whole file, so `SparseCheckoutAudit.Jobs`
returned the `on:` triggers push, pull_request and merge_group as jobs, PB3810 item 2), on the characters of a job id, on
which job a banner comment belongs to (the job above, in six of them), on whether a comment inside a step is kept, and on
what a step is (a line at exactly six spaces, or the indentation of the first item). A workflow edit one reader tolerated
another misread, so a drift test could pass vacuously or go red on a legal change.

The fix is the one reader the note asks for, `tests/_shared/CiWorkflow.cs` (`CiWorkflow`, `CiJob`, `CiStep`). It reads
the jobs under the top-level `jobs:` key, each job's `steps:` list and each step's own keys (`Value("uses")`,
`Value("continue-on-error")`; a key nested under `with:` is not the step's), and attributes a comment or blank line to what
follows it, so a banner naming `--list-tests` is never part of the job above. A shape it cannot read (no `jobs:`, an
orphan line under `jobs:`, a flow-style `steps:`) raises `InvalidDataException`: a scan that looked at nothing must not
report clean. All eight sites now ask it and no private splitter remains; the two helpers that stayed
(`SparseCheckoutAudit.Jobs`, `LinuxGateDriftTests.LinuxJobs`) add a file tag or a filter and read nothing themselves.
`StepsThatCanFailTheJob` takes a `CiJob` and reads `continue-on-error` as the step's own key instead of a six/eight-space
regex; the self-test fixtures name their `steps:` key now.

`CiWorkflowDriftTests` holds the reader on a fixture workflow (a trigger block, a comment between jobs, a multi-line `run:`
block with a comment and a blank line inside, a step list at the key's own indentation, a nested `continue-on-error`, CRLF,
and the four refusals) and sweeps every other C# file under `tests/` for a job-key regex or a hand-found `jobs:` anchor,
with its own planted arms. DESIGN-test-build-ci.md's `tests/_shared` table carries the reader; PB3810 item 2 is marked
resolved; PB3787's and PB3308's site paragraphs follow the move.

Self-review: the old readers' behaviours that differed were each a bug or an accident, so the unified semantics changed
three results on purpose and none showed in the existing arms: comment-only lines are now dropped from the text the
sparse-checkout audit and the Linux-job scans match (a commented-out `uses:` could be read as a local action before),
a job's `runs-on` is read as the job's key rather than any line of the job, and `RunCommands` of the self-test test reads
a step's `run:` value rather than any trimmed line starting with `run: `. No diagnostic code is used; no inventory row is
claimed or closed.

Review round 1 (R1-1): `CiArtifactDriftTests` kept a ninth reader, a step splitter that took the indentation of the first
`steps:` list for every job, plus a `Value` that read a key at any depth. With a same-indent step list in a later job the
upload step was never read and the audit passed vacuously, and a `continue-on-error` under `with:` satisfied the upload
action audit. It now asks `CiWorkflow` (jobs, `CiStep.Value`, and a new `CiStep.Input` for a `with:` input such as
`overwrite`), with arms for a same-indent list in a second job, a `with:`-nested `continue-on-error` and an `overwrite`
under `env:`. The tree sweep now also refuses a hand-found `steps:` key (`== "steps:"`), so the next fixed-depth step
splitter fails at the gate.

Merging main in: PB3810 had landed first and answered its item 2 by making `DotnetSdkInstallDriftTests.Jobs` internal, the
reader `SparseCheckoutAudit.Jobs` and `ToolchainManifestDriftTests` then called. That is the reader this change deletes,
so the three conflicted files took `CiWorkflow.Jobs` in its place, and main's half of PB3810 was kept whole: the audit's
`Imports`, which follows a function body's lazy import, and its arms (`TheJobReader_SeesTheKeysBeneathJobsAndNotTheTriggers`
holds `SparseCheckoutAudit.Jobs` on the real workflow and on a planted trigger block). The note body's union merge had
kept two `**Sites:**` lines in PB3810; the landed one stays. The reader's comments and the design row now count the ten
classes it replaced.
