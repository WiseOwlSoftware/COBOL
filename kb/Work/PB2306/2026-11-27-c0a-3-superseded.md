# PB2306: the wave 20262168c branch is superseded by wave 20262172b

`worktree-wf_05bb8d55-c0a-3` ABANDONED: superseded by `worktree-wf_bfd20f03-a44-2`, which cherry-picked all four of its
commits (step 1, LinageControl: `git cherry worktree-wf_bfd20f03-a44-2 worktree-wf_05bb8d55-c0a-3` marks 0638506b4c,
360a5989d5 and 21070d41bf as already present; f752b6a90d's change is in 5891127df2, re-resolved against main). a44-2 then
finished PB2306 with step 2 (RecordCodec, 7d8c222c01) and gated GREEN at cfb2fd35d2. c0a-3 holds nothing a44-2 lacks.
Decided by the resume unit of 2026-11-27.
