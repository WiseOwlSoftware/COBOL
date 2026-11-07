## Record three superseded 1985-wave branches as abandoned, and file what PB4911 left (PB4914)

A note-only change. Wave 20261893 (PR 1272) cherry-picked the unlanded work of three earlier branches, two of PB4849 and one
of PB4899, onto main and landed it. The branch survey still held them IN FLIGHT or DECISION, which kept PB4897 from being
planned. Their notes now record each one ABANDONED as superseded, so `prune_worktrees.py --apply --abandon` can delete them.
PB4914 files the leads of PB4911's last wave (PR 1274): 580 undecided design-doc candidates, citations composed at run time
in console output, two citation readers that skip the tooling, and two low review items.
