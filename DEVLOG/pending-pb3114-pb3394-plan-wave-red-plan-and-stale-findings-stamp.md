## PB3114 + PB3394: a RED wave plan changes no shared state, and a FINDINGS stamp older than its branch tip plans no finisher

Wave 1131, group F: two planner defects in `scripts/orchestrator/plan_wave.py`, one root each, both re-probed on this
tree before the fix.

**PB3394 (the planner and the branch survey disagreed on a FINDINGS stamp at an older head).** Re-probe: a DONE report
`w1127a-PB7` stamped `FINDINGS 1 … 11f7b8ab0c0e` on an unlanded branch whose tip is `314d06415aaa` got a
`FINISH-FIRST: the branch review of w1127a` group from `plan()`, while `prune_worktrees.lifecycle()` read the same
branch WAITING FOR REVIEW: wave 1127's shape, where three finishers were planned on branches whose findings had already
been answered (and one branch was still receiving commits). The rule now lives in one place,
`prune_worktrees.stamp_covers(review, tip)`: a review stamp covers a tip when it names it or every commit since its
head is mechanical (`review_holds`, `unreviewed_since == []`). The sibling arm was wrong as well: the survey measured
`review_holds` only for a CLEAN stamp, so a FINDINGS stamp followed only by `enqueue.py`'s merge of main or its DEVLOG
stamp read WAITING FOR REVIEW (a re-review of nothing); both lifecycle arms now read one `covered`. The planner takes
the survey's coverage at the survey's tips (`prune_worktrees.review_coverage(rows)` → `plan(review_covers=…)`), and
`plan()` splits its FINDINGS predicate into `stamped` (any head: the lead set and the owned-branch wait) and `findings`
(covering the tip: the finisher). A stamped-but-uncovered note waits as `WAITING FOR REVIEW: …`, never a group. The
prune self-test gains `findings-moved` (WAITING FOR REVIEW to the survey, no finisher from `plan()` fed by the same
survey) and `findings-stamped` (DECISION, finisher planned); `test_plan_wave.py` pins the 1127 shape in both modes.

**PB3114 (a RED plan held its notes and burned its codes; a drive-relative `--scratch` was accepted).** Re-probe from
Git Bash with unquoted Windows paths on a scratch coordination directory: the planner reserved `COBOLNET4625`-`4627`,
created `COBOL-coordscratchw1131fprobe\` inside the worktree, recorded `w1130a` in the ledger, and only then went RED
on the stop file. Now the fleet paths have one rule in `coord.py` (`absolute_problem`, `scratch_problem`,
`fleet_stop_problem`), asked by `plan_wave.main` before it plans and by `make_dispatch_specs.main` before it writes
(exit 2, nothing changed), and read by `check_practices.py`'s groups-file check in place of its own regex;
`coord.coord_path` refuses a drive-relative `--coord` too. `make_dispatch_specs.py` records the dispatch ledger only
once `check_practices.py` is GREEN (`--no-record` renders and checks only). `plan_wave.main` renders and checks on the
PEEKED codes first, and only on GREEN reserves one block of codes, re-renders with them, records and writes the args
file. The unit prompts single-quote every Windows path (`units/wave.md`, `units/enqueue.md`) and `units/common.md` says
why. Sibling: `scripts/arch/r2_inputs.py` read the coordination directory from its own `COBOL_COORD` variable (the rest
of the tree reads `COBOL_COORD_DIR`); it now uses `coord`'s directory and stop files and refuses a bad `--stop-file`.
`test_plan_wave.py` drives `main()` end to end (refusal first, a RED render reserving and recording nothing, the GREEN
two-pass render, the renderer's own record-on-GREEN), each case mutation-checked.

Docs: DESIGN-orchestrator-loop section 9 items 2, 7 and 8 and the ledger row (items 7 and 8 also lost their stale
lead-id and train args), DESIGN-test-build-ci's review-rounds paragraph, MANDATORY-PRACTICES I10 and O10, the workstream
SKILL's Specs paragraph. Both notes landed, `closes_rows: []` (process tooling, no inventory row). No diagnostic code
used (COBOLNET4616-4618 returned).
