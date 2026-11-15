## 2026-11-15: the mechanism chosen and built (wave 20262047 group A), and a correction

**A correction of the note's body.** The note says the read of the inherited 1985 rows in 1985's own terms "is what
makes widening and KEPT possible". For KEPT it is not: the writer's whole test of a new KEPT record includes the
candidate pairing (word-identical text), and `docs/rearchitecture/spec-lineage-candidates-1985.json` pairs 0 of 1985's
2,274 rules, so every new 1985 KEPT record is refused (`targets are not the pairing's`). That is intended (the Mythos
lane, 2026-11-15): 1985 is the earliest edition and inherits from nothing, and its rows are recorded with their own
verdicts, each with a test-ref that runs at 85, never as new KEPT records. The read still feeds widening and the
own-verdict records.

**The mechanism (B of the phase-1 proposal).** A chained (KEPT) row of every older edition closes only on its chain and
a spec-derived `test-ref` of its OWN (`lineage.state_of`; the C# twin `SpecTraceabilityInventoryDriftTests.StateAt`).
The LINEAGE READS audit checks, at every older held edition, that each such test-ref names a suite case that runs at the
row's edition of the kind its rule needs (`lineage_reads.Witnesses.own_problems`, which reads no target): a narrowed or
moved witness is red by row and case. WITNESS LOSS compares every held edition. The writer (`audit_batch.py
change-batch`) keeps the whole test of a new KEPT record at every edition (`lineage_reads.kept_witnesses`) and writes
the cases it finds as the row's test-ref. `lineage_reads.py own-edition` is deleted; `witness-batch` is the one candidate
search over stored records, and the views say the rest. An `editions.of` entry may carry only
`inventory_schema.EDITION_KEYS`.

**The witness batch** (`lineage_reads.py witness-batch`, applied by `record_verdicts.py --edition`): 2002, 704 records
(+928 test-refs); 1985, 570 records (+785). Each adds one field to a store file and nothing else (1,274 files, one line
added each); a second run writes nothing.

**The figures** (`inventory_view.py --summary`): 2023 0 GAP and 2002 unchanged by it (2,258 before and after at main 63ab0c4ece; 2,172 at main d8b7ad040e); 1985 from 32 GAP to
835 (803 inherited without an own-edition witness, 14 never adjudicated, 18 open). This is the count becoming true, not
a regression. The 570 that stay closed rest on the old comparison audit plus a witness at 85; their read in 1985's own
terms is still owed, so 835 is a lower bound.
