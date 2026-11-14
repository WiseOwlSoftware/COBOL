## PB5483: a root fix lands first, and the landing lane re-runs one cause once

The owner, 2026-11-13: "we rerun things far too many times, often in CI, rather than chase down and fix the ultimate
issue. Then we should immediately get the issue fix in the build rather than waiting for all preceeding jobs to encounter
the same problem." Every landing joined the BACK of GitHub's merge queue (`gh pr merge --auto`), so the branch that removed
a red waited behind every pull request queued before it, each of which could meet the red first; and the landing lane
re-queued a RUNNER or INFRA red once per landing, so one flake was re-run once on every pull request it hit (PR #1607 met
kb/Work PB5478's flake twice, runs 43550577247 and 43553292493).

`enqueue.py land <branch> --fix-first "<cause>"` now enqueues at the FRONT with GraphQL `enqueuePullRequest(jump: true,
expectedHeadOid: <pushed head>)`, dequeuing an entry already queued first. The cause is required and must name the red
(a kb/Work note the worktree carries and/or a failed run id); an unstated one is refused before the land lock. Every
other guard is unchanged. A refused jump is queued at the back like any landing, exit 17, never lost. Each attempt goes
into the landing record's new `fix_first` list. No landing is held while a fix-first entry is queued: anything queued
after it is built on top of it.

The lane now records each red's test cases and re-runs one CAUSE once: a second red of a test case already re-queued on
any pull request, with no merged fix-first landing naming its runs, is held (`hold-for-fix`, a new act) and the operator
is alerted to land the root fix. `landing_measure.py summary` counts re-queues and holds per cause, plus the jumps.

Evidence: `enqueue.py --self-test`, `landing_measure.py --self-test` (13 arms) and `land_lane.py --self-test` PASS. With
`jump` removed from the mutation, three enqueue arms FAIL. No real pull request was queued with `jump`: the first real use
must confirm GitHub accepts the mutation and what it answers for a pull request whose check is still pending.

The branch review (round 1) found that a hold had only one way out: a fix landed with `--fix-first` naming the cause's runs.
Most fixes land the ordinary way, so a fixed cause would stay held forever, and once any run was named a test could never
be held again. Fixed: the open causes are read in time order, so a release covers only earlier reds. A hold names the
open kb/Work note that mentions the failing test, and is released when that note lands on main, when a fix-first landing
names the runs, or by `land_lane.py release-hold`. A hold older than 12 hours is alerted again. The per-cause figures
and the hold now share one key (each red test case). The lane treats exit 17 as a queued landing and alerts once.
