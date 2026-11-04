## The fix lane plans an unsited ready defect as a group of its own (PB4566)

The owner asked to fix the open compiler defects. Two of them, PB3600 and PB3957, were open, unblocked and in
`work.py next`, and two operator-steered waves planned neither. A dry run of `plan_wave.py` did not mention them at all.

The cause was a dropped list. `fix_clusters.py` resolves a note's sites only against `src/WiseOwl.Cobol.*` `.cs` and
`.g4` files; both notes are sited in data files under `tests/version-matrix/`, so it listed them as `unsited`.
`run_fix_clusters` returned that list beside the clusters, and the planner read the clusters alone. The ledger read the
same call and counted each unsited note as a group of one, so the page promised work no wave would do.

`clusters.fold_unsited` now makes every unsited note a one-note group and returns the clusters only, the rule the
campaign lane already followed. The planner's `--clusters-json` path folds the same way, and the ledger's separate
unsited count is gone. The dry run at 1b7b9f19a planned PB3600 and PB3957 as groups A and B; `test_plan_wave.py` holds
the fold and the planning of a folded note.

A sibling bug in the public base is filed as PB4567: `fix_clusters.sites` cannot match a hyphenated file stem, so
`reserved-words.json` would stay unsited even with a data root in the population. It does no harm today (no source file
under `src/` has a hyphen in its name, and an unsited note is now planned anyway); its fix needs a base release and a pin
move.
