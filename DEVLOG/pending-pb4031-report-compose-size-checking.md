## PB4031: a report's implicit COMPUTE takes the EC-SIZE checking of the GENERATE or TERMINATE that prints it

**The defect.** A printable item's `SOURCE IS <arithmetic-expression>` is an implicit COMPUTE (§13.18.53.4 GR2), but the
report compose that evaluates it is one generated method, emitted outside every statement, so it never saw any statement's
checking state. Under `>>TURN EC-SIZE CHECKING ON`, `SOURCE IS WS-A * WS-B` (100 x 100 into `PIC 9(3)`) printed `000` and
the run went on, where the same COMPUTE statement terminates with EC-SIZE-TRUNCATION. Re-probed on this branch's base:
reproduced exactly (the report printed `000`, `DONE` displayed, then the COMPUTE terminated).

**Which TURN governs (the open design question).** §7.3.25.4 GR6 enables checking "for the procedure division statements and
procedure division headers that follow in the compilation group", so a TURN at the REPORT SECTION entry enables nothing
there; §14.7.5 names GENERATE among the statements whose execution the size error condition "may occur as a result of". The
implicit COMPUTE runs as part of the GENERATE or TERMINATE that prints the item, so that statement's state governs, the
answer the report's runtime conditions (EC-REPORT-SUM-SIZE, EC-REPORT-VARYING) already took.

**The fix.** The state travels at run time. A new ambient flag, `ExceptionState.SizeTruncationChecking` (a `CheckingFlags`
field, so PUSH ALL covers it), is set by a GENERATE or TERMINATE whose line enables EC-SIZE-TRUNCATION, around its engine
call, through the one `EcEmitter.OpenGateFlags` scope (`ReportWriterEmitter.EmitPresenting`). Each implicit COMPUTE in the
compose (a SOURCE expression, a SUM counter's ROUNDED face) emits both transfers and picks by the flag:
`MoveEmitter.ConvertSource` gained `raiseOnSizeError`, under which every numeric arm lands through its checked landing and
its Try store (`CobolNum.StoreOrRaise`; `RuntimeApi.EditTryFormatFor` / `EditTryFormatFloat` wrapped by the new
`RuntimeApi.SizeRaisingImage`, which throws EC-SIZE-TRUNCATION) and the operations that develop the result take the
size-error kernels (`ComputeSender`, `ReceiverContext.InSizeError`), so an intermediate past binary64 raises
EC-SIZE-OVERFLOW as the COMPUTE statement does instead of reaching the store as a non-finite value. Under PROHIBITED the
rule-7 keep applies only while the flag is off. A compilation group with no exception checking emits only the checking-off
transfer (`EcState.Active`, the zero-scaffolding gate), so every existing report compose is byte-identical.

**Goldens.** `13.18.53-SOURCE/pb4031_report_source_size_checking` (2002, 2014, 2023): checking off keeps the low-order
digits (`000`, `  0`), checking on prints a fitting value (`500`) and raises EC-SIZE-TRUNCATION from a GENERATE into
`9(3)` and into `ZZ9` and from a TERMINATE's CONTROL FOOTING FINAL, and EC-SIZE-OVERFLOW from a binary64 intermediate,
each reaching the USE declarative. `pb4031_report_source_prohibited_checked` (2014, 2023): the PROHIBITED item keeps
spaces with checking off, prints an exact value and raises on an inexact one with checking on. Refusal
`pb4031-report-turn-size-85` (COBOL0900 at 85). No diagnostic code used (COBOL5461-5463 returned).

**Rows.** GR-13.18.53.4-2, GR-7.3.25.4-6, GR-14.7.5-3, GR-14.7.5-4, GR-14.7.4.3-7 and DOC-A.1-70 were already CONFORMS; a
record_verdicts batch added the goldens and code sites as witnesses, GAP 21 -> 21. Docs: REPORT_WRITER_DESIGN §3 (the
governing statement), CONFORMANCE.md DOC-A.1-70.

**Left as a lead.** The compose's OTHER receiverless renders (PRESENT WHEN conditions, VARYING FROM/BY, subscripts) and
the implicit COMPUTE's intermediates under a presenting statement that enables EC-SIZE-OVERFLOW or -UNDERFLOW but not
EC-SIZE-TRUNCATION still compile unchecked: one run-time flag selects the checked arm, and it is TRUNCATION's.
