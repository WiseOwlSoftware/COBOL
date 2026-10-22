## Entry 2361 — 2026-10-22 13:22 PDT — kb/Work PB3528: two SPLIT branches sharing a file deadlock the wave planner

Wave unit 1150 planned 0 groups, in the PB3353 campaign and in a fix-lane dry run alike. The two SPLIT branches of
wave 1141, `worktree-wf_31aba3ec-d06-1` (PB3338) and `worktree-wf_31aba3ec-d06-2` (PB3493), both change
`docs/CONFORMANCE.md`. Each is idle past `--min-age`, so the branch survey reads it DECISION, and the file-set
partition treats a DECISION owner as running work. Each finisher is exempt only from the branch it continues, so
PB3338's finisher waits on d06-2 and PB3493's on d06-1, and neither is ever planned. kb/Work PB3528 records the
defect and the fix shape (a branch this wave finishes is not running work to the wave's other groups; two such
finishers sharing files chain as successors). No fleet was launched.

