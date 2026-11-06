## kb/Work: PB4759 waits on PB4808, and its empty branch is abandoned

Wave 20261879 group D stopped on PB4759 with no code change. The cd-name, file-name and data-name USE FOR DEBUGGING triggers take
their semantics from X3.23-1985's Debug module chapter, which is not in the repository. The work is also a redesign of the debug
facility, too large for one slice.

The note ranked second in `work.py next`, so a later wave would have dispatched it again. PB4808 now holds the missing step: check
GnuCOBOL, read the IBM and Micro Focus manuals, and end in a design-doc section or one owner question. PB4759 is `blocked_by` it.
The wave's reasons reach main here. They were only a commit on `worktree-wf_d77cc414-ca7-4`, which is now recorded as abandoned
because it holds nothing else.

No compiler change; the gate ran the inert lane.
