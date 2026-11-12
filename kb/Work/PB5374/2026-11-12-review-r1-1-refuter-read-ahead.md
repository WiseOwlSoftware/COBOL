**Branch review R1-1 (2026-11-12, wave 20262008 group B review finish).**

Finding: the refuter record file a §8 row's `Refuted` cell names is read by the state rule (`Derivation.refuter_outcome`)
but was in neither hand list of the files a commit's view reads ahead (`inventory_view.inventory_at`,
`gen_ledger.measure_commits`), and a read-ahead `Tree` answered None for any unlisted path, so a claude-signed argued row
with an UPHELD record computed GAP at a commit and OK live.

Fix: `Schema.view_files` (the files the schema names) and `inventory_schema.refuter_files` (the files the register
names) are the one list; `inventory_schema.read_view_ahead` reads them in two stages for `inventory_at`, and
`measure_commits` reads the same two stages for every commit (two extra git processes only when a register names one,
constant in the commit count). `Tree.text` on a read-ahead tree raises `NotReadAhead` for a path it was not asked, so a
future input a list misses fails by name. `gen_ledger.VIEW_INPUTS` feeds `STAMP_PATHS` and `missed_points`' pathspec,
which had the same gap. Witnesses: `inventory_view.py --self-test`, `gen_ledger.py --self-test`.
