Resume unit, 2026-11-14: the branch worktree-wf_f48cbfdb-e7e-1 is ABANDONED, superseded by worktree-wf_6fe280d5-b8c-1.

e7e-1 was the SPLIT attempt at the PB5395 group (wave 20262016 group A, report w20262016a-PB5395-report.md; six
non-merge commits ending 4cfc59440d: PB5395 step A, PB5396, PB5397, PB5398, PB5432 and its gate-red fix). Its successor
b8c-1 (wave 20262029 group A, report w20262029a-PB5395-report.md) cherry-picked all six onto main, resolving the
PublicAPI.Unshipped.txt, inert_lane.py and gate.py conflicts, and went on to PB5411, PB5433 and PB5434. `git cherry -v
worktree-wf_6fe280d5-b8c-1 worktree-wf_f48cbfdb-e7e-1` matches two patches outright and shows the four conflict-resolved
ones with the same subjects on b8c-1 (f0e7ac07b3 for step A, 01951efac4 for PB5396, 4241b5427c for PB5397, ...).
e7e-1's merge of worktree-wf_3a4f011a-b9e-1 is already on main. Its worktree was clean (status_delta: CURRENT,
uncommitted none). Its worktree is deleted (`prune_worktrees.py --apply --abandon`) once this record lands; PB5395 stays
open and b8c-1 carries it.
