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
