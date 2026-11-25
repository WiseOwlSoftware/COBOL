## Entry 4062 — 2026-11-25 02:47 PST — A wholly absent STEPped field keeps its entry's layout for later occurrences (PB6178)

PB6178 asked what column a later operand's step anchor is measured from when a whole report field is absent (hoisted constant sum in
`EmitAnchorSeeds`), and carried two comments PB6146 left stale in `ReportWriterEmitter.EmitFieldPlacements`.

Re-probe on this build (OCCURS 2 STEP 10 VARYING G, two relative operands, `PRESENT WHEN G NOT = 1`): occurrence 1 absent, occurrence 2
prints at 17 and 19 (`HELLO` + 11 spaces + `2 2`). Decision: keep. §2023:13.18.38.4 GR12b positions a later occurrence from the column
the items occupy in the preceding one; §2023:13.18.41.4 GR2b makes an absent entry "as though omitted" but names no step origin, so the
entry keeps the layout it takes when present, which avoids collapsing the operands onto one column. Recorded in kb/Work/PB6178.md.

Fix shape: the two stale comments (the hoist comment in `EmitFieldPlacements` and the AnchorSeed arm) now say the hoisted seed serves a
wholly absent field and an absent operand 0, and a later operand of a present field is rewritten from the live counter inside the field
guard. No emitted text changed. Golden `pb6178-absent-field-anchor-layout` (2002) and refusal `pb6178-absent-field-anchor-layout-85`
(COBOL0900). No inventory rows closed. Gate: see the landing.

