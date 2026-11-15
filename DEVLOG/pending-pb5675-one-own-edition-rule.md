## PB5675: one own-edition evidence rule for every older edition; 1985 from 32 GAP to 835, the count becoming true

**The decision.** The owner, 2026-11-14: "2002 spec covers 2002 rules"; 2026-11-15: "the same argument for the 2002
rules applies equally to the 1985 rules." kb/Work PB5597 had put the own-edition rule behind a 2002-only schema switch,
`own-edition-evidence`, so a 1985 row recorded KEPT closed whenever its 2023 targets did. kb/Work PB5675 makes it one
rule with no edition key.

**Phase 1, measured on main 97f1e70b45 / 63ab0c4ece.** Of 1,373 stored 1985 KEPT records, 570 have a witness of the
kind the rule needs at 85 for every target, 232 for some and 571 for none. `own-edition` reported 90 of 2002's 704
records as having none, but only because it ignored the read records' licences: with them none fails, and LINEAGE
READS was green. The 1985 candidate pairing pairs 0 of 2,274 rules, so the writer's whole test refuses every new 1985
KEPT record. Two mechanisms were proposed (the evidence test inside the state derivation, about 25 s on every
older-edition view and a C# copy of the rule; or a witness of the row's own, checked by the audit). The Mythos lane
chose the second, with the stored records' witness batch in the same landing.

**The mechanism.** A chained row's state is its chain's AND a spec-derived `test-ref` of its own (`lineage.state_of`,
`lineage.chain_closed`, `lineage.unwitnessed`; the C# `StateAt` mirrors it). The state reads only the row, so no view
reads the suite and the probe still takes about 3 s. LINEAGE READS (`lineage_reads.py --check`, every older held
edition, one reading of the suite shared by all) holds each own witness to a case that runs at the row's edition of the
kind its rule needs (`Witnesses.own_problems`, which reads no target): a narrowed or moved witness is red by row and
case. WITNESS LOSS compares every held edition. The writer (`change-batch`) keeps the whole test of a new KEPT record at
every edition (`kept_witnesses`: pairing, then the candidate search `Witnesses.search` through the targets) and writes
the cases found as the row's test-ref. Only the writer reads `to`, to find candidate cases. `own-edition` is gone;
`witness-batch` is the one candidate search over stored records. `Edition.own_evidence` and the switch are deleted, and
the INVENTORY VIEW audit names any `editions.of` key outside `EDITION_KEYS`. A KEPT row's own witness is a
`conformance:` case (`lineage.next_errors`). `inventory_view.gap_causes` splits each edition's GAP (inherited without an
own-edition witness, never adjudicated, open) in the probe's `invent` line, `--summary` and the ledger.

**The batch.** `witness-batch` then `record_verdicts.py --edition`: 2002, 704 witness-only records (+928 test-refs);
1985, 570 (+785). 1,274 store files gain one line each and lose none; a second run writes no record.

**The figures** (`inventory_view.py --summary`). 2023 0 GAP and 2002 unchanged by it (2,258 before and after at main 63ab0c4ece; 2,172 before and after once merged with main d8b7ad040e). 1985 from 32 GAP to 835
(803 inherited without an own-edition witness, 14 never adjudicated, 18 open). This is the count becoming true, not a
regression. The 570 that stay closed rest on the old comparison audit plus a witness at 85; their read in 1985's own
terms is still owed, so 835 is a lower bound. 1985 is the earliest edition: its rows are recorded with their own
verdicts, never as new KEPT records.

**Tests, each failing on the old code.** The `lineage.py` self-test checks that a KEPT row with no own witness is GAP and
counted as unwitnessed, and that a unit test-ref on a KEPT row is refused. It also covers a synthetic 2027 that makes
2023 an older edition with the rule by construction, and an edition entry carrying an evidence switch, named by
`edition_key_errors`. `test_reads.py` cases 05 and 05b cover the audit at 2002 and 1985 alike: a lost or moved witness
is red by name, a row with no witness is no error, the stored pairing is not asked, and the writer refuses at 1985.
`test_audit_batch.py` checks the written test-ref and the change-batch refusal at 1985. `audit_witness_loss.py
--self-test` loses an older row's own witness and the audit goes red. `gen_ledger.py` names the causes.
