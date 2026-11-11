## 2002 reconciliation, account2-r05 to r07: 156 pages compared, 17 discrepancies confirmed

Runs account2-r05, r06 and r07, the last three of the `account2` half of the 2002 reconciliation plan
(`2002/ranges.json`), ran in parallel through `.claude/workflows/spec-reconcile.js` on Opus at effort medium (workflows
wf_9827e5f2-747, wf_05215061-45b and wf_dd388b0c-96e; 34 agents, 2.72M subagent tokens). All 156 pages were read and
every claim got its own adversarial verdict: 21 claimed, 17 confirmed (1 normative, 16 structural), 4 refuted. The
normative one is §2002:14.8.43.1 (UNLOCK, PDF page 566), whose general format stacks `UNLOCK` over `file-name-1` so the
two read as alternatives. Fifteen of the structural ones are the index (pages 865-876), whose entries ran together and
lost their sub-entry nesting. With account2-r01 (#1522) and r02-r04 this closes the account2 half; the ledger is written
when the round closes (README step 5).
