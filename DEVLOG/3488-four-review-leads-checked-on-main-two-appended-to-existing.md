## Entry 3488 — 2026-11-11 13:12 PST — Four review leads checked on main: two appended to existing notes, two needed nothing

A clerk pass over four leads from reviewed reports. Lead A (a fixed 30-second ceiling in the gate self-test's audit-slot arm,
`scripts/gate.py:2641`) is not on origin/main: the assertion exists only on the unlanded PB5322 branch, so nothing was filed and
the reviewer should see it at that branch's landing. Lead B (two push cases failing on Linux at a branch head) passes 267/267 on
both Windows and Linux at main e3c9e8551a, so PB5269 got a short append recording that the failure is gone. Lead C (citation
forms `cite.py` refuses: `§2023:9.1.13.6 item 1`, `§1985:p.XVII-94 item 14`, `§1985:IX.1.3.4 item (1) b)`) went to PB4976 as one
dated append listing the sites checked against main; two of the three families are still only on unlanded branches. Lead D
(2014 em-dash lists run together) is already fixed at main by the dash-list split in PB5268's review round 1, so nothing was filed.
No new notes, so PB5325 and PB5326 stay unused.

