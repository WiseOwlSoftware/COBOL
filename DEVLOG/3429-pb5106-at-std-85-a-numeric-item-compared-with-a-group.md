## Entry 3429 — 2026-11-10 04:47 PST — PB5106 - at --std 85 a numeric item compared with a group compares its own image, not the de-signed one

**Re-probe.** The note's program (RELGRP) printed EQUAL / NOT EQUAL / SIGNED NOT EQUAL / SIGNED EQUAL at `--std 85` on a fresh build; the 1985 text requires SIGNED EQUAL then SIGNED NOT EQUAL.

**Root cause.** `ConditionRenderer.RenderRelationalCore` read every signed numeric operand of a character comparison de-signed (§2023:8.8.4.2.5 into §2023:14.9.25.4 GR6a), the elementary-move rule. §1985:VI.6.3.1.1.2 GR2 makes the numeric operand "moved to a group item of the same size" when the other operand is a group, and a move to a group is an alphanumeric-to-alphanumeric move with no conversion (§1985:VI.6.19.4 GR3 item 5), so a SEPARATE sign character is part of what is compared. The note's `GR5` citation for the move was wrong; `cite.py --check` found the clause GR3 item 5.

**Fix.** One predicate, `ConditionRenderer.DropsOperationalSign(a, b)`: false when the dialect is below 2002 and either operand is a group (`RelationPair.GroupItemOf`), true otherwise. The direct relation and the EVALUATE range (`Visit(BoundRangeMembership)`, which lowers to the same pair, §2023:14.7.8) both ask it. From 2002 a group is treated as an elementary alphanumeric item (§2002:8.8.4.1.1), so the old behaviour stays there. The figurative-relation arm needs nothing: its anchor is never the group with a sign to keep.

**Golden.** `conformance-suite/cases/8.8.4.2-Simple-relation-conditions/pb5106-numeric-with-group-1985` with `.85.out` and `.out`: unsigned control, SIGN LEADING and TRAILING SEPARATE, group as subject and as object, and the EVALUATE range. No refusal case: nothing is refused below 1985. Rows closed: none (the inventory has no 1985 rows yet, PB5094).

