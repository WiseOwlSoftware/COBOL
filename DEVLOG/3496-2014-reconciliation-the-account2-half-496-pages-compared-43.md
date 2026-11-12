## Entry 3496 — 2026-11-11 14:00 PST — 2014 reconciliation, the account2 half: 496 pages compared, 43 discrepancies confirmed

The first 2014 reconciliation: the seven runs of the `account2` half of `2014/ranges.json` (account2-r01 to r07) ran
in parallel through `.claude/workflows/spec-reconcile.js` on Opus at effort medium, as the 2002 runs did (workflows
wf_d5e1c11c-7dc, wf_0e29e375-ff3, wf_7def2beb-58a, wf_01bb3b98-924, wf_5173d248-142, wf_047c66c4-632 and
wf_df4089e0-2d4; 112 agents, 9.92M subagent tokens). All 496 pages were read and every claim got its own adversarial
verdict: 70 claimed, 43 confirmed (7 normative, 35 structural, 1 cosmetic), 27 refuted. The normative ones mostly repeat
the 2002 transcription's defects in the 2014 text: the OCCURS Formats 2 and 4 general formats with misplaced braces
(page 370), the level-number `1` without its underline in the 13.10.1 general format (page 306), the MOVE table's
merged numeric-edited sub-rows (page 559), flattened operand subscripts in ADD (page 491) and the 8.8.1.3.1.1 bounds
written without their superscripts (page 166). Page 219 holds a Greek capital iota in place of a Latin `I` ("If"),
which breaks exact text matching. Three agent files were written CRLF and rewritten LF before commit (kb/Work PB5330).
This change lands the runs' compare and verify files only; the ledger is written when the round closes (README step 5).

