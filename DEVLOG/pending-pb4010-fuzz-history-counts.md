## PB4010: file the fuzz history page's two miscounts of WiseOwl compiler bugs

The round-2 review of branch fuzz-summary (PB4006) came back CLEAN with two low leads, filed here as one note so the register
holds them. R2-1: `finding_kind` in `scripts/fuzz_history.py` calls every defect a compiler bug unless its area starts with
`tooling` or `docs`, so harness and process defects filed under `tests/`, `process/`, `orchestrator/`, `scripts/` or `ci/`
inflate the page's count; the register's `process_only` flag already decides it. R2-2: `defects_of` counts only notes whose
source names a night run, so PB3538, a wrong-answer COMPUTE defect from the aborted run fuzz-20261022-160216, is missing from
the page. No code changes in this landing; the note names the fix and its two self-test cases.
