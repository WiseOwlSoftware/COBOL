## File the leads of PB3760 and PB3609: PB3969-PB3971, and a PB3849 witness

PB3969 records a Format-1 CALL whose computed numeric operand (`FUNCTION MAX(W 0)`, `(1.25 + 0)`) reaches a numeric formal as zero; a silent wrong answer found by the PB3760 implementer, pre-existing on main, related to PB3677 and PB1113.

PB3970 records that no golden pins the Format-1 BY CONTENT screen's `cBareLit` and `cGrouped` arms (PB3760 review lead R2-1), so reverting them leaves the suite green.

PB3971 records that `rearm_hold` tests its LANDED arm after the no-worktree arm while `drop_stale_rearms` drops a LANDED re-arm regardless of worktree (PB3609 review lead R1-1), so the status line can promise the opposite of the next tick.

PB3849 gains a 2026-10-27 witness: PB3609's notes-only commit `ac958bcfe` after a gated head forced the resume unit to re-gate the tip.
