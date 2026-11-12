## Entry 3541 — 2026-11-12 13:41 PST — PB5444 filed: the survey's CLI reads no one-file chain records

The enqueue unit of 2026-11-12 found `prune_worktrees.py --brief` and the session probe listing
worktree-wf_c902d12a-c75-3 WAITING TO LAND while the landing lane's own survey held it IN FLIGHT as a one-file chain link
(kb/Work PB4396). The CLI calls `live_survey` with no coordination directory and `chain_records` returns nothing for that.
kb/Work PB5444 records the site, the witness and the fix direction; nothing is fixed here.

