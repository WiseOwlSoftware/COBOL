## Entry 2513 — 2026-10-24 20:30 PDT — PB3665's bounce filed as PB3693 and PB3694; PB3672 retired into PB3673 and PB3674; the environment-cause pattern named

PB3665's pull request #541 bounced because the branch carried PB3613's branch, which the landing lane also landed
alone (PR #535). PB3693 files that double landing: `devlog.py stamp` still folds a carried branch's entry, and nothing
tells the lane or `enqueue.py` that a carried branch is being landed separately. The merge group then kept
`CobolNum.GroupDigits` twice in `PublicAPI.Unshipped.txt` under `merge=union`, which PB3694 files, together with the
fact that only GitHub's single-base merge produced the duplicate, a CI-invariant gap. The operator's PB3672 is retired
as a duplicate of the loop's PB3673 and PB3674, which hold its evidence. Those two notes and PB2665 now name their one
mechanism: an environment cause read as a failure of the work. The gate already classifies such causes once
(`run_gate_legs.NOT_RUN_CAUSES`, PB3526); the fix extends that classification to the pin settle, `enqueue.py`'s exit
mapping and the loop's unit accounting.

