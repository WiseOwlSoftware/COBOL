## The resume unit records a landed-elsewhere PB3178 branch ABANDONED

`worktree-wf_ee682338-d7f-1` (wave 20262148, PB3178 pilot) read DECISION in the branch survey although every one of its six
commits is patch-equivalent to main: wave 20262150 group A landed the same work as DEVLOG 3978, and the only file the
survey found absent was the branch's un-numbered `DEVLOG/pending-*.md`. The addendum
`kb/Work/PB3178/2026-11-23-d7f-1-landed-elsewhere.md` records it ABANDONED with that evidence, so
`prune_worktrees.py --apply --abandon` can remove it. The same unit applied the three ABANDONED records an earlier resume
unit filed (23e-3 under PB2726, 23e-6 under PB2652, 772-1 under PB2723).
