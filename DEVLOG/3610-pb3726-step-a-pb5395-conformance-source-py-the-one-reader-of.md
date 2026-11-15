## Entry 3610 — 2026-11-14 09:43 PST — PB3726 step A (PB5395): `conformance_source.py`, the one reader of docs/CONFORMANCE.md; PB3726 steps: the gate, the landing lane and the inert lane leave their self-tests and split by job (PB5396 to PB5398, PB5432 to PB5434, PB5411); PB3726 step B1: docs/CONFORMANCE.md made regular in place, and the first commit of step B2's citation burn-down (PB5429, PB5430); The conformance statement's pointers, its citation arm, and the statement in every package (PB5430 commit 3, PB5431)

### PB3726 step A (PB5395): `conformance_source.py`, the one reader of docs/CONFORMANCE.md

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

### PB3726 steps: the gate, the landing lane and the inert lane leave their self-tests and split by job (PB5396 to PB5398, PB5432 to PB5434, PB5411)

Seven steps of the pipeline restructure land together on one branch, in the order the work ran. The branch also carries the wave
20262028a and 20262027a landings it was planned onto (PB5373, PB5118) and the first implementer's PB5395 step A.

**PB5396, the public API baselines.** `scripts/public_api_baselines.py` owns the baselines' rules (moved from `landing_check.py`) and
its `--check` is the PUBLIC API BASELINES audit in the gate and CI. `PublicAPI.Unshipped.txt` was out of the analyzer's order from
line 138; `--write` ordered it once, the line multiset unchanged, and this branch merged main's 34 new symbols into that order.

**PB5397 and PB5398, the inert lane.** `inert_lane.py`'s 45 expectations became 44 cases in `scripts/selftests/inert_lane/`; the module
split into `lane_classification.py` (the lanes and the one classification, its entries in key order), `source_reads.py` (the static
reads of build files, test sources and scripts), `inert_watch.py` and the 95-line CLI. The classification over the last 20 landings is
identical before and after. Main changed `inert_lane.py` while the split sat unlanded (the C# lexer moved to `csharp_lexer.py`, the
RepoDrift test target joined the READ and CODE_READS entries): both were ported into the split modules, and `source_reads` calls the one lexer.

**PB5432, the gate's cases.** `gate.py` lost 1,430 lines: 123 expectations became 31 cases in `scripts/selftests/gate/`, and the
planted host `FakeHost` an ordinary module `scripts/gate_fake_host.py` that `arch/perf_baseline.py` also imports. The RepoDrift
population main added meanwhile was ported into the cases and the planted host.

**PB5434, the landing lane's cases.** `land_lane.py`'s self-test was one `try` body of 602 statements over one shared scope. A
reaching-definitions walk found the five groups of cases that share no state and made each a file (`test_tick`, `test_detached_runner`,
`test_stranded_branches`, `test_queue_watch`, `test_conflict_brief`), plus `test_source_shape` and `fixtures`; the file went from
3,038 lines to 1,750. The 97 labels are the same set before and after. The conversion found one accident (the conflict-brief case read a
list the real-launch case filled) and one trap (the stopped-clock case rebound the subject's `now_utc` through `globals()`, which in a
case file rebinds the case file's). The note and the design called the detached runner's world Windows-only; the old self-test carried
no platform header and ran on Linux, so its file carries none either (section 16.2 says so). `landing_rules.DECLARED` now names land_lane's
arms as case functions.

**PB5433, the gate's module split.** `gate.py` (2,399 lines) became the driver plus `gate_audits.py`, `gate_verdict.py`, `gate_host.py` and
`gate_runs.py`, cut by an AST mover from the design's table, with every importer re-pointed and no old spelling left. The split decided
what the table did not: `LOCK_NAME` and `Spawn` are `gate_host`'s (as the run store's they made `gate_host` and `gate_runs` import each
other), and the layering is `gate_audits` < (`gate_verdict`, `gate_host`) < `gate_runs`, declared in `script_layering.py`. The mover masked
one import behind a method of the same name (`toolchain` in `Host.toolchain`); the first gate ended NOT RUN [driver] on it, and a new
real-host case (`scripts/selftests/gate/test_host.py`) now fails on any such name, which no planted-host case could.

**PB5411, the `Gate` class split.** `Gate` keeps the run and holds `PassCarry` (`gate_reuse.py`: reuse, carry, record, re-derive),
`Generations` (`gate_generated.py`) and `Legs` (`gate_legs.py`: plan, listings, first classes, hosts, population check), each returning
what it found and never calling back; a verdict a collaborator reaches is a value (`Ending`, `GenerationFailed`) that `Gate._finish`
writes, and `Outcome` and `HostRun` moved to `gate_verdict` beneath all three.

**Proof.** Gate `000003-20261114T135439Z` at `cf4af90432`: `=== BUILD-LOCAL GATE: GREEN — Conformance 24,880/24,880 ran · Unit 40,728/40,728 ran
· RepoDrift 177/177 ran · Characterization 37/37 ran (skipped 0)`; `python scripts/self_tests.py` GREEN over 196 self-tests; `script_layering.py`,
`inert_lane.py check`, `work.py check` and the DOC AUDITS GREEN. PB5429, PB5430 and PB5431 (the CONFORMANCE.md steps B1, B2 and C) are not
started and stay open for the next link of the chain. No compiler behavior changed.

### PB3726 step B1: docs/CONFORMANCE.md made regular in place, and the first commit of step B2's citation burn-down (PB5429, PB5430)

**PB5429 (step B1 of the conformance split).** The re-probe was the survey: `conformance_source.py --check`, written
first, found 60 departures on the document as it stood: 50 determinations of section 3 with no `D-<KEY>`, sections 3,
3.1, 3.2, 7 and 8 out of key order, prose after the tables of 3.2 and 7, and a section 4 whose 27 blocks had no key at
all. The design's survey had drifted (section 3.11 exists, 3.3 to 3.11 carry tables, sections 2 and 5 have nothing after
their tables), so the as-built shape is recorded in DESIGN-orchestrator-loop.md section 16.8. The reader now knows the
document's one shape: `KINDS` (BULLETS 3 and 3.1, TABLE 2, 3.2, 5, 7 and 8, BLOCKS 4, PROSE the rest), `shape(n)`,
`determinations()`, `natural_key`, `render_section`/`render` (the one layout, which step B2 renders through) and `check`,
a DOC AUDITS check. A one-off script over that parse keyed the 50 (`kb/Work/PB5429/2026-11-14-keys.md`), gave section 4
`#### <key>` blocks and an epilogue, made the 3.2 and 7 tails row bodies and an epilogue, sorted every keyed section and
rendered the whole. Oracle: the non-blank line multiset differs only on the 50 keyed lines, 29 `#### ` headings and 2
epilogue markers, plus one citation repaired on purpose (row DOC-A.1-169, now `§2023:12.4.6.4.4 GR4b`, `cite.py --check`
OK); `audit_derivations`, `differential_fuzz`'s declined sets and `--write-register` gave identical output, `audit_annex_a1
--json` and the rendered ledger equal line multisets, and the extract's two row lists equal sets, now written in key order.

Two audits had leaned on the old order, and both were fixed at the root: `audit_annex_a1.py` counted a row body's
`#### DOC-A.1-56` heading as a prose mention, and the CITATIONS audit read the row above a table row as its context, so a
row whose prose names a wrong citation excused the next row's refused citation (`context_of`; with it the baseline is
unchanged). Planted cases: `scripts/selftests/spec/conformance_source/test_shape.py`, one finding per departure.

**PB5430, commit 1 (the burn-down).** Over the CITATIONS audit's files, 42 `CONFORMANCE.md §N`/`#ANCHOR` citations did not
resolve. Eleven, each the standard's clause number 4.2.16 written as a section of the document, now name what they meant
(`DOC-A.1-123` for the Int128 intermediate, the runtime message in `CobolNum.cs` included; `DOC-A.1-12`, `-135`, `-193`,
`-213`); seven planted bad anchors carry the example marker. The rest (16 templates such as `#DOC-A.1-<n>`, 6 in kb/Work
note bodies) wait for the arm, which steps B2 and C continue on this branch.

**PB5430, commit 2 (the cut).** The statement's storage is now `docs/conformance/`: one directory per section and one
file per entry named by its key (514 files), JSON-valued frontmatter for the index (`heading:`, `columns:`), a row
(`cells:`) and a section-4 block (`order:`); a determination is its bullet as written. `conformance_source.load` renders
the sources and parses the render, so no reader changed; `conformance_doc.py` renders and checks. The migration cut the
step-B1 monolith from HEAD and the render was byte-identical (1,363,844 bytes); the script was then deleted.
`docs/CONFORMANCE.md` is an ignored view whose first line `rendered_views.py` now stamps with its body's digest, and
rule (e) refuses an edited or unstamped view and a render over one. The CITATIONS baseline was re-keyed (40 entries, the
same citations and kinds), and an A.3 range key is stored as an ASCII file name. Four rows the cut's renderer could not
write back (A.4.8 and A.4.11 with unescaped grammar pipes, DOC-A.1-31 and DOC-A.1-95 with a stray `||`) were repaired
and `check` now refuses both shapes. Left for the next link: the pointer sweep, the conformance arm and PB4053 (commit 3)
and the published statement (PB5431).

Gate: run 000005-20261114T161341Z at bcbef55d93, `=== BUILD-LOCAL GATE: GREEN — Conformance up to date (24,899 cases) · Unit up to date (40,743 cases) · RepoDrift 177/177 ran · Characterization up to date (37 cases) ===`; reds on the way (standard_streams rule 2, EVIDENCE SUPERSESSION on the view path, RepoDrift document code-locations) each fixed at its root with a planted case. LINUX GATE: not part of the per-change gate (PB4327).

### The conformance statement's pointers, its citation arm, and the statement in every package (PB5430 commit 3, PB5431)

Wave 20262031 continued the PB3726 group from wave 20262030's split: its 25 unlanded commits cherry-picked onto main
8b854e403f with no conflict (the merge of an already-landed branch left out), and the gate GREEN at that head before new
work (run 000001-20261114T162439Z: Conformance 24,966/24,966 · Unit 40,766/40,766 · RepoDrift 177/177 ·
Characterization 37/37 ran).

**PB5430, commit 3: the pointers and the arm.** Every instruction that names a place to write a determination now names
`docs/conformance/` (CLAUDE.md rule 1, the `spec-lookup` skill, the implementer dispatch spec's DOC-A.1 rule, the golden
lane and cloud adjudication briefs, the issue form, DOC_INDEX, the plan's publication criterion, DESIGN-test-build-ci
section 3.17.4, which now also states the views' stamp and rule (e)); README's links name the directory, and the session
probe renders the view. The CITATIONS audit gained its `CONFORMANCE` arm: every `CONFORMANCE.md §N` names a section
directory and every `CONFORMANCE.md#<key>` an entry key (`Conformance.keys`). Before it turned on, 23 citations did not
resolve: 16 were templates (`#DOC-A.1-<n>`) and 7 sat in kb/Work note bodies; the arm reads neither (a template is matched
possessively so its prefix is not read as a key; a filed note's prose is its owner's history, CLAUDE.md rule 8), and it
is GREEN with no baseline. Its self-test fires on the three misattribution shapes the burn-down rewrote and stays silent
on a real section, a real key, a template and a marked example. PB4053 (row-key partition of a keyed table) is retired:
with one file per row, `inflight.collision` finds no meeting between notes on different row files and still finds one on
the same row.

**PB5431: the published statement.** `Directory.Build.props` gains `CobolPackConformanceStatement`, joined to NuGet's
`TargetsForTfmSpecificContentInPackage`: every package renders the statement at pack time and carries it at
`docs/CONFORMANCE.md`, the path 62 compiler and CLI strings name. `dotnet pack` of the CLI and of the runtime: the packed
file's sha256 equals the checkout view's stamped body digest; a planted malformed entry fails the pack (MSB3073). The
manual has no build, so the half of the step that renders the statement beside it had nothing to join; the design
records the correction, and the manual's index says where the statement is.

Self-review: one renderer (the pack runs `conformance_doc.py`, the view's own generator); one reader of keys
(`Conformance.keys`); no second population for the arm (PHANTOM's loop). Found on the way and not fixed (outside the
group): the CLI packs as `cobol`, not the `WiseOwl.COBOL` tool package CLAUDE.md names (no `PackageId`/`PackAsTool`).

Gate: run 000002-20261114T164614Z at db6e01c2a9, `=== BUILD-LOCAL GATE: GREEN — Conformance 24,966/24,966 ran · Unit 40,766/40,766 ran · RepoDrift 177/177 ran · Characterization 37/37 ran (skipped 0) ===`. LINUX GATE: not part of the per-change gate (PB4327).

Branch review round 1 (two blocking findings), answered at the root. R1-1: the C# inventory drift test now skips a
document's location before its `File.Exists` (the conformance statement is a rendered view a fresh checkout lacks), but
`docchecks/inventory.py` judged only anchored document locations, so a BARE one (`DOC-A.1-75`'s `docs/FILES_DESIGN.md`)
was checked by nothing. The audit now judges every document location, a bare one for its file; probe: with
`docs/FILES_DESIGN.md` moved aside, `doc_audits.py --check` printed `DOC-A.1-75: code-location 'docs/FILES_DESIGN.md' — no
such file` and RED, and two self-test arms pin it. R1-2: rule (e) held every in-tree file view to a stamp only
`render_all` wrote, so running a generator directly (`drift_rules.py --out docs/DRIFT_RULES.md`, `gen-diagnostics-doc.ps1`)
left an unstamped view the gate called a hand edit. The stamp now has one writer, `rendered_views.write_view`: the Python
generators' `--out` reaches it through `write_rendered` (stamped at a view's path, the body alone elsewhere, so the
package's copy stays plain), the diagnostics test writes its raw render to the file `WISEOWL_COBOL_WRITE_DIAGNOSTICS_DOC`
names and the script places it with `rendered_views.py diagnostics --write`, and `render_all` fails a generator that exits
0 without a stamped view. Probe: the three generators run directly, then `rendered_views.py --check`: `rendered views OK —
7 views … none edited after its render`; the self-test grew four arms (20/20). The first fix gate (run
000001-20261114T171525Z) was RED at the INERT LANE audit: the C# test no longer names `docs/DIAGNOSTICS.md`, so its Markdown
allowance in `source_reads.py` was dead and went. Gate: run 000002-20261114T171657Z at 5336079ba2, `=== BUILD-LOCAL GATE:
GREEN — Conformance up to date … · Unit 40,766/40,766 ran · RepoDrift 177/177 ran · Characterization up to date … ===`.

