## 2002 reconciliation, the account1 half: 438 pages compared, 42 discrepancies confirmed

The seven runs of the `account1` half of the 2002 reconciliation plan (`2002/ranges.json`, account1-r01 to r07) ran in
parallel on account 2 through `.claude/workflows/spec-reconcile.js` on Opus at effort medium (workflows wf_7f0d042a-811,
wf_a8b6e7f4-a39, wf_ce8f4b9e-119, wf_7bb66444-124, wf_41ed85e1-e1b, wf_a3522b70-8b7 and wf_a1a4a016-59d; 97 agents,
8.69M subagent tokens). All 438 pages were read and every claim got its own adversarial verdict: 72 claimed, 42
confirmed (8 normative, 31 structural, 3 cosmetic), 30 refuted. Among the normative ones: Table 2's character column is
shifted one row against its meanings (page 83); Table 18 merges the numeric-edited usage-display and usage-national
sub-rows of the MOVE table, losing the National category (page 503); the RECORD clause format lets DEPENDING ON stand
as its own alternative (page 259); the A.1.1 COMMUNICATION SECTION format lost its brackets (page 677); and three more
operand subscripts are flattened into other operand names (pages 562 and 653). One agent file was written CRLF and
rewritten LF before commit (kb/Work PB5330). With the account2 half this sweeps every page of 2002; the ledger is
written when the round closes (README step 5).
