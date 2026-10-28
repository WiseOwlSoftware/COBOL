## Entry 2697 — 2026-10-28 11:46 PDT — PB3808: audit_witness_loss owed_statements counts a stranded row as owed

**Mechanism.** `scripts/spec/audit_witness_loss.py` `owed_statements` stranded a row (its erased notes argued a verdict
the row no longer carries) by popping `notes` from `owed[rid]` and leaving the emptied dict. `run_history` then printed
`rows OWED a statement back: 1 (notes 0, derivation 0)` and `restore_records` emitted a vacuous
`{rule-id, verdict, editions}` record.

**Re-probe.** On this build, `Erasure(c1, notes, CONFORMS)` then `Erasure(c2, notes, PARTIAL)` on a CONFORMS row with
empty notes gave `owed {'R': {}} stranded {'R': 'PARTIAL'}` and one restore record. Live `--history` carries no such row.

**Fix.** One rule, no clean-up pass: `owed_statements` collects the latest erasure per (rule-id, field) (the nearest to
the present wins; vanished rows and rows that say something again are skipped), then partitions that once into `owed`
and `stranded`. The `stranded.pop` / `owed.pop` bookkeeping is gone. After the fix the probe gives `owed {}`, `stranded
{'R': 'PARTIAL'}`, no records. Sibling arms: `owed_back` (witnesses) builds `owed` with `setdefault` only on an
actual add, so it never leaves an empty entry; `restore_records` and `run_history` read `owed_statements` and needed no
change.

**Test.** `audit_witness_loss.py --self-test` gained the two-erasure arm (GREEN). No ISO rule governs this tooling, so no
conformance golden; no inventory row is claimed or closed (`closes_rows: []`).

