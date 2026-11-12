## Branch review round 1 (review w20261997-g at 5892a87be3): eight findings, seven fixed here, R1-1 held

**Sites:** `scripts/spec/lineage.py` (`pairing_text`, `candidate_key`, `target_digests`, `stale_targets`, `next_errors`,
`main`, `self_test`), `scripts/spec/record_verdicts.py` (`main`), `scripts/spec/inventory_schema.py` (`link_errors`,
`Edition.outside`), `scripts/spec/inventory_view.py` (`check`, `summary`, `lower_bound`), `scripts/spec/verify_publishable.py`
(`RECONCILIATION`, `ledger_pages`, `reconciled`, `self_test`), `scripts/spec/spec_pdf.py` (`Edition`, `EDITIONS`),
`scripts/spec/merge_reconciliation.py` (`LEDGER`), `scripts/spec/extract_rule_catalog.py` (`IsoEdition.reconciled`,
`catalog_document`, `OUTSIDE`, `main`), `scripts/spec/audit_catalog_coverage.py` (`numbered_heading`, `clause_bodies`,
`main`, `audit`, `STRUCTURAL_HEADING`), `scripts/spec/gen_ledger.py` (`measure_inventory`, `editions_text`, `self_test`),
`scripts/spec/cite.py` (`norm`, `self_test`), `scripts/spec/test_audit_batch.py`, `scripts/gate.py` (`AUDITS`),
`.github/workflows/build-and-test.yml` (the audits job), `tests/version-matrix/inventory-schema.json` (`next.keys`,
`editions` comment), `tests/version-matrix/inventory-1985/` (whole: 1,937 records gain `next.digests`),
`docs/rearchitecture/spec-lineage-candidates-1985.json` (new), `docs/CONFORMANCE.md`.

- **R1-1 (build_targets.json, the 2002 catalog an undeclared Conformance input): HELD** on the coordinator's word until
  kb/Work PB5231 lands its generated `conformance-suite` target; then this branch merges main and declares the catalog there.
- **R1-2 (no gate or CI audit for 2002).** RULE CATALOG and RULE CATALOG 1985 became one RULE CATALOGS audit,
  `extract_rule_catalog.py --held --check`, which runs every edition `inventory_schema.HELD` names; LINEAGE CANDIDATES,
  `lineage.py --candidates --check`, regenerates every older held edition's pairing and fails when one is stale (1985's
  pairing is now committed too: none of its 2,274 rules is word for word a 2023 rule). CI's audits job runs the same three
  commands, so `ci_audits_not_in_gate` holds. A newly held edition needs no audit line.
- **R1-3 (normalized pairs that differ in meaning).** `lineage.pairing_text` erases typography only: a Markdown escape,
  typographic quotes and dashes, the ellipsis glyph (read as `...`), a list item's delimiter, white space; never a bracket,
  brace, bar, ellipsis, underline, word, case or number (self-test arms for each). Re-derived 2002 to 2023: 1,758 paired
  (1,732 exact, 26 normalized, 2 with two candidates), 1,532 unpaired; 197 of the 223 normalized pairs fell out, among them
  2002-FMT-8.3.1.2.1.1 and 2002-FMT-8.3.1.2.3.1. The 26 left differ by en dashes (19), escapes (8), apostrophes (5) and
  ellipsis glyphs (5) only.
- **R1-4 (a change record's target can move under its id).** (a) Linking refused before reconciliation:
  `verify_publishable.reconciled(edition)` is the fact (the edition's reconciliation ledger records every page of its PDF,
  `spec_pdf.EDITIONS[...].pages`, measured: 976, 882, 1261; 1985 and 2023 are reconciled, 2002 is not), and
  `inventory_schema.link_errors` refuses a linked unreconciled edition in the EDITION GATES audit (moved there from INVENTORY VIEW at the merge of PB5231: the view is in the suite generator's import closure). The extractor's
  `IsoEdition.reconciled` reads the same fact (the hand flag is deleted). (b) A change record carries `next.digests`, each
  `to` rule's `rule-digest` when it was decided, stamped by `record_verdicts.py`; `lineage.stale_targets` names a record
  whose target's text moved, STALE, and the audit fails on it. 1985's 1,937 records were stamped through
  `record_verdicts.py --edition 1985` against the 2023 texts, which have not changed since before the first 1985 record
  (the catalog's last text change is 2026-11-08 02:48; the 1985 store's first commit 10:41): every changed file equals its
  committed self with `digests` removed (verified, 1,937 of 1,937). The reviewer's plant in a scratch worktree (a typo in
  READ SR2, a rule inserted as GR3) moves the text under 27 ids (2002-GR-14.8.29.3-3 to -29, and adds -30): 27 records
  into them are named STALE, every moved id and no other; with 2002 set linked, `link_errors` refuses the schema.
- **R1-5 (the 2002 count printed with no sign it is a lower bound).** The catalog records `outside-denominator` (the 92
  pending clauses, the 4 untyped headings); `inventory_view.lower_bound` prints it beside the count on the probe's
  `invent` line and in the ledger (`editions_text`).
- **R1-6 (`cite.norm` deleted `<for classes>`).** It strips `<u>` and `</u>` only; both cases are self-test arms, and
  `cite.py --check "§2023:3.103" "for classes mechanism for using the interface"` passes again.
- **R1-7 (the coverage critic read 2023 only).** `audit_catalog_coverage.py --edition`/`--held`: each ISO edition's
  furniture, page markers, rule-bearing annex and part headings; a difference a recorded finding explains is named, not
  failed. Now a gate and CI audit, CATALOG COVERAGE (2002 and 2023 GREEN; 2002's two differences are its two findings).
- **R1-8 (CONFORMANCE.md said no 1985 or 2002 text is held).** Both sentences now say when the determination was made,
  where the held editions are listed (`spec_pdf.EDITIONS`), and that each determination is still to be read against them
  (the 1985 text's own I-O status 07 names only CLOSE and OPEN, §1985:VII.1.3.5). Those two re-reads are open work that
  needs a note of its own (an id from the coordinator).

**R1-1, done after PB5231 landed (merged main at d9dde0e7ba).** The `conformance-suite` target declares
`docs/rearchitecture/spec-rule-catalog-2002.json` (the inventory view of 2002, which generate.py joins with every held
edition), its `tests/version-matrix/` entry and the `read_store` judgement name `inventory-2002/`, and the probe
`lineage.py|main|path.exists()` is judged (the CLI's committed pairing, never reached by the generator). A traced
generation (`buildgraph.traced_run` judged by `buildgraph.undeclared`) read the 2002 catalog undeclared before the entry
and reports no undeclared read after it; it reads no other file this branch added (no 2002 store file exists, no pairing
file). The link check moved from INVENTORY VIEW to EDITION GATES (`edition_gates.check`): the view is in the generator's
import closure and the reconciliation ledgers' reader (`verify_publishable`, with `citation`, `reserved_word_order`,
`spec_pdf`) is not, and BUILD TARGETS refused the probes that import brought into the closure.
