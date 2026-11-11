## The 2002 and 2014 reconciliation ledgers: every page of both editions compared, 157 discrepancies confirmed

`merge_reconciliation.py --write` wrote `LEDGER-2002.json`, `2002/REPORT.md`, `LEDGER-2014.json` and `2014/REPORT.md`
from the agent files that landed with the fourteen 2002 runs (#1522 to #1525) and the fourteen 2014 runs (#1526,
#1527); `--check` reads CURRENT for both. 2002: 882 of 882 pages swept, 123 claims, 83 confirmed (17 normative, 62
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
