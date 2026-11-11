## Finisher: spec-2002-reconcile-tools merged in (2026-11-11)

The merge of spec-2002-reconcile-tools (PB5255, fb2ecd1949) conflicted on `scripts/spec/merge_reconciliation.py`:
this branch had pointed its module-level `LEDGER` at `verify_publishable.RECONCILIATION[2023]` (review R1-4, so
`reconciled` and the merge share one ledger path); PB5255 replaced those module constants with its per-edition `EDITIONS`
table, which named its own ledger files (2023, 2002, 2014). Taking either side alone leaves two tables of ledger paths, and
`reconciled(2002)`/`reconciled(2014)` false forever, since `RECONCILIATION` named no 2002 or 2014 ledger.

Resolution: PB5255's file, with `EDITIONS` taking each row's ledger from `verify_publishable.RECONCILIATION`
(`ledger_of`, a KeyError for an edition it does not name), and `RECONCILIATION` naming 2002's and 2014's ledgers. The
dependency runs merge_reconciliation -> verify_publishable only: the reverse import cycles through reconcile_ranges ->
sweep_figures -> verify_publishable. Both self-tests PASS and `merge_reconciliation.py --edition 2023 --check` is CURRENT.
