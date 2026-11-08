## PB4995: the 1985 audit's disputed and own-row rules are adjudicated and refuted by batch, 2,104 change records recorded, and the drafts run at 85

Lineage slice 4 (kb/Work PB4995, DESIGN-spec-conformance-review.md §10.2 and §10.3). The 1985 audit left 564 DISPUTED
rules (a SAME the verify pass refuted or was unsure of) and 930 OWN-ROW rules (DIFFERS or NONE). The note's plan was the
cheapest path that keeps the strongest signal: batch passes decide, an independent batch pass refutes, the compiler
answers what a program can answer, and agents take only what is left.

**The passes.** `audit_batch.py` gains `adjudicate` (one request per DISPUTED or OWN-ROW rule: its map candidates, the
clause part as the map's cached block, and both readers' answers; KEPT, CHANGED or REMOVED with the 2023 rules and the
differing words, or OPEN) and `refute` (one per decided answer, with every candidate the adjudicator saw). The change
kinds are `lineage.NEXT_KINDS`, imported, not restated. `dispositions` is the one derivation of a rule's DECIDED change:
KEPT for CARRIES, an adjudication its refuter UPHELD; an OPEN, refuted or unsure one is `contested`. `lineage.kept_batch`
became `change_batch`, which records every decided change from `results.json` (the old command is gone, its PB4993
mentions updated). `write_plan` now lets a plan GROW by new items while keeping every planned one, so the drafts plan
takes the DISPUTED rules the adjudication made CHANGED or REMOVED.

**The runs** (canary first, then the rest under `--max-usd`): adjudicate 1,494 requests for $21.23 (KEPT 555, CHANGED 501,
REMOVED 315, OPEN 123); refute 1,371 for $17.73 (UPHELD 1,345, REFUTED 19, UNSURE 7). The run's map items predated
PB4998's candidate split, so they were migrated once in the scratch run, every candidate kept in order. Decided: KEPT
1,289, CHANGED 501, REMOVED 314; contested 149. `record_verdicts.py --edition 1985` wrote the 2,104 change records into
`tests/version-matrix/inventory-1985/`; the 1985 GAP went 2,253 to 1,087, the KEPT chains whose 2023 row is OK and placed
at 85.

**The drafts.** `run_drafts.py` (new) compiles and runs each draft at 85 through the conformance harness's `Run` and the
WiseOwl adapter, the one place a program is compiled, run and compared, and writes `drafts/runs.json` naming the
compiler's commit: 785 MATCH, 144 MISMATCH, 1 NO VERDICT (a run past the hang guard). A MATCH becomes a witness only
after the fleet checks the draft against the rule text.

**Left open (PB4995 stays open):** the 149 contested rules for `cobol-adjudicator` agents; 35 new drafts, whose submit
the key file refuses (the run bills `account1` and the one key's label does not name it); and each CHANGED or REMOVED
rule's own verdict and witness at 85. Self-review: the passes reuse the map's candidate shape and cache layer (no
second prompt builder), the change record's kinds have one home, the runner reuses the harness rather than a second
compile-and-compare, and each new arm has a planted self-test case (adjudicate and refute plans and answer shapes,
decided and contested results, the change batch, CARRIES as KEPT, a plan that grows, and the runner's five outcomes).
