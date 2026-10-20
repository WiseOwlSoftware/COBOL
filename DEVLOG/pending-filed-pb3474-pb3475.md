## Filed PB3474: two places still state the never-work rule beside coord.NEVER_WORK

The round-1 review of the PB3465 branch left three leads. R1-1 and R1-3 are one mechanism and are filed as kb/Work PB3474 (MINOR, process-only): `prune_worktrees.py` `classify()` still hand-codes `TestResults/` and `STATUS.md` (and a `tools/` exclusion `coord.NEVER_WORK` lacks) for its added-files reader, and the `enqueue.py` module docstring still lists only `STATUS.md` and `settings.local.json` as the clean-worktree exceptions.

R1-2 was not filed. It said `DEVLOG/pending-pb3465-settings-local-collision.md` ends "no code change in this landing" while `stamp` folds it into the fix's entry. Checked on the branch head: that pending file no longer exists, the filing entry is the already-landed Entry 2304 (its claim was true for its own landing), and the fix's Entry 2309 carries no such sentence. PB3475 was allocated and is unused. No code change in this landing.
