## 2014 reconciliation, the account1 half: 456 pages compared, 31 discrepancies confirmed

The seven runs of the `account1` half of `2014/ranges.json` (account1-r01 to r07) ran on account 2 in parallel through
`.claude/workflows/spec-reconcile.js` on Opus at effort medium (workflows wf_ad1f93b6-6ee, wf_bd8e8184-6d2,
wf_e2964404-bb0, wf_c2165839-ad1, wf_07ab161b-71e, wf_cd90b432-be6 and wf_88c76f7a-3d3; 102 agents, 9.06M subagent
tokens). All 456 pages were read and every claim got its own adversarial verdict: 64 claimed, 31 confirmed (6
normative, 22 structural, 3 cosmetic), 33 refuted. The normative ones are flattened operand subscripts in five more
rules (§2014:14.9.40.3 and the intrinsic functions at 15.56.3, 15.81.3 and 15.91.3) and the reserved-word list on page
192, where `FLOAT-NOT-A-NUMBER-SIGNALING` was split into a malformed word and a stray `SIGNALING`. With the account2 half
this sweeps every page of 2014; the ledger is written when the round closes (README step 5).
