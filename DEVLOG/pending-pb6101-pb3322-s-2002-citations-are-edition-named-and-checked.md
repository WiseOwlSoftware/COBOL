## PB6101: PB3322's 2002 citations are edition-named and checked

PB3322's refuter pass wrote its 2002 and 2014 locators as prose ("11.8.5 ... general rule 2", "Annex A item 44", "2002 8.1.1"). Re-probed each with
`cite.py --check` on this tree:

- DOC-A.1-64: 11.8.5 has no general rules (FAIL no-rule-list); the rule is `§2002:11.8.5.3 GR2` and the default paragraph sits in the same subclause. The item
  list is not "Annex A" either: 2002 Annex A is the communications facility, the implementor-defined list is `§2002:B.1` (item 44 checks OK there).
- `§2002:8.1.1` and `§2002:7.2.14` are right clauses (OK against their prose); DOC-A.1-112's "Annex A item 87" is `§2002:B.1 item 87`; DOC-A.1-188's items 151 and
  "general rules 7f and 7h" are `§2002:B.1 item 151` and `§2002:12.2.6.3 GR7f/GR7g/GR7h` (UCS-4/UTF-8/UTF-16); DOC-A.1-202's is `§2014:A.1 item 184`.

Fix: DOC-A.1-64's notes re-recorded through `record_verdicts.py` (batch `pb6101-cite-2002`; 1 row changed, GAP 2 -> 2, verdict and editions unchanged);
`kb/Work/PB3322.md` and `adjudication/pb3322-refute-pb3173/overturn-batch.json` carry the corrected forms. No compiled program changes; no golden applies
(process-only citation text). `work.py check` and `audit_annex_a1.py --check` are clean; PB6101 is landed with `closes_rows: []` (no row's verdict moved).
