# PB1723 addendum (2026-11-24): the letter is stale, the mechanism survives at five sites

**Source:** `E:/COBOL-coord/scratch/reports/w20262164b-PB3205-report.md`, New defects (note-ready); the lead shares no root with the
other leads of that report.

The note's two `default_store` copies (`impacted_tests.py`, `record_impact_map.py`) were deleted with PB4328, so its title names code
that is gone. The mechanism is not: `git rev-parse --git-common-dir` is still resolved separately at five sites, and they differ in
whether they absolutize or resolve the result:

- `scripts/buildgraph.py:653`
- `scripts/gate_slot.py:257` (`slot_dir`)
- `scripts/orchestrator/account.py:242` (asks git for `--path-format=absolute`)
- `scripts/orchestrator/land_lane.py:561`
- `scripts/prune_worktrees.py:983`

Whoever owns PB1723 re-verdicts it against these five sites, retitles it, and unifies them in one reader of the git common dir (git can
return an 8.3-style or forward-slash path on Windows, the original concern). Not probed in this filing.
