## Entry 2953 — 2026-11-01 10:50 PST — PB4361 and PB4368: GOBACK RETURNING is screened as a MOVE sender, and two message arms quote a procedure-name as written

Two defects in `ControlFlowBinder`, found by earlier reviews and fixed at their roots.

**PB4361.** `GOBACK RETURNING x` with no header RETURNING item folds into an implicit `WITH NORMAL STATUS x` (PB4335), and
`ReturningAsStatus` then ran the STATUS phrase's screen over the operand. Re-probe: a `PIC S9(3)V99 COMP-3` operand and an
index data item both drew COBOL1704 citing "§14.9.18.3 SR6 for GOBACK" although the program wrote no status phrase, and the
COMP-3 operand compiled clean under `PROCEDURE DIVISION RETURNING R`. SR6 ("Identifier-2 shall reference an integer data
item or a data item with usage display or usage national", `cite.py --check` OK) governs identifier-2 of the status phrase;
§14.9.18.2 has no RETURNING phrase, so the refusal was a second defect, not only a wrong message. ISO is silent and GnuCOBOL
moves the operand to RETURN-CODE, so the operand is now screened as the sender of that MOVE: `MoveTable16.SenderClassRefusal`
(§14.9.25.3 SR1, the reader the header-RETURNING arm's `BindMoveOf` also asks) refuses index, message-tag, object and pointer
operands as COBOL0809 naming the implicit MOVE of GOBACK RETURNING, and every other operand exits with its value truncated
toward zero (item 192). Goldens: `pb4361_goback_returning_scaled_operand` (2002) and `pb4361-goback-returning-index-item`
(COBOL0809 at 2002, 2014, 2023); `StopGobackExitCodeTests` asserts exit 7 for COMP-3 and COMP-2 7.5. `docs/CONFORMANCE.md`
records the screen on the `goback-returning` row. No GnuCOBOL differential was run (no cobc from the implementer's shell).

**PB4368.** The PERFORM THRU SR11 message (COBOL2186) and the ALTER shape message (COBOL1757) still passed
`ProcedureNameShape.Head`, so `P1 OF S1` printed as `P1`. Both now pass `AsWritten`; two rows join
`ProcedureNameMessageTests`. The PB4277 entry (2932) said "No code is added or changed"; it is corrected in place: no rule
and no accepted or refused program changes.

Self-review: the architecture lens found one rule per place (the sender class screen is `MoveTable16`'s, not a copy); the
code lens checked the STATUS phrase arms still ask SR6 and nothing else calls the status screen; no hot path is touched.

**Review round 1 (PB4361).** The reviewer found that `ReturningAsStatus` asked only SR1's class half of the MOVE it stands
for, a hand-composed subset of the validity question that `MoveTable16AskerDriftTests` exists to forbid. A boolean operand
(`PIC 1(8) USAGE BIT`) that the old SR6 screen refused compiled clean and aborted at run time, while the header-RETURNING
arm refuses it as COBOL0819 (SR10, Table 16). The screen now goes through `MoveBinder.ScreenImplicitMoveToPosition`: SR1 and
the whole `MoveTable16.Validity` chain against an elementary-integer receiver position, framed by `FrameMoveRefusal`, which
`MoveCategoryLegality` also uses now, so the written MOVE and this implicit one word a refusal alike. Golden:
`pb4361-goback-returning-boolean-item` (COBOL0819 at 2002, 2014, 2023). Two comment and doc leads were also fixed: the
truncation to an integer is item 192's implementor determination, not an ISO rule, and the CONFORMANCE.md row now says a
COMP-2 operand beyond the 128-bit integer range ends the run unit with EC-SIZE-OVERFLOW. One lead is left: a display
boolean item under the STATUS phrase aborts at run time (`NumericRenderer.StatusNum`), pre-existing and outside this diff.

