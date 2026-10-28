## Entry 2686 — 2026-10-27 19:11 PDT — File PB3987: a bounced branch whose finisher merged main in reads WAITING TO LAND

The enqueue unit of 2026-10-28 watched the landing lane start `enqueue.py land` on two bounced branches (PR #716 and PR #714) inside
their finishers' worktrees, because `prune_worktrees.py` reads BOUNCED only at the pull request's head and a main-merge commit moves
the tip. No open note owned the rule (PB3275, PB3628, PB3527 and PB3103 are landed), so PB3987 is a new note, with the fix direction
recorded and not decided.

