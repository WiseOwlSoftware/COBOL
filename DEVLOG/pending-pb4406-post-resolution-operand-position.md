## PB4406: the screens that run after a reference resolved report at the operand, not at the statement

PB4383 put the position inside `ReferenceResolver.ResolveImpl`, so an undefined name on a continuation line named its own
line. The reports made AFTER the resolution did not move: the expression mapping (`ExpressionBinder.RefExpr`: the
index-name COBOL1637, the class COBOL0844, a LINAGE-COUNTER refusal, a keyword-omitted function's COBOL1543), the
statement-slot mapping (`FieldOperand`), the receiving chokepoint (`ResolveReceiving`: COBOL2197, COBOL2026, COBOL3979 and the
other bars), the arithmetic resultant's category screen (`ScreenResultant`, COBOL1675) and the literal refusals. Each ran
after the resolver's scope had closed, under the statement's cursor.

**Re-probe (main 68de94820).** With the operand on the line below `COMPUTE N = 1` or `ADD 1 TO N`, sixteen reports named
`(12,12)`, the statement's first token, while the operand stood on line 13. `ADD "A" TO N` named the verb's column (12,12), the
literal being at column 16.

**Fix.** The members that map a reference to a bound node, or screen one, open `using var _ = ctx.Edition.At(...)` on the
operand: `FieldOperand`, `RefExpr`, `ResolveReceiving`, `RefuseNonReceivable`, `ScreenResultant`, `NonNumericLiteralOperand`,
`LiteralOperand`, `NumLiteral`, `NonNumericInNumericContext`, `NotNumericIdentifier` and `BindPrimary`. `ScreenResultant` took the
operand's text and now takes its parse node, so its four callers (ADD/SUBTRACT/COMPUTE receivers, MULTIPLY BY, DIVIDE REMAINDER)
pass the node and the screen positions itself. The scope is a struct, so the hot path allocates nothing.

**Tests.** `DiagnosticPositionTests.PostResolutionScreen_NamesTheOperandsOwnLineAndColumn` (18 cases) pins the line and column of each
code above; `PostResolutionOperandPositionDriftTests` lists the members that must open the scope, so a renamed member fails loudly
and a new reporter is added to the list. No conformance golden: the refusal goldens pin the edition and the code, never the position.

**Rows.** None: the same programs are refused with the same codes.

**Left, registered in the report.** The verb binders' statement-shape diagnostics that name one operand (SET sender, CORRESPONDING,
the `BoundRejected.Report` sites of SetBinder, InspectBinder, KeyedIoBinder, SortBinder, ReportWriterBinder), the subscript and
reference-modification bound screens in `ReferenceResolver`, and `CheckLiteral(string)`'s node-less callers still stand at the
statement or the outer reference.
