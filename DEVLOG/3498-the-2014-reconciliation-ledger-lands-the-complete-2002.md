## Entry 3498 — 2026-11-11 14:18 PST — The 2014 reconciliation ledger lands; the complete 2002 ledger is held for its rule catalog (PB5332)

`merge_reconciliation.py --write` wrote the ledgers of both editions from the agent files that landed with the fourteen
2002 runs (#1522 to #1525) and the fourteen 2014 runs (#1526, #1527); `--check` read CURRENT for both. Only
`LEDGER-2014.json` and `2014/REPORT.md` land here. A complete `LEDGER-2002.json` makes `reconciled(2002)` true, and the
2002 rule catalog then fails on six findings it records today (the gate's RULE CATALOGS audit went RED on this
branch), so the 2002 ledger is held until they are resolved: kb/Work PB5332. 2002: 882 of 882 pages swept, 123 claims, 83 confirmed (17 normative, 62
structural, 4 cosmetic), 40 refuted. 2014: 952 of 952 pages, 134 claims, 74 confirmed (13 normative, 57 structural, 4
cosmetic), 60 refuted. Every claim carries its own adversarial verdict.

A correction to the entry for #1525 (the 2002 account1 half): its figures, 72 claimed and 42 confirmed, were the
merge's totals on a branch cut after account2-r01 (#1522) had landed, so they counted r01's 12 claims and 4
confirmations a second time. The account1 half alone is 60 claimed and 38 confirmed. The ledger counts each agent file
once.

The confirmed defects are the repair work: kb/Work PB4813 (open) for 2002 and the new PB5331 for 2014. They mostly
repeat across the two editions: operand subscripts and exponent superscripts flattened into other names and numbers,
general formats whose braces or underlines moved (OCCURS, SELECT, RECORD, UNLOCK, the level-number brace), and
tables whose rows shifted or merged (the MOVE category table, Table 2's characters, 2014's reserved-word list).

