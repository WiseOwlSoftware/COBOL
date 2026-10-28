## PB3820: the merge-group `supersede` job's checkout can no longer fail the verdict

Re-probe on this build: `.github/workflows/build-and-test.yml` still had `continue-on-error: true` on the `supersede` job's
script step only. The job is a `needs` of `ci-gate`, whose verdict whitelists `success` and `skipped`, so a failed
`actions/checkout` (a runner or network error, a sparse-checkout error) failed the job and the merge group's verdict, against
the rule that housekeeping never decides a verdict (the workflow comment, `cancel_superseded.py`, DESIGN-test-build-ci.md
3.17.2 step 4).

The fix takes the acceptance option the design states: the checkout carries `id: checkout` and `continue-on-error: true`;
the cancelling step runs only when `steps.checkout.outcome == 'success'`; a third step, run when the checkout did not
succeed, says so as a warning annotation, so a sweep that did not run is never silent. The job stays a `needs` of `ci-gate`
because the ci-gate drift check wires every job. A job-level failure (the runner never starts) is outside step control and
still fails the verdict; the note records it.

The drift test `TheMergeGroupRun_CancelsTheRunsTheQueueSuperseded_AndNothingElse` now reads every step of the job
(`StepsThatCanFailTheJob`, a step starting at a six-space `- ` with its keys at eight) and names any step lacking
`continue-on-error: true`; it also pins the checkout's id and both `if:` guards. The reader has its own test
(`TheSupersedeStepReader_NamesEveryStepThatCouldFailTheJob`: a step with the key, one without, one whose `with:` value
holds the text, `false`, an empty body). Mutation probe: removing the checkout's `continue-on-error` turns the test red
naming `checkout`. DESIGN-test-build-ci.md 3.17.2 step 4 and 3.17.8, and the `cancel_superseded.py` docstring, now say every
step. No inventory row is claimed or closed.
