## Entry 2690 — 2026-10-28 10:22 PDT — Abandon the two branches that followed GnuCOBOL's COMP-5 exemption; carry their findings into PB3975 and PB3990; file PB3993

The owner ruled on 2026-10-27 that GnuCOBOL 3.2's exemption of COMP-5, COMP-X and COMP-N receivers from
keep-on-overflow under a NOT-only statement is GnuCOBOL's error (PB3989). Two branches had followed it:
`worktree-wf_5cf68b99-205-1` (wave 20261735 group A, which also fixed PB3975) and `worktree-wf_57132140-60a-1` (wave
20261736's finisher of it, which also added a rule-7 gate). On the morning restart the planner's dry run proposed a
finisher for PB3869 + PB3975 on 205-1, and held PB3990 and PB3953 behind it for a file-set collision. PB3989 now records
both branches ABANDONED, so the planner stops planning them and the prune can delete them.

Nothing is archived, so what the branches found moves into the notes before the branches go. PB3975 gets the re-probe:
the filed case prints the same as GnuCOBOL, and the real difference is PB129's digit cap on the subsidiary quotient
(803 against GnuCOBOL's 003). The note also carries the open question the re-plan must settle: whether a NOT-only
statement keeps identifier-4 under PB3989. PB3990 gets round-3 review lead R3-1: the PROHIBITED truncation hits every
usage, not only binary. The truncation is DOC-A.1-70's PB1196 determination, so the fix must overturn it together with
the unchecked leg of the pb1196 golden. PB3990 also gets the abandoned finisher's exactness helper
(`RuntimeApi.InexactAtScale` over every carrier) and its site list. PB3869's title and `closes_rows_reason` now
qualify "GnuCOBOL keeps them" with the exemption (lead R3-2). PB3993 is new (lead R3-3): no golden or test row pins
the keep for a binary-capacity receiver, so a change that copied the exemption would turn nothing red.

