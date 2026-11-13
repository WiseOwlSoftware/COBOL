## PB3726 step A (PB5395): `conformance_source.py`, the one reader of docs/CONFORMANCE.md

`scripts/spec/conformance_source.py` now opens docs/CONFORMANCE.md for every script that reads it: `load(root)` and
`parse(text)` return a `Conformance` with `section(number)` (a section runs from its heading to the next heading of its own or a
shallower level), `rows`/`rows_at` (a table's first row is its header; cells split on unescaped pipes), the typed §7 and §8
rows, `a3_dispositions`/`a4_dispositions`, `facility_titles`, `bullet`, `paragraph`, `row_line` and `text`. The parsers each
reader held moved into it and were deleted: `inventory_schema.py` lost `REGISTER_PATH`, `register_cells`, `register_section`,
`register_section_at`, `section7_rows`, `register_determinations` and `derivation_rows`; `gen_ledger.py` lost its posture
parser, `differential_fuzz.py` its section 5 row regex, `docchecks/common.py` the path, `section` and `table_row`. Every reader
(`docchecks/register`, `facts`, `inventory`, `design`, `audit_annex_a1`, `gen_ledger`, `differential_fuzz`, `phase_b_batch`,
`inventory_view`'s planted probe) calls it.

The re-probe corrected the design's estimate: `generate.py`'s completeness report reads the registers' extract, never the
Markdown, so it needs no change and the INERT LANE gets no CODE_READS entry (an entry for a reader that does not exist is as red
as a missing one). The one place `inventory_schema.py` did open the document was `DerivedSelector.register`; it now reads
`Schema.register_leads()` from the extract, and the no-Markdown probe asks it too. DESIGN-orchestrator-loop.md section 16.8 says
so.

Proof: `audit_annex_a1.py --json`, `audit_derivations.py --check`, `gen_ledger.py --check` and `doc_audits.py --check` print the
same bytes before and after; `measure_conformance()` and `declined_modules()` compared old against new in one process are equal;
the new cases (`scripts/selftests/spec/conformance_source/test_reader.py`, 6 cases, 31 expectations) pass.

### PB3726 step PB5396: `public_api_baselines.py`, one order for PublicAPI.Unshipped.txt

The baselines' rules (`SYMBOL_BASELINE`, `is_symbol_baseline`, `baseline_duplicates`, `baseline_texts`, `baseline_stops`) moved out
of `landing_check.py` into `scripts/public_api_baselines.py`, whose arm 0b now calls it; the module adds `ordered`,
`order_problems`, `check` and `write`. `--check` is the gate audit `PUBLIC API BASELINES` (gate.py `AUDITS`) and a step of CI's
`audits` job, the same list (`ci_audits_not_in_gate` stays GREEN). The runtime's Unshipped baseline was out of order from line 138
(`sort -c`); `--write` put it in ordinal order once, its line multiset unchanged (`sort | md5sum` before and after equal, 1,140 lines).
Cases: `scripts/selftests/public_api_baselines/test_rules.py` (5 cases): duplicates, out-of-order, the fixer, the audit RED on each breach
and on a tree with no Unshipped baseline.

### PB3726 step PB5397: inert_lane.py's self-test became case files

`inert_lane.py`'s 360-line `self_test`, its `wiring` check of the real tree, `_refused` and the `--self-test` flag are gone from the
script. Its 45 expectations are 44 cases in `scripts/selftests/inert_lane/`: `test_classification.py` (the pure classification, 24
cases), `test_scratch_repository.py` (the changed paths, `check` and `code_readers` on planted trees; each case asks
`fixtures.world(*steps)` for the repository as it stood at that point, 9 cases), `test_static_reads.py` (the C# lexer and
`markdown_readers`, 5), `test_watch.py` (the inotify arms, which pass with their reason off Linux, 5) and `test_wiring.py` (the real
tree's wiring, 1). Oracle: the multiset of expectation labels before (read from the old source by `ast`) and after is equal, 45 and
45 (`labels.py`, output in the scratch directory); `selftest_layout.check_repo` is `[]` with `scripts/inert_lane.py` gone from
`REMAINING`; `inert_lane.py check` GREEN. The converter that cut the function (statement-by-statement, a case per `expect`, a
statement used by one case attached to it and one used by several hoisted) is not kept: the case files are the record.

### PB3726 step PB5398: inert_lane.py split into the lanes' modules

`inert_lane.py` (1,150 lines) is now the CLI (95 lines: `check`, `main`) over three modules: `lane_classification.py` (the lanes, `Classification`,
`CLASSIFICATION`, `under`, `listing_glob`, and the git reads `changed`/`tracked` of the paths it classifies), `source_reads.py` (the build's
and the compiler tests' static reads: `code_readers`, `import_closure`, the C# lexer, `markdown_readers`) and `inert_watch.py` (the inotify
watch). Three corrections to design 16.12's table, recorded there: `Classification.check` became the CLI's `check(classification, root)` (the
method would have made `lane_classification` import `source_reads`, which imports it back), `_listing_glob` is `lane_classification.listing_glob`
because `under` calls it, and `changed`/`tracked` live with the classifier. Every category of `CLASSIFICATION` is in key order and
`Classification.order_findings` (called by `check`) refuses one out of order, with cases. Importers re-pointed: `gate.py`, `buildgraph.py`,
`line_endings.py`, `self_tests.py`, `land_lane.py` (and the messages that name where an entry is declared); `script_layering.py` declares the
new line and split. Oracle: `classify` over the 20 landings before this branch's base gives the same lane, flags and reason with the old and the new module
(20 of 20, `lane_oracle.txt`), the classification's four categories are equal as data, `inert_lane.py check` is `[]` on both, and the case files pass.

### PB3726 step PB5432: gate.py's self-test became case files, FakeHost its own module

`gate.py`'s 1,390-line `self_test`, `audit_shape_arms`, `FakeHost` and the `--self-test` flag are gone from the script (it lost 1,430 lines). The 123
expectations are 31 cases in `scripts/selftests/gate/` (`test_verdicts.py`, `test_regate_and_audits.py`, `test_lanes.py`, `test_driver.py`,
`test_runs_and_locks.py` (one case of 67 expectations: its arms share the locks and runs of one world and a few closures over it, so they
were not cut apart), `test_await.py`, `test_tree_audits.py`), with `fixtures.world()` (the scratch directory, the `gate(name, **fake)` runner and
`said`) and the planted ONE SHAPE audit. `FakeHost`, shared with `arch/perf_baseline.py`'s self-test, is the ordinary module
`scripts/gate_fake_host.py`; `perf_baseline.py` imports it. `self_test_arms.DECLARED['scripts/gate.py']` names 24 cases instead of 85 labels (each label
resolved to the case that holds it), `NET_KNOWN` names `test_verdicts.py`, and `scripts/gate.py` is out of `REMAINING`. Oracle: the multiset of
labels before (the `arm(...)` calls of the old function, read by `ast`) and after (the `expect(...)` calls of the case files) is equal, 123 and 123
(`gate_labels.txt`); every case file passes; `self_tests.py --self-test`, `perf_baseline.py --self-test`, the layout rule and `self_test_arms.py --self-test` are GREEN.
