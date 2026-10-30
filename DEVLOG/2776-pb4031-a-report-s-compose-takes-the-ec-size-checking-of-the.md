## Entry 2776 — 2026-10-29 22:16 PDT — PB4031: a report's compose takes the EC-SIZE checking of the GENERATE or TERMINATE that presents it

**The defect.** A printable item's `SOURCE IS <arithmetic-expression>` is an implicit COMPUTE (§13.18.53.4 GR2), but the
report compose that evaluates it is one generated method, emitted outside every statement, so it never saw any statement's
checking state. Under `>>TURN EC-SIZE CHECKING ON`, `SOURCE IS WS-A * WS-B` (100 x 100 into `PIC 9(3)`) printed `000` and
the run went on, where the same COMPUTE statement terminates with EC-SIZE-TRUNCATION. Re-probed on this branch's base:
reproduced exactly (the report printed `000`, `DONE` displayed, then the COMPUTE terminated).

**Which TURN governs (the open design question).** §7.3.25.4 GR6 enables checking "for the procedure division statements and
procedure division headers that follow in the compilation group", so a TURN at the REPORT SECTION entry enables nothing
there; §14.7.5 names GENERATE among the statements whose execution the size error condition "may occur as a result of". The
implicit COMPUTE, the PRESENT WHEN conditions and the VARYING expressions are evaluated as part of the GENERATE or TERMINATE
that presents the group, so that statement's state governs, the answer the report's runtime conditions (EC-REPORT-SUM-SIZE,
EC-REPORT-VARYING) already took.

**The fix.** The state travels at run time as two `CheckingFlags` fields (so PUSH ALL covers them), the run-time twins of the
two compile-time questions `EcState.SizeChecking` and `EcState.SizeTruncationChecking`: a GENERATE or TERMINATE whose line
enables any EC-SIZE-* name sets `ExceptionState.SizeChecking`, and one enabling EC-SIZE-TRUNCATION sets
`ExceptionState.SizeTruncationChecking` too, around its engine call, through the one `EcEmitter.OpenGateFlags` scope
(`ReportWriterEmitter.EmitPresenting`).
- The checked kernels: each numeric render the compose and the presence probe make is rendered twice, plain and under
  `EcState.ComposeSizeChecking` (where every receiver-less render takes the checked kernels, `NumericRenderer.CheckedFor`),
  and `ReportWriterEmitter.UnderPresentingChecking` selects on `SizeChecking`; equal renders emit once. It covers a SOURCE
  expression, a SUM counter's ROUNDED face, a PRESENT WHEN chain (`PresentExpr`, now also the field arm of `PlanOf`, which
  had its own copy of the join) and a VARYING FROM/BY value (`VaryValue`).
- The raising final transfer: `MoveEmitter.ConvertSource` gained `raiseOnSizeError`, under which every numeric arm lands
  through its checked landing and its Try store (`CobolNum.StoreOrRaise`; `RuntimeApi.EditTryFormatFor` /
  `EditTryFormatFloat` wrapped by the new `RuntimeApi.SizeRaisingImage`, which throws EC-SIZE-TRUNCATION), selected by
  `SizeTruncationChecking` and rendered under the checked twin, so an intermediate past binary64 raises EC-SIZE-OVERFLOW
  as the COMPUTE statement does. Under PROHIBITED the rule-7 keep applies only while that flag is off.
A compilation group with no exception checking emits only the plain render (`EcState.Active`), so every existing report
compose is byte-identical.

**Branch review round 1 (finding R1-1, blocking).** The first landing carried EC-SIZE-TRUNCATION alone (one flag, and a
`MoveEmitter.ComputeSender` that set `InSizeError` only on the raising transfer), so a GENERATE under
`>>TURN EC-SIZE-OVERFLOW CHECKING ON` printed `000` for `SOURCE IS WS-D * WS-D` (WS-D = 1.0E200) where the next COMPUTE
raised EC-SIZE-OVERFLOW; PRESENT WHEN and VARYING BY expressions never raised at all. Re-probed on the reviewed head (no
RAISED line at any of the three) and fixed by the family flag and the checked twin above; `ComputeSender` is deleted (the
twin is the one mechanism). Lead R1-2 fixed in the same change: the `CheckingFlags` doc comment named a member that does
not exist.

**Goldens.** `13.18.53-SOURCE/pb4031_report_source_size_checking` (2002, 2014, 2023): checking off keeps the low-order
digits (`000`, `  0`), checking on prints a fitting value (`500`) and raises EC-SIZE-TRUNCATION from a GENERATE into
`9(3)` and into `ZZ9` and from a TERMINATE's CONTROL FOOTING FINAL, and EC-SIZE-OVERFLOW from a binary64 intermediate,
each reaching the USE declarative. `pb4031_report_overflow_checking` (2002, 2014, 2023): under EC-SIZE-OVERFLOW alone a
SOURCE expression, a PRESENT WHEN condition and a VARYING BY expression each raise EC-SIZE-OVERFLOW, a truncation stays
unraised and prints its low-order digits (`000`, DOC-A.1-70), and nothing raises with checking off (the reviewed head:
`RAISED 0 TIMES`). `pb4031_report_source_prohibited_checked` (2014, 2023): the PROHIBITED item keeps spaces with checking
off, prints an exact value and raises on an inexact one with checking on. Refusal `pb4031-report-turn-size-85` (COBOL0900
at 85). No diagnostic code used (COBOL5461-5463 returned).

**Rows.** GR-13.18.53.4-2, GR-7.3.25.4-6, GR-14.7.5-3, GR-14.7.5-4, GR-14.7.4.3-7, GR-13.18.41.4-2, GR-13.18.64.4-3 and
DOC-A.1-70 were already CONFORMS; two record_verdicts batches added the goldens and code sites as witnesses and retired the
two deleted members (`MoveEmitter.cs#ComputeSender`, `ReportWriterEmitter.cs#RaisingImage`), GAP unchanged. Docs:
REPORT_WRITER_DESIGN §3 (the governing statement, the two flags), CONFORMANCE.md DOC-A.1-70.

**Left as leads (other roots).** A receiver-less render in ANY statement that enables EC-SIZE-TRUNCATION but not
EC-SIZE-OVERFLOW terminates the run unit with EC-SIZE-OVERFLOW (`IF WS-D * WS-D > 0`; `NumericRenderer.CheckedFor` keys
the checked kernels on the family, and nothing catches the unenabled name); the compose inherits it through the same rule.
And a COMPUTE statement under EC-SIZE-OVERFLOW alone leaves a truncated receiver unchanged (`COMPUTE WS-C = WS-A * WS-A`
kept 007), where DOC-A.1-70 says an unenabled EC-SIZE-TRUNCATION stores the low-order digits.

