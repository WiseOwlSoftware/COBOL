## Entry 2768 — 2026-10-29 21:04 PDT — PB3841: the float-landing descriptions PB3829 left stale now describe the two-answer rule

PB3829 deleted `FixedLane`, `FromDoubleBounded`, `IntrinsicCodomain` and the third `FloatLanding` arm and did not sweep their descriptions. This change is documentation and doc comments only; no behavior moved, so no golden was added.

**Re-probe.** The note's three sites were stale as filed. `FloatWorkingScale` is already gone (PB3839 deleted it with its only reader), so the third bullet's second half needed no edit.

**Fix.**
- `docs/NUMERIC_DESIGN.md` names the two goldens that exist, `14.7.4-ROUNDED/pb3829_rounded_bounded_function_modes` and `15.10.4-Returned-value-rule/pb65_bounded_returned_value_landing`, and marks the `2023/pb65_codomain_clamp` blast-radius line as PB647 history.
- `NumericRenderer.Landed` no longer crefs the deleted `FixedLane`; it says every consumer carries its own float arm.
- `FloatLandingDecision` says exactly two answers (quantize at a fixed-point resultant, or keep the binary64).

**Sibling sweep.** Present-tense descriptions of the deleted nested `max(Receiver.Scale, 9)` landing in `NumericRenderer.Power`, `IntrinsicRenderer.RenderFloat`, `NumericRenderer.Fold`, `OperandText` and the `CobolIntrinsics` Int128 remark were rewritten to the PB653 rule. A grep for `FixedLane`, `FromDoubleBounded`, `IntrinsicCodomain`, `CodomainConst` and `pb65_codomain_clamp` over `src/` and `docs/` now finds only history (frozen census evidence, the dated plan narrative, dated clauses in the design doc).

**Rows.** None claimed, none closed (`closes_rows: []` with a reason). PB3841 is `landed`.

