## Entry 3657 — 2026-11-14 23:06 PST — PB5589: the lineage screen flags the 2002 pairs whose identical words govern more in 2002; one change-record writer, from read decisions only; PB5094 claims resolve in every edition

**What landed.** `scripts/spec/lineage_screen.py` flags the 2002 candidate pairs whose identical words are known to govern
more in 2002 than they show, by a table of triggers (`scripts/spec/data/lineage-screen-triggers-2002.json`), each a row
naming its mechanism, parameters, source and reason. The pending-clause diff compares each of 2002's 89 pending clauses
with its counterpart and flags the pairs that rest on a changed or removed one; a clause the table does not yet judge
flags every pair under it and naming it. The other triggers read the terms §2002:4 defines (by sense), the compiler's
dialect behaviours (the compiler facts now carry `changedIn`), cross-references, the records a 2002 CD entry holds, the
obsolete and archaic elements of §2002:G, `band-formats`, deferral and identity hygiene. The screen decides nothing: every
pair is read. `audit_batch.py change-batch --edition` is one writer for every older edition and writes only an audit run's
decisions; a record for a flagged pair answers each flag (`next.answers`) or is refused, by the writer and by the new
LINEAGE SCREEN audit (gate and CI). Two blind samples are its regression fixture. `work.py check` resolves `inventory_rows`
and `closes_rows` against every held edition (PB5094, first half).

**Measured.** The screen flags 1,220 of 1,770 pairs, all 16 DIFFERENT pairs of the first sample and 3 of the 16 the second
sample found among pairs the earlier triggers left unflagged. The clerk's plan-2002 notes (PB5574-PB5586) had 221
unresolved 2002 claims under the old register check and none under the new one.

**Not done.** No 2002 record is written. 11 pending-clause judgments are owed and fail closed meanwhile. Coverage of the
older editions' defective rows waits on PB5094's second half (38 1985 rows).

