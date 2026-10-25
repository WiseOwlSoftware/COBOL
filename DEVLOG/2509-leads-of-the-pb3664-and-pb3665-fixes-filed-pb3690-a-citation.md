## Entry 2509 — 2026-10-24 19:52 PDT — Leads of the PB3664 and PB3665 fixes filed: PB3690 (a citation), PB3691 (held for the owner), and a PB3375 recurrence

PB3664's branch reviewer found that the new sign rule cites §14.6.8.2 4), which governs a fixed-point numeric receiver,
where a numeric-edited receiver is §14.6.8.2 5). PB3690 records the four sites, and the operator corrects them once
PB3664 lands. PB3665's implementer recorded the determination D-FNUSE: a numeric, integer, index or boolean
function-identifier is not usage display or national. By the same reading, §8.8.4.2.5 refuses `IF FUNCTION INTEGER(N) =
"0007"`, which compiles today. PB3691 holds that sibling for the owner's approval (`status: owner`), because it is a
behavior change found through a fuzz fix. PB3665's round-2 review also found `record_verdicts.merged` blanking an
inventory row's notes, which is open PB3375's defect. PB3375 gains the recurrence and the row to restore.

