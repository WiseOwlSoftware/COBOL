## 2026-11-10 20:0x (operator): a queued group that never starts also HOLDS its notes

Wave 20261973's groups G (PB5081, PB5091) and H (PB5071) were queued on 2026-11-09 and never began: the wave was stopped
at 00:40 on 2026-11-10 and its runner died with that session. They had no scratch directory, worktree, branch or report.
progress.py posted a STALL for each every hour (1362 min at 19:57), which is this note's false stall. But the hold is a
second harm. `reports.answers` decides "a dispatch returned" from the group's own report, and `held_by_dispatch` holds an
unanswered dispatch's notes from the planner. So the three compiler notes sat unplanned for about 22 h, in the same
window as the owner's "compiler work first".

The operator unblocked them by hand with a `NOT STARTED` report for each group
(`E:/COBOL-coord/scratch/reports/w20261973g-PB5081-report.md`, `w20261973h-PB5071-report.md`; codes COBOL6832-6837
returned). progress.py's running count dropped from 3 to 1, and plan_wave's finishers-first rule takes the notes next.

For the fix: a dispatch whose runner is gone and which never started (no scratch directory, no worktree) should be
answered mechanically, for example by the runner's death recording `NOT STARTED` for every group it had not begun. Then
neither the hold nor the stall outlives the run.
