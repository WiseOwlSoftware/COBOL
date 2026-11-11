## 2002 reconciliation, account2-r02 to r04: 216 pages compared, 24 discrepancies confirmed

Runs account2-r02, r03 and r04 of the 2002 reconciliation plan (`2002/ranges.json`) ran in parallel through
`.claude/workflows/spec-reconcile.js` on Opus at effort medium (workflows wf_fb48f096-c2e, wf_9f9f0d15-6f9 and
wf_c0a525a5-4d0; 48 agents, 3.79M subagent tokens). All 216 pages were read and every claim got its own adversarial
verdict: 30 claimed, 24 confirmed (8 normative, 15 structural, 1 cosmetic), 6 refuted. The normative ones are lost
subscripts and superscripts that rename operands or change numbers (`argument-1` with subscript 1 transcribed
`argument-11` in four intrinsic-function rules and in 14.8.2.3, and the 8.8.1.3.1.1 bounds `10^-999` and
`10^1000 - 10^968` flattened to other numbers), and the general formats of SELECT Format 4 (page 234) and OCCURS Format
2 (page 328), whose drawn braces were misplaced. Most structural ones are Annex E flowcharts that kept their labels and
lost every connector. This change lands the runs' compare and verify files only; the ledger is written when a round of
runs closes (README step 5).
