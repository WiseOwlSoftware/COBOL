## Entry 2687 — 2026-10-27 19:44 PDT — PB3987 witness: the landing lane retries a branch whose worktree a live agent or a hand lock holds

After PB3987 landed, the enqueue unit saw land_lane.py candidates() retry a DIED or GATE BUSY attempt on a row held by a hand lock and a finisher in flight (attempts 000274 and 000275 on worktree-wf_f781f6f8-448-20); the second merged main into the finisher's worktree and gated it RED. The note gains a dated witness paragraph and the candidates() site; its status stays open.

