## PB4383: an undefined operand on the continuation line of a statement is reported at that line, not at the statement's first token

`DISPLAY W-X` on one line and `Q-ONE Q-TWO.` on the next drew COBOL1639 for both names at the DISPLAY's position
(8,12), a line before the names stand. Re-probed on main 3b34806eb: it reproduced exactly. The note blamed
`AcceptDisplayBinder.BindDisplay` and the unpositioned `FieldOperand` call, but the fault was wider. The report is
made in `ReferenceResolver.ReportUnidentified`, and positioning the cursor at an operand was each statement binder's
job, so every verb misplaced a reference that did not start on the statement's first line.

**Fix at the chokepoint.** `ReferenceResolver.ResolveImpl`, the one entry every verb's data-reference resolution passes
through, now opens `using var _ = data.Edition.At(dref)`. Everything the resolution reports (COBOL1639, the reference
modification and linkage screens, COBOL2364) lands on the reference's first token; a nested subscript restores the outer
position on exit. The screens that run after the resolution report from their binders, so `BindDisplay` positions each
operand (the COBOL1637 index-name screen, the excluded-class refusal), and so do the three other callers of
`ScreenIndexNameOperand` under a statement cursor: MOVE's sending operand, the STOP RUN / GOBACK status operand and
`StringUnstringBinder.ScreenedField`. `ScreenOperandClass` has only the DISPLAY caller.

**Result.** The repro now reports `(9,16)` for Q-ONE and `(9,22)` for Q-TWO. `DiagnosticPositionTests` had pinned the
old statement column in three places (`BindError_NamesFileLineAndColumn`, the `cursor` and `switch` rows of
`FixedForm_DiagnosticNamesThePhysicalColumn`); each now pins the reference's column, and
`OperandOnAContinuationLine_NamesItsOwnLineAndColumn` pins DISPLAY, a DISPLAY index-name, MOVE's receiver and sender and
STRING on a continuation line. No conformance golden: the refusal goldens pin the edition and the code, never the
position. `docs/VALIDATION_DESIGN.md` (Positions) names the chokepoint. No inventory row changes verdict
(`closes_rows: []`, the reason in the note).

**Self-review.** Architecture: one mechanism, the resolver chokepoint, plus per-operand positions only where a screen
reports after the resolution. Full code: a null `Start` (a synthetic reference) leaves the position standing, as
`DiagnosticCursorAt.At` defines. Performance: the scope is a struct and the position a two-int write, on a path that
already allocates a `RefResolution`. Duplication: no second positioning rule; the callers that already wrapped
`FieldOperand` in `At` (report writer, condition binder) are unchanged and now redundant only for the resolver's own
reports. The gate's verdict line is recorded in the implementer report.
