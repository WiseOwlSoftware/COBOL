## PB2652: account 2's duplicate PB1754 branch is abandoned

Account 2's wave 20262143 replayed three of account 1's PB1754 branches as finish-first rows of its own cluster by mistake and
handed them back. Its group C branch, `worktree-wf_6f9eca3a-cc1-21`, did the same nine notes as account 1's wave 20262147 group B,
which landed as #1996. kb/Work PB2652 records the branch ABANDONED, so `prune_worktrees.py --apply --abandon` removes it and no
finisher is planned over it. Its two `editions: []` addenda were already true on main. The planner gap is PB6059.
