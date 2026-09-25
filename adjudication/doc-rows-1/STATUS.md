# doc-rows-1 — PB1522 step 3 integrator status

- DONE step 1: merged.jsonl (126 items: WRITE 62 · OMIT 45 · SKIP 13 · NOT-PROVIDED 5 · KEEP 1) from the six batches
  (`merge.py`, integrator re-checks in `integrator-checks.json`).
- DONE step 2: 67 rows written into docs/CONFORMANCE.md §7 (65 appended in item order, 72 and 128 replaced in place);
  audit_annex_a1.py: every row ok, 125 of 184 discharged, 59 remain; preamble numbers updated; §2/§3 prose that
  restated or contradicted a filed row repointed (compile-time arithmetic → 29, FLAG-02/14 → 79/80, leap second →
  111, error termination → 192/193, .NET host entry → 65, §2 row 27 → 186, item 62's dangling item-60 reference);
  FILES_DESIGN + pb673 comment follow the corrected row 72; COBOLNET0825 cites §14.9.23.3 SR4 a)/b).
- DONE step 3: batch-doc-v2.json (68 records: CONFORMS 51 · PARTIAL 12 · DOCUMENTED-NON-SUPPORT 5 via the
  a1-optional-not-provided selector) recorded — GAP 1652 → 1627 (`record_verdicts.out`). Row 187's PB-new → PB1542.
- DONE step 4: new defects PB1537–PB1545; 19 notes extended; PB1530 landed; PB1534 31/41 discharged (open on 10 owned
  items); PB1522 claims 59 (58 OMIT/SKIP + 201) and carries the step-3 result.
- DONE step 5: gate green (build 0/0; filtered unit gate 80/80 after moving DerivedVerdictDriftTests' undetermined-optional
  anchor from item 7 to 67; ClosesRowsBackLink 5/5; work.py check; gen_conformance_notes regenerated; witness loss GREEN).
- DONE step 6: adjudication/registrar-1/LANDING.md "Step 3 (doc-rows-1)". NEXT: owner's local landing via scripts/push-main.sh.
