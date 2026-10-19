## PB3384: the folded tests/nist/chains.tsv is deleted with every sentence that still called it live; PB3383 and PB3385 filed

`tests/nist/chains.tsv` was folded into `corpus.tsv`'s `chain-preds` column at P0 (verdict-equivalent, entry 667) and
never deleted. Nothing read it, yet it still declared itself "the ONE chain source of truth". DESIGN-test-build-ci.md,
the plan's Phase 14 text and the comments of `CorpusManifest.cs`, `NistDifferentialTests.cs` and `corpus.tsv` still
described the pre-fold state. This landing deletes the file and rewrites those sentences to the current shape:
`corpus.tsv` is the source of the green set and its chains for the tests and the group guard, while the serial
guard's hand-written `NIST_TESTS` remains (kb/Work PB925). It also files PB3383, a lead from the PB3334 finisher:
the battery's serial `guard.sh` runs every NIST program in one shared directory without cleaning between programs, the
defect the finisher fixed in `guard-run-group.sh`, and the two scripts carry two copies of the chain run; the Mythos reviewer's round 3 adds that the battery's equivalence proof goes red once PB3334 lands and that `guard-fast.sh` still calls the serial guard the authority. It files PB3385: the group guard's `run_predecessor` drops a predecessor's exit status. The owner,
2026-10-18: "You seem to have a habit of failing to remove obsolete information after a change of process."
