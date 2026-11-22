## Entry 3923 — 2026-11-22 13:35 PST — The inventory schema's verdicts and separators have one reader (PB3206, PB3210)

Two R2 duplication findings of the PB1754 review, both about scripts that kept their own copy of what
`tests/version-matrix/inventory-schema.json` owns.

**PB3206.** The re-probe showed the note's headline already fixed: commit 2e741ec75f derived `gen_ledger.py`'s RESOLVING
and DEFECTIVE from the schema's `resolves` flags after a NEEDS-OWNER-DECISION row broke the hero band, and PB5994 later
rebuilt the ledger on `edition_status.py`, deleting both sets and `measure_inventory`. What remained was the note's other
named site: `backfill_closes_rows.resolving_verdicts()` parsed the schema JSON a second time to re-derive the resolving set.
It is deleted with its `SCHEMA` constant; `main` reads `Schema.resolving` from the one loader. The script's self-test passes
24/24 and its report mode prints the same counts as before.

**PB3210.** The form table (`KNOWN_FORMS`, `PROJECT_FORM`, `COB_REF`) had already gone in PB3177 and PB5230. What remained
was `normalize`'s literal `";"` split and `"; "` join of `test-ref` and `code-location`; both now use
`Schema.test_ref_sep` and `Schema.code_location_sep`. A new case file,
`scripts/selftests/spec/normalize_batch/test_normalize_batch.py`, plants a schema with an extra xUnit form, an extra file
form and changed separators, and checks each is carried through; its separator case fails on the old code.

`DESIGN-architecture-review.md` §8.2 gains rule 7: a schema-owned vocabulary has no hand-kept copy in `scripts/`.
The sibling sweep found every other writer of the two lists already joining by the schema's separator.

Self-review: architecture (one schema reader), duplication (no verdict set or separator literal outside fixtures), the
selftest layout rule (the runner's `check_repo` passed). No inventory row changes: `closes_rows: []` on both notes.

