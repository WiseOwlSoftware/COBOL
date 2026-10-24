## PB3629: file the open-files-at-host-entry-return lead from the PB3194 review

The PB3194 branch review found that `CommitRollbackRefusalWitnessTests.Run` returns with the run unit's files still open (about 20 handles in one run), now visible through the new SCRATCH-LEFT report. No existing kb/Work note covered it, so this files PB3629 with the site, the repro and the open question: whether §14.6.11 GR2 (implicit CLOSE at normal run-unit termination) makes the test or the runtime responsible for the close on a host-entry return. No code changes.
