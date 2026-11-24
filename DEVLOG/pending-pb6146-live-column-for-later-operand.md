## A later operand of a multiple COLUMN clause places at the live horizontal counter when an earlier operand is absent (PB6146)

PB6146: `EmitAnchorSeeds` hoists every operand's step anchor as a constant sum assuming all earlier operands present, and the
`AnchorSeed` arm places repetition 0 at that anchor. With a per-placement PRESENT WHEN that absents operand 1 and keeps operand 2,
operand 2 printed at the hypothetical column (9) instead of horizontal counter + integer-2 (7), against §2023:13.18.41.4 GR2b and
§2023:13.18.14.4 GR8.

Re-probe on this build (new case, STEP 10, `PRESENT WHEN J NOT = 1`): `01 [HELLO   2         2]` before the fix (col 9, 19). Fix:
`EmitFieldPlacements` rewrites a later operand's anchor from the live `__hc` just before its placement (outside the placement's
presence guard, so an absent operand still leaves its "would occupy" anchor); the hoisted seed still serves a wholly absent field.
Golden `pb6146-absent-earlier-operand-live-column` (2002, `HELLO 2` + 9 spaces + `2`, computed from GR8 and GR12) and its
refusal below 2002 (`pb6146-absent-earlier-operand-live-column-85.json`, COBOL0900). No inventory rows closed.
