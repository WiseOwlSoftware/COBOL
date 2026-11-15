## Entry 3647 — 2026-11-14 20:42 PST — PB5105 at --std 85 MOVE, ADD and SUBTRACT CORRESPONDING refuse a level-66 operand and a USAGE IS INDEX group

**Re-probe.** The note's repro now stops earlier (COBOL1639: `RENAMES A THRU B` is ambiguous when G2 also has an `A`), so the probe was rewritten with qualified names (`RENAMES A OF G1 THRU B OF G1`). On that build, `MOVE CORRESPONDING R TO G2`, `MOVE CORRESPONDING G1 TO R3` and a `USAGE IS INDEX` group as either operand all compiled at `--std 85`, exit 0.

**Cause.** `StatementValidation.CheckCorrespondingGroupOperand` carried one rule per verb from the 2023 text. ADD and SUBTRACT exclude level 66 by name; MOVE's rule does not. §1985:VI.6.4.3 GR3 ("The description of D1 and D2 must not contain level-number 66, 77, or 88 or the USAGE IS INDEX clause") governs all three at COBOL-85, and nothing asked it. ADD and SUBTRACT refused level 66 only by accident of the 2023 rule, and no verb refused a USAGE INDEX group.

**Fix.** One edition check ahead of the per-verb arms: below COBOL-2002, the operand's denoted item must not be a RENAMES entry and neither it nor any enclosing group may carry USAGE INDEX (`Level85Forbidden`). Level 77 and 88 never name a group, so they were already refused. COBOL1757, no new code. 2002 and later are unchanged.

**Goldens.** `1985-GR-VI_6_4_3-3` (MOVE with a level-66 sender and receiver) and `1985-GR-VI_6_4_3-3-usage-index` (ADD and SUBTRACT over a USAGE INDEX group), each refused at 85 with COBOL1757 and each with a legal control line.

**Rows.** No inventory row (the inventory has no 1985 rows yet); `closes_rows: []` with its reason.

