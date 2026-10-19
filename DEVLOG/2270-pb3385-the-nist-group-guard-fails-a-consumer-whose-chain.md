## Entry 2270 — 2026-10-19 15:51 PDT — PB3385: the NIST group guard fails a consumer whose chain predecessor did not run to a normal end

`scripts/guard-run-group.sh` runs a consumer's declared predecessors (corpus.tsv's `chain-preds`) ungraded in the
consumer's clean directory, then grades the consumer. `run_predecessor` dropped the predecessor's exit status and
treated a predecessor with no compiled program as a stderr warning, so the consumer was graded over a partial file or
none. `NistRunner` fails the consumer in both cases (`[chain p] run exit non-zero`, `[chain p] compile …`). In a full
group run the predecessor is also a graded member and shows red there; a GUARD_TESTS subset that names only the consumer
has no other place to see it fail.

**Re-probe.** A witness with the consumer alone in its group (`WT002A <- WT001A`) and the predecessor's `dotnet` shim
exiting 1, 124 or 139, or its program absent, graded the consumer `MATCH` in all five cases.

**Fix.** Two functions in `scripts/guard-verdict.sh`, the file both guards source, apply the existing evidence rules to
the predecessor and name it. `guard_predecessor_verdict` scores its run: a timeout, a signal or an exit with no
diagnostic is `RUN NO-VERDICT`, an exit with a diagnostic is `DIFF — REGRESSION!`. `guard_predecessor_compile_verdict`
scores its absent program through `guard_compile_verdict`. Both use verdict words the audit and `guard-verify.sh`
already classify. `run_predecessor` returns the verdict, keeps the predecessor's stderr outside the run directory and
preserves it as forensics, and the group loop prints it as the consumer's verdict instead of grading. A real
chain group (IX109A IX115A, ST101A ST102A ST103A) still grades clean through `guard-fast.sh`; IX115A alone now reports
`COMPILE NO-VERDICT (declared predecessor IX109A: …)` rather than running without its input.

**Goldens.** Eight `guard-verify.sh --witnesses` cases: a control, the predecessor's non-zero exit with and without a
diagnostic, timeout, signal, absent program with and without compile evidence, and the preserved stderr. Shown RED before
the fix. The serial `scripts/guard.sh` runs no ungraded predecessor; PB3383, which gives it the chain run, calls the
shared functions (noted on that register item).

**Self-review.** One missing-program case was found beside the reported one and fixed in the same function. No
TERMINATES row is a predecessor, so a non-zero exit is never a legitimate predecessor outcome in the corpus.

