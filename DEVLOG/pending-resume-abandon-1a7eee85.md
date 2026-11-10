## The superseded PB5045 first-attempt branch is recorded ABANDONED

The resume unit of 2026-11-09 found two DECISION rows. `worktree-wf_1a7eee85-bc8-1` (PB5045, SPLIT at page B-11) is
superseded: each of its sixteen WIP checkpoints has an equivalent on main under the same subject, main carries
Amendment 2 on through page B-48, and its draft DEVLOG entry is superseded by the stamped entries 3390 and 3399. It is
recorded ABANDONED in kb/Work/PB5045, so `prune_worktrees.py --apply --abandon` can remove it. The second row,
`worktree-wf_30df8fbb-94c-1` (PB5044), carries a self-test witness main lacks (the one-glyph monospaced word `M`, the
only witness of the `len(text) < 2` guard in `figure_raster.underlined_hints`) and goes to review and landing.
