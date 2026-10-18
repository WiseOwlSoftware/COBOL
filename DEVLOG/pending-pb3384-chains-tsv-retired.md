## PB3384: the folded tests/nist/chains.tsv is deleted with every sentence that still called it live; PB3383 filed

`tests/nist/chains.tsv` was folded into `corpus.tsv`'s `chain-preds` column at P0 (verdict-equivalent, entry 667) and
never deleted. Nothing read it, yet it still declared itself "the ONE chain source of truth". DESIGN-test-build-ci.md,
the plan's Phase 14 text and the comments of `CorpusManifest.cs`, `NistDifferentialTests.cs` and `corpus.tsv` still
described the pre-fold state. This landing deletes the file and rewrites those sentences to the current shape:
`corpus.tsv` is the one source of the green set and its chains. It also files PB3383, a lead from the PB3334 finisher:
the battery's serial `guard.sh` runs every NIST program in one shared directory without cleaning between programs, the
defect the finisher fixed in `guard-run-group.sh`, and the two scripts carry two copies of the chain run. The owner,
2026-10-18: "You seem to have a habit of failing to remove obsolete information after a change of process."
