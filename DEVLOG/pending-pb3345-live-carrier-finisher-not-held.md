## kb/Work PB3325 gains a second instance; PB3345 retired as its duplicate

The operator filed kb/Work PB3345 on 2026-10-17. On that day the attended session's rolling wave (wave 1110, PB3339)
stamped `FINDINGS 2` on its implementer's branch. The wave's own review finisher then worked in a new worktree that had
merged that branch, while `plan_wave.py --dry-run` still planned a second PB3339 finisher. The window closed when the
finisher's report named its own branch (CLEAN at round 2), and no duplicate was dispatched.

The branch review showed this is the open note PB3325: the rolling wave never records its review finishers in the
dispatch ledger. PB3345 is retired unlanded. Its evidence and four readings are now PB3325's section "Second
instance". The fourth reading is the file-set partition's "the hold decided those" exemption, which assumes the
ledger holds every live dispatch. PB3325's fix shape now names that premise and a self-test for both shapes. Notes only;
no source change.
