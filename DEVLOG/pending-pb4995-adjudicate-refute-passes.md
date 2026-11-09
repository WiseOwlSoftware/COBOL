## PB4995: the 1985 audit's disputed and own-row rules are adjudicated and refuted by batch, 2,104 change records recorded, and the drafts run at 85

Lineage slice 4 (kb/Work PB4995, DESIGN-spec-conformance-review.md §10.2 and §10.3). The 1985 audit left 564 DISPUTED
rules (a SAME the verify pass refuted or was unsure of) and 930 OWN-ROW rules (DIFFERS or NONE). The note's plan was the
cheapest path that keeps the strongest signal: batch passes decide, an independent batch pass refutes, the compiler
answers what a program can answer, and agents take only what is left.

**The passes.** `audit_batch.py` gains `adjudicate` (one request per DISPUTED or OWN-ROW rule: its map candidates, the
clause part as the map's cached block, and both readers' answers; KEPT, CHANGED or REMOVED with the 2023 rules and the
differing words, or OPEN) and `refute` (one per decided answer, with every candidate the adjudicator saw). The change
kinds are the schema's `next.kinds` (`change_kinds`), never spelled in code: the adjudicate answer's verdicts, which
answers name rules and which state a reason, and which decided rules take a draft all read their flags, and a self-test
case fails if the prompt stops offering a kind the schema adds. `dispositions` is the one derivation of a rule's DECIDED
change: KEPT for CARRIES, an adjudication its refuter UPHELD; an OPEN, refuted or unsure one is `contested`. PB4993's
`audit_batch.py kept-batch` (which main had moved out of `lineage.py` while this branch ran) became `change-batch`
(`change_records`), the one writer of every decided change; no second copy is left in `lineage.py`. One stored form: a
record states its `reason` or omits the key, and `lineage.next_errors` refuses an empty one (PB4993's 759 KEPT records
carried none, this branch's carried `""`; the merge kept main's files and re-applied the batch in that form). `write_plan` now lets a plan GROW by new items while keeping every planned one, so the drafts plan
takes the DISPUTED rules the adjudication made CHANGED or REMOVED.

**The runs** (canary first, then the rest under `--max-usd`): adjudicate 1,494 requests for $21.23 (KEPT 555, CHANGED 501,
REMOVED 315, OPEN 123); refute 1,371 for $17.73 (UPHELD 1,345, REFUTED 19, UNSURE 7). The run's map items predated
PB4998's candidate split, so they were migrated once in the scratch run, every candidate kept in order. Decided: KEPT
1,289, CHANGED 501, REMOVED 314; contested 149. `record_verdicts.py --edition 1985` wrote the 2,104 change records into
`tests/version-matrix/inventory-1985/`; the 1985 GAP went 2,253 to 1,087, the KEPT chains whose 2023 row is OK and placed
at 85.

**The drafts.** `run_drafts.py` (new) compiles and runs each draft at 85 through the conformance harness's `Run` and the
WiseOwl adapter, the one judge of a program's outcome: an output draft is an accepted test, a refused draft a FLAGGED
test (`Run.flagged`), so a refusal counts only when the diagnostic is at the line the draft names as violating and its
conforming control compiles. The first count (817 MATCH) scored any nonzero exit as a refused MATCH, a second judge the
branch review caught (w20261931a R1-1): 20 of those were refused only for COBOL2199, a report-writer rule that holds at 85
too (§1985:XIII.3.15.3 SR5), so the drafts broke a rule other than their own. The drafts prompt now asks for the line and
the control; no stored draft has them, so a diagnosed refusal is REFUSED UNCONFIRMED, never a MATCH. `drafts/runs.json`
names the binary (its `--version` and the digest of its assemblies, R1-2), not the checkout's HEAD. All 965 drafts (the
35 grown drafts were collected after the parser fix, PR #1363) give MATCH 603, REFUSED UNCONFIRMED 219, MISMATCH 142, NO
VERDICT 1 (a run past the hang guard). A MATCH becomes a witness only after the fleet checks the draft against the rule
text. The change records' reasons quote the differing words as written (they carried Python repr quoting, R1-6); the
batch was regenerated and re-applied (809 store files, GAP unchanged).

**Left open (PB4995 stays open):** the 149 contested rules for `cobol-adjudicator` agents; a sample audit of the refute
pass (it upheld 98 %); the MISMATCH drafts to triage; and each CHANGED or REMOVED rule's own verdict and witness at 85. Self-review: the passes reuse the map's candidate shape and cache layer (no
second prompt builder), the change record's kinds have one home, the runner reuses the harness rather than a second
compile-and-compare, and each new arm has a planted self-test case (adjudicate and refute plans and answer shapes,
decided and contested results, the change batch, CARRIES as KEPT, a plan that grows, and the runner's outcomes through the harness).
