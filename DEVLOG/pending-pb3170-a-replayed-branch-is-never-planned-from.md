## PB3170 — the planner plans no finisher for a branch another branch replays, and none from an abandoned one

**Defect.** The planner's own FINISH-FIRST pred tells a SPLIT successor to CHERRY-PICK its predecessor (never merge it),
so the successor holds the predecessor's work as other commits and is no descendant of it. `plan_wave.branch_carrier`
sees only a DONE branch that CONTAINS another by ancestry, so it never saw such a successor. Wave 1079 planned PB3127
from `…-28e-2` although `…-3a0-2` (a SPLIT report's branch, planned beside it) replayed all four of its commits, and
wave 1080 planned it again: two finishers for one set of commits, and an `after:` that would have held the carrier's
branch behind the finisher of its own predecessor. An ABANDONED record for a report's branch was not read at all.

**Re-probe.** A fixture with two SPLIT reports on `…-28e-2` and `…-3a0-2` (the second holding the first's commits as
cherry-picks) and no replay information planned a finisher for each, as the waves did.

**Fix.** One rule, `prune_worktrees.replays`, next to the `git cherry` rule that classifies a branch LANDED. It reads the
patch id of every commit every unlanded branch holds beyond origin/main from one batched read
(`git_batch.patch_ids`: `git diff-tree --stdin -p | git patch-id --stable`, two processes whatever the count; the per-pair
`git cherry` would be a process per pair). A branch is carried when branches that OUTRANK it hold every one of its
commits; the order is (distinct patches, then the later report, then name), so two branches that hold the same work
cannot carry each other and the largest of a family carries nothing: exactly one branch still does the work.
`plan_wave.plan` takes `replay` (`branch_replays`) and `abandoned` (`prune_worktrees.abandon_records`, the survey's
ABANDONED matcher run once over the notes for a list of branches):

- a SPLIT, NOT STARTED or gate-ended report whose branch is carried gets no finisher. Its note joins the finisher group
  of the branch that replays the most of it (the pred says the notes ride on that branch) or waits, named, while that
  branch has no finisher candidate (DONE and landing on its own, or still running);
- a branch replayed only in part keeps its finisher, whose pred names only the commits no other branch holds, in the
  report arm, the report-less unlanded-branch arm and the half-note arm alike (the siblings the note named);
- a carried report-less unlanded branch is not planned from: the carrier's entry plans the notes its subjects name;
- a branch recorded ABANDONED is never planned from. Its reports leave the newest-report map and its notes leave the
  unlanded map; the model routing still counts its reports as attempts.

The DONE-ancestor carrier of PB3150 is unchanged and consulted first, and a FINDINGS-stamped report is still planned
(its findings are answered on its own branch). `DESIGN-orchestrator-loop.md` section 4 and the planner's module
docstring (item 10) describe the rule.

**Witnesses.** `test_plan_wave.py` PB3170 block: the rule on real cherry-picks in a scratch repository (a carried
branch, one carried by two, one with a commit of its own, the largest of the family, an unrelated branch, two branches
with equal work), the planner's wiring (the later report ranks, ABANDONED and LANDED carry nothing), `abandon_records`
against `abandon_record` per branch, and the plan in six shapes, the first of which is the defect itself (no rule: two
finishers). `git_batch.py --self-test` gains three `patch_ids` arms (root commit, merge and empty commit absent, a
cherry-pick shares its id), declared in `GitBatchDriftTests`.

**Review round 1 (R1-1).** `replays` chose a branch's carrier as the outranking branch replaying the most of its
commits, a tie broken by branch NAME. In the planner's own successor chain (c-x, then a-mid cherry-picking it, then
b-top cherry-picking a-mid) c-x was carried by the middle link a-mid, which is itself carried and gets no finisher, so
c-x's note waited for a-mid to land (it never does) while b-top's finisher landed c-x's commits blind to the note. The
outcome depended on worktree-name order. Fix: `replays` settles branches highest key first and names as carrier (and in
`by`) only outranking branches carried by nobody, the tie going to the later report. Every patch of a carried branch is
held by an uncarried one (induction from the top of the order), so a carried branch always has a carrier that
has a finisher. Witness: two `test_plan_wave.py` checks on a real three-branch chain whose names disagree with the
report order (361/361).

**Not fixed here.** `prune_worktrees.contained()` (the branch survey) still reads ancestry only: a replayed SPLIT branch
stays a DECISION row until its carrier lands and `git cherry` reads it LANDED. The planner no longer dispatches it.
