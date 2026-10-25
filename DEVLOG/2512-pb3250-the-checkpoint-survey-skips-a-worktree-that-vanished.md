## Entry 2512 — 2026-10-24 20:23 PDT — PB3250 — the checkpoint survey skips a worktree that vanished after it was listed

`checkpoint.py` (the supervisor's frequent handoff, DESIGN-orchestrator-loop.md 5.1) lists the linked worktrees and then
measures each one. It ran git with the worktree as the subprocess `cwd`, so a worktree removed between the listing and the
measurement (`prune_worktrees.py --apply`, a finished agent's cleanup) raised `NotADirectoryError: [WinError 267]` out of
`subprocess.run`; the supervisor logged the traceback and skipped the whole checkpoint write for that tick, and a unit that
died then got a handoff built from an older checkpoint. The same helper returned `""` for every git failure, so a worktree
git could not read would have been written as a clean `uncommitted: 0`.

Re-probe: the new self-test case deletes a worktree right after `git worktree list` returns; on the unfixed `checkpoint.py` it
raised the WinError 267 above (and so did a worktree whose `.git` pointer file is unreadable).

Fix at the root, in `checkpoint.git`: the directory goes through `git -C`, so a missing directory is an ordinary exit 128, and
the function returns `None` for a failure. `worktrees()` leaves out a worktree whose status git cannot read and whose
directory is gone, keeps one whose directory exists with `uncommitted: null` (unknown, never a clean 0), and reads the
`STATUS.md` headline tolerating an `OSError`. The listing moved into `listed_worktrees`, which says on stderr when `git
worktree list` itself fails and records no worktrees (it does not raise: a first version did, and the supervisor's
orchestrate self-tests for the handoff, the core loop and the startup, which run `synthesize` against a plain directory,
went RED at the gate; a dead unit's handoff must still be synthesized).

Sibling sweep: `prune_worktrees.git` and `plan_wave`'s worktree walk already use `git -C`; `devlog.worktree_of` and
`enqueue.worktree_of` look up one named branch's worktree and run no git there; `land_lane` and `landing_measure` run git in
the one worktree they land, not in a surveyed set. `scripts/orchestrator/test_checkpoint.py` pins the vanishing worktree,
the unreadable one and the failed listing.

Gate: the first gate (run 000001-20261025T030851Z) stopped RED in its self-test audit on the three orchestrate self-tests
above; the fix is the stderr-and-continue listing.

