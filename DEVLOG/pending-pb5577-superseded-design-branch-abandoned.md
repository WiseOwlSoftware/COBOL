## PB5577: the superseded first design branch is recorded abandoned

The resume unit classified `worktree-wf_7d5ef379-5e3-1` as a DECISION row: one WIP commit holding the first draft of the
VALIDATE facility design. Main already carries that design, newer, through the S1 landing (DEVLOG 3684), and the branch's
only file absent from main is a pending DEVLOG entry whose content 3684 records. The branch is recorded ABANDONED in
`kb/Work/PB5577/2026-11-15-abandon-superseded-branch.md` so `prune_worktrees.py --apply --abandon` can remove it. No
compiler change.
