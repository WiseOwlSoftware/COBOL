## Entry 3981 — 2026-11-23 06:15 PST — The 1985 row obligations no longer count a wiseowl/ test as a test Stage 2 can clear

kb/Work PB6104. `obligations.classify` put a row whose only accepted-at-85 tests live in the `wiseowl/` tree under `accepted-uncleared` ("Stage 2 clears it"), though `generate.Case.in_suite` excludes that tree for good, and its `test-ref` arm offered a `wiseowl/` test as a header to repair.

Re-probe on the build: 18 rows were `accepted-uncleared` with only `wiseowl/` witnesses (2,276 total, 2,258 with a `cases/` test), and 5 rows sat under `shared-test` through a `wiseowl/` test-ref.

Fix: a new witness kind `wiseowl-tree-only` (observable, unmet, authoring obligation); `accepted-uncleared` needs a `cases/` test; the `test-ref` arm reads `cases/` tests only (`SUITE_TREE`). The status line, the completeness report, the module docstring and the design doc carry the new term. Self-test case_09 plants each arm. Measured after: accepted-uncleared 2,258, wiseowl-tree-only 18, shared-test 219, no-test 126; observable and unmet unchanged (2,961 of 2,985). No inventory row changed verdict.

