## Entry 3651 — 2026-11-14 21:52 PST — A numeric or integer function is refused where an identifier stands at --std 85 (COBOL0900, PB5108)

At `--std 85`, `MOVE FUNCTION SQRT (4) TO N`, `DISPLAY FUNCTION LENGTH (W)`, `PERFORM FUNCTION INTEGER (2.5) TIMES`, `GO TO ... DEPENDING
ON FUNCTION LENGTH (X)`, a PERFORM VARYING FROM or BY function and the sending operands of ADD, SUBTRACT, MULTIPLY and DIVIDE all
compiled with no diagnostic. X3.23-1985 with Amendment 1 says "A function-identifier which references an integer or numeric function
may be used only in an arithmetic expression" (§1985:IV.4.3.8.3.3 SR4; §1989:A.2.3 GR2 and GR3), and each of those positions prints an
identifier. ISO/IEC 1989:2002 Annex F.2 item 60 removed the restriction (`§2002:F.2`), so the edge is a construct row,
`numeric-function-identifier-operand-2002` (COBOL0900; VCR row 7.76).

One screen, `IntrinsicBinder.ScreenIdentifierPlacement`, reads the bound operand's category and reports the row below 2002. Every door
that binds a function written as an identifier asks it: `IntrinsicOperand` (MOVE, DISPLAY, PERFORM TIMES, GO TO DEPENDING, WRITE
ADVANCING, INSPECT, INITIALIZE REPLACING), the new `ExpressionBinder.BindArithmeticStatementOperand` (the five sending-operand sites of
`ArithmeticBinder`, which called `BindExpr`; the wrapper walk behind `BindExpr` also serves relation and EVALUATE operands, where a
function is an arithmetic expression, so the screen could not live in the walk) and `BindVaryingOperand`. COMPUTE, relations,
EVALUATE and subscripts are untouched, and an alphanumeric function stays legal (SR3). The report SOURCE operand and the CALL argument
keep their own rows; `ReportWriterBinder.SourceIdentifierOperand` binds without the screen so SOURCE is not reported twice.

Goldens: `pb5108_numeric_function_as_identifier` (2002 to 2023), the control `pb5108_function_placement_85_legal` (85 to 2023) and
`pb5108-numeric-function-as-identifier-85` (refused at 85). The refusal exposed fourteen existing goldens accepted at 85 with such a
function. Twelve were rewritten to the 85-legal form with the output unchanged (`COMPUTE` into the receiver; a work item of the
function's device picture for a DISPLAY operand), and two now accept from 2002: `pb1421_goto_depending_function_identifier` and
`pb4075_wide_argument_consumers`.

Not changed: the 1985 inventory rows stay unrecorded (kb/Work PB5094), so `closes_rows` is empty with its reason.

