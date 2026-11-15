## PB5589: the 2002 rule stage's screen (pending-clause diff, triggers as data) and one KEPT writer; PB5094 claims resolve in every edition

**What landed.** `scripts/spec/lineage_screen.py` screens an older edition's candidate pairs by a table of triggers
(`scripts/spec/data/rule-stage-triggers-2002.json`), each a row naming its mechanism, parameters, source and reason. The
pending-clause diff (P0) compares each of 2002's 89 pending clauses with its 2023 counterpart and marks the pairs that rest
on a changed or removed one. The other triggers are the definitions changed or dropped (keyed by term and domain), the
dialect behaviours that changed after 2002 (read from the compiler facts, which now carry `changedIn`), cross-references,
communication reach, the removed-element vocabulary of §2002:G, `band-formats`, deferral, and identity hygiene.
Whole-clause identity is measured only. The K0 sample's 100 labels are a regression fixture: the new gate audit LINEAGE
SCREEN fails if a DIFFERENT pair is ever unmarked. `audit_batch.py change-batch --edition` is now one writer for every
older edition. The register check (`work.py check`) resolves `inventory_rows` and `closes_rows` against every held
edition's view (PB5094, first half).

**Measured.** P0 marks 540 of the 1,543 eligible pairs, the prototype's figure. T5 to T8 keep 524 of v2's 674, the v3
prototype's figure. The writer writes 461 KEPT and refuses 1,309; the 63 it refuses beyond the 524 are definitions read
sense by sense (59) and the whole-text 2014 check (4). The fixture's 16 DIFFERENT pairs are all marked. The clerk's
plan-2002 notes (PB5574-PB5586) had 221 unresolved 2002 claims under the old register check and none under the new one.

**Not done.** No 2002 KEPT record is stored. The governs judgments of 14 changed pending clauses wait on the
adjudicator's table. Coverage of the older editions' defective rows waits on PB5094's second half.
