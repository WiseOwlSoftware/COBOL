## PB5052 review leads filed as PB5095 and PB5096

The branch review of PB5052's fix (PR #1437) left two non-blocking leads. PB5095 records that the figure identity check's no-drawn-figure branch has no self-test case. PB5096 records that the spec-reconciliation README still calls `sweep_figures.py --check` the standing gate while the gate audit is `--identity`. Neither existing note covered these, so each got its own. No code changed.
