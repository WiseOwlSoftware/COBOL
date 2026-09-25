# Golden lane 1 (cloud) — LANDING

Branch `claude/golden-lane-1`, cut from origin/main 138a872. Lands via the owner's local orchestrator (push-main).
Rows in scope: 393 CONFORMS-with-empty-test-ref + 4 DOCUMENTED-NON-SUPPORT owing a witness = 397, split into 39 input
files (`scratch/in/`, built by `scratch/build_inputs.py` = the template builder with Linux paths and a hard MAX 12).

Pipeline: writer agent per input file (`scratch/WRITER.md`) derives expected output from the spec, then runs the built
compiler; one refuter per file (`scratch/REFUTER.md`) re-derives. Only refuter-upheld, passing goldens land
(`scratch/integrate.py`). `scratch/` is the raw record (drafts, held drafts, reports) — NOT a test location.

Baseline gate on 138a872 (this VM): Conformance filter Corpus|Negative|Intrinsic|VersionMatrix Passed 5446/5446;
Unit filter SpecTraceabilityInventory|DefectiveRowCoverage|Manifest|AnnexA1Register Passed 25/25.

Owner budget notice (2026-09-25): waves 1 and 2 only; no wave 3.

## Status
- Snapshot pushed: wave 1 (misc-p1..p6) writers done, refuters partially done; wave 2 (misc-p7..p12) writers running.
