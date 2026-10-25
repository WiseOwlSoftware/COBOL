## The branch survey and the review-findings hand-off: four fixes in one root (PB3386, PB3409, PB3404, PB3713)

Wave 20261107 group E. The four notes share `scripts/prune_worktrees.py` and the merge drivers; each was re-probed on
this tree's scratch repository (`python scripts/prune_worktrees.py --self-test`) before it was touched.

**PB3386 — a carried branch is carried whatever its report says.** The note's headline case, a SPLIT predecessor merged
by a WAITING TO LAND successor, no longer reproduced: PB3652 had admitted SPLIT and NOT STARTED rows. Its wider claim
did: `contained()` still skipped every DECISION row that had a report unless the report was FINDINGS-stamped or
unfinished, so a DONE report 30 h old, one a later commit had outrun, one whose lead note had landed, and a review
finisher's BLOCKED branch (`rpred-fix`) read DECISION beside a carrier that lands the same commits. The ancestry arm now
carries every DECISION row a carrier contains; the replay arm keeps its narrower input (the rows the planner plans a
finisher from). Self-test: `cw-pred`/`cw-stale`/`cw-moved`/`cw-lost` chained under a WAITING TO LAND `cw-succ`.

**PB3409 — `--apply` pruned nothing and reported GitHub's own deletes as errors.** GitHub deletes a merged pull
request's head branch, nothing pruned the tracking refs, and `git push --delete` of one failed with `remote ref does not
exist` (13 of 14 branches in the 2026-10-18 run; 178 refs listed beside 21 on origin). `prune_stale_remote_refs` runs
`git remote prune origin` once at the top of `apply`, so `drop_remote` reads origin's real state; a push that fails now
stands alone. Self-test: a stale ref (the arm fails with the prune removed) and a delete origin refuses
(`receive.denyDeletes`), still reported, its ref kept.

**PB3404 — one owner per review-findings case.** The rolling wave told a successor's reviewer to record an
unanswered predecessor's findings again (the successor's finisher fixes them) while `carrying_findings` held the
successor until the predecessor's own file recorded CLEAN, which its next-wave finisher can never write (a new wave
writes a new file) and whose head reaches main only through the successor that waits. `review_findings.capped` is the
one definition of the round after the last review finisher (the verdict's own `blocking` severities); a CAPPED verdict
stays the predecessor's next-wave finisher's, an UNCAPPED one is the successor's: `prune_worktrees.review_answers`
releases a successor whose own CLEAN review contains the verdict's head, the planner reads the same rule
(`answered_by_carrier`, a new `answered` argument of `plan_wave.plan`) and plans no second finisher, and the wave's
`predecessorEnd` and the successor's note say the same. Docs: DESIGN-test-build-ci, DESIGN-orchestrator-loop, SKILL.md.

**PB3713 — the LANDED proof merged without the drivers.** A branch whose entry another carrier had put on main beside
its own conflicted in a keyed catalogue under a plain `git merge-tree` and never read LANDED. `merge_drivers.merged_tree`
is now the one definition of a clean merge's tree; `is_drivers_merge` and `merges_to_main_unchanged` both read it
(negative control: the fixture fails on exactly `key-landed` with the old call). `MergeDriverDriftTests` holds
`prune_worktrees.py` to it.

**Self-review** (silent-failure, comment and test lenses): an offline prune says so and drops nothing (the deletes then
report their own failures); `findings_answered_by` answers False for a torn reading, a missing head or no CLEAN review,
each pinned; the PB3713 proof keeps the strict tree comparison rather than `is_drivers_merge`'s pin leniency; comments
cite the notes. Gate: filled in by the report.
