## Entry 2539 — 2026-10-25 11:27 PDT — PB3723: parallel branches conflict at the append points the partition never sees

The owner asked why anything is serialized when branches exist, and chose to let wave 20261107's two same-file
pairs decide. Both pairs conflicted, and neither edited the member it was planned to share. E and F met only
inside `prune_worktrees.self_test()`, where both added test arms. D and H merged cleanly in `merge_drivers.py`
but met in one paragraph of `DESIGN-test-build-ci.md`. PB3723 (tier 1 of PB1957) asks for insert/insert hunks to
resolve themselves where that is safe, the owner's branches-and-merge model, instead of serializing by file.

