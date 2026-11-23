# PB2726 — worktree-wf_ef90b3a1-3e3-5 ABANDONED (replayed into its successor)

Branch `worktree-wf_ef90b3a1-3e3-5` (wave 20262135 group E, SPLIT, five WIP commits 0b45c8d169..204b037f44) never
landed. Wave 20262139 group C cherry-picked all five onto 9bfe9e1c44 in `worktree-wf_46d3a156-23e-3` (report
`w20262139c-PB2726-report.md`, "Replay"): `git cherry` shows four of them patch-identical there, and the fifth
(0b45c8d169, PB2760) differs only by the `RuntimeAbi.pin` conflict the replay resolved (major 33 → 34). The successor
gated the replay GREEN (run 000001-20261122T205303Z) and carries the work forward, so nothing unique remains on this
branch. It is ABANDONED and removed with `prune_worktrees.py --apply --abandon worktree-wf_ef90b3a1-3e3-5`.
