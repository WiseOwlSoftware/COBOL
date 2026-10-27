## A merged branch reads LANDED at once, not IN FLIGHT for three hours (PB3609)

`prune_worktrees.lifecycle` gave a MERGED or LANDED branch the state LANDED only when it was idle past `--min-age`; a branch
whose pull request had just merged read `IN FLIGHT, touched N min ago` for three hours. The survey is what the session probe,
the planner (`inflight.GONE_STATES`) and the landing lane read as "what is not on main yet", so the backlog looked three times
its size, and on 2026-10-24 the lane started `enqueue.py land` on a branch 14 minutes after its pull request merged, because
the re-arm hold (`land_lane.rearm_hold`) keys on the survey's state and the merged branch was not in `QUEUE_OWNED`.

Re-probe on this tree: the real survey showed 13 MERGED rows; with the fix all 13 read LANDED (the two that did not were
held by a live agent's lock, which still wins, correctly).

The fix, at the one place the rule belongs:

- `lifecycle`: LANDED for every row whose content is on main and whose worktree holds nothing uncommitted, whatever its age.
  The age now guards the deletion only: `apply` keeps a LANDED row touched inside `--min-age` and says `LANDED, kept: ...`.
- "Merged" alone is not "its work landed": a branch cut from main a moment ago, with no commit, is an ancestor of main too.
  The evidence that a branch carried work is topological, not a reflog read and not a pull-request read: a merge-queue landing
  leaves the branch's commits on the merge commit's second parent, so a MERGED tip that is not on origin/main's first-parent
  line is a commit a pull request brought in. A tip ON the line (cut from main, or fast-forwarded to it) has no commit of its
  own (`Row.on_main_line`); it keeps the old reading, IN FLIGHT while fresh. The line is one `git rev-list --first-parent`,
  held by `git_batch.Ancestry.on_base_line` (the survey's one-process-per-question rule, PB3582).
- Review R2-1 of the filing: (a) the merged-pull-request alternative is dropped (the topology test makes it unnecessary, and the
  git-only lifecycle has no pull requests); (b) a branch fast-forwarded to newer main differs from its creation commit yet
  carries no work, and the topology test reads it correctly (self-test case `merged-ff`).
- The lane's view: `land_lane.drop_stale_rearms` drops (and says it drops) the re-arm of a branch that reads LANDED, and
  `rearm_hold` says the work is on main rather than that the queue owns it. The self-test replays the 2026-10-24 shape (12d).

Goldens (all self-tests, run by the gate's script-self-test cases): `prune_worktrees.py --self-test` gains `merged-own` (one
commit merged by a `--no-ff` merge, touched a minute ago: LANDED, kept by `--apply`), `merged-bare` and `merged-ff` (IN FLIGHT
while fresh, LANDED once idle), a dirty fresh merged branch (IN FLIGHT); a mutation of the rule back to the old `not fresh`
turns five checks red. `git_batch.py --self-test` gains the `on_base_line` arms; `land_lane.py --self-test` gains 12d.

Limit: a branch whose own commits were fast-forwarded into main directly (before the merge queue) has its tip on the line and
reads by its age, as before. No merge-queue landing has that shape.
