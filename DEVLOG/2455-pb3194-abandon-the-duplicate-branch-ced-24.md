## Entry 2455 — 2026-10-24 03:26 PDT — PB3194: abandon the duplicate branch ced-24

`worktree-wf_0e51b14b-ced-24` implemented PB3194 a second time, after `fix-pb3194-linux-gate-clones` had already been implemented, reviewed and landed (PR #484). Its only other content, the merge of ced-21, is on main. The resume unit recorded it ABANDONED in kb/Work note PB3194 so `prune_worktrees.py --apply --abandon` can remove it. No code changed.

