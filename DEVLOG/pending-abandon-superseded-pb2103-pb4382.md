## Two superseded SPLIT branches recorded ABANDONED (PB2103, PB4382)

A resume unit decided the three DECISION rows `prune_worktrees.py` listed on 2026-11-02. All three are SPLIT
implementer branches whose finishers landed the same work, and `git cherry origin/main` marks every commit `-`. The
only file each tree holds that main lacks is its unstamped pending DEVLOG entry. `worktree-wf_0a800ee7-313-1`'s
ABANDONED line was already in `kb/Work/PB2103`, so it was removed. This change adds the lines for
`worktree-wf_bedc88aa-30a-1` (PB2103 group, landed through `worktree-wf_7ac2b995-6fd-5`, PR #1019) and
`worktree-wf_f5aa0389-c3d-1` (PB4382 group, landed through `worktree-wf_8cb75a40-6b0-8`, PR #1021), so
`--apply --abandon` can remove them. PB2103 part 2 stays open. No code changes.
