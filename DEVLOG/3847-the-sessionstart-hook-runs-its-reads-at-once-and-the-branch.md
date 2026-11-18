## Entry 3847 — 2026-11-18 15:14 PST — The SessionStart hook runs its reads at once, and the branch survey, the ruleset check and the fuzz status stopped being slow

The SessionStart hook took 32.7 s typical and 107 s worst over 51 runs (Claude Code transcripts). Its probe, `session-probe.ps1`,
ran about twenty independent reads in a row; timed alone they summed to about 57 s, of which the branch survey was 26 s,
`ruleset.py check` 9.4 s, `work.py check` 6.1 s, the tooling check 4.3 s and the fuzz status 3.8 s (kb/Work PB5939).

The probe is now `scripts/session_probe.py`, and `session-probe.ps1` is deleted with every caller and doc renamed. Each section
is a function that returns its lines. `run()` starts them all at once and prints them in the order the pwsh probe printed
them, and the hook runs the tooling check on the same pool, so the wall time is the slowest section. A section that raises
or hangs prints its own UNREAD line (each command has a 120 s guard) and costs the others nothing.

Four of the slow sections were slow at the root:
- `prune_worktrees.py`: the review-coverage loop (a `git merge-tree` and a `git diff-tree` per branch holding a review stamp) ran
  serially in the main thread, 18 s of a 32 s run, and it ran for landed branches whose lifecycle returns before it reads
  the result. Now the coverage reads run on the pool and only for rows that read them; every worktree's `git status`, the
  `git grep` of main's names and the pull-request read start together; the origin fetch and the landing order's register
  read run beside the survey. `--brief` is byte-identical on a stable branch population (old, new, old back to back; idle
  hours normalized): the survey self-test is green.
- `ruleset.py check`: the four reads after the repository name go out together; their answers are taken in the old order.
- `work.py check`: the sites rule reads the notes' site blocks side by side.
- `fuzz_history.py status` keeps its verdict under the object ids of what it extracts from origin/main, so an unmoved main is
  judged once; a test covers kept, moved and error cases.

Measured after `prune_worktrees.py --apply` had removed 102 of 113 branches (11 remain), three interleaved pairs on the
same tree: the old hook took 30.9, 32.0 and 30.6 s, the new one 8.0, 6.8 and 6.9 s. (Before the prune, under load, the old
hook took 63-94 s and the new one 9.4-16.4 s.) The `gh` pull-request read now starts first in `live_survey`, because the
survey's fixed cost does not shrink with the branch count: the register load (1.7 s), the `git grep` of main's names (1.3 s)
and the gh read (2.3 s) were each on its critical path. What is left on the hook's critical path is `work.py check` (about
5-7 s, the full register lint) and the survey's fixed floor (about 5 s); the per-worktree `git status` cannot drop its
untracked scan without changing the dirty count.

