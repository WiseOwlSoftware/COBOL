## PB4765, PB4947: a counter register is refused as a condition, and a parenthesized function or register is no relation operand of an index-name

**PB4947.** `IF LINAGE-COUNTER`, `IF LINAGE-COUNTER OF F` and `IF LINE-COUNTER` compiled with only warning COBOL1756 and aborted the run
unit, while `IF W` over a numeric item drew COBOL2318. A register resolves to no data item, so `ConditionBinder.RefuseNonCondition` sent it
to the undefined-word arm. It now has a register arm first (`DataReferenceCst.Register`), reporting COBOL2318 through the one
`ReportNotACondition` wording (§2023:8.8.4.2.1 lists the simple conditions; §2023:8.4.3.14.4 GR1 and §2023:8.4.3.15.4 GR1 make the
registers numeric data items). The alias, class-name and mnemonic tests in that method, `BareClassWord`, and two reads in
`SearchAllFormat2Rules` took `dref.cobolWord()`, which in the register alternatives is the QUALIFIER; they ask
`ReferenceResolver.WrittenWordOf` now (PB4154's sweep of the sites this mechanism reaches).

**PB4765.** The four tree-shape reads of a function-identifier: `RelationComparability.IndexNameAgainstNumeric` was the live defect (`IF IX =
(FUNCTION ABS (N))` and `IF IX = (LINAGE-COUNTER)` compiled, `IF IX = (N)` was refused COBOL2533), and `IsNumericIntegerOperand` admitted
`A = (LINAGE-COUNTER)` over an alphanumeric A. Both ask `ExpressionAsWritten` (PB4763's statement) first. The MoveBinder, OoBinder and
ConditionBinder reads are clear: a MOVE source has no parenthesized form in the grammar, the OoBinder read follows `SoleFunctionCall`, and the
ConditionBinder read follows PB4763's refusal.

Goldens: `pb4947-if-linage-counter`, `-qualified`, `pb4947-if-line-counter`, `pb4947-if-page-counter` (COBOL2318);
`pb4765-index-name-paren-function`, `pb4765-index-name-paren-counter` (COBOL2533), `pb4765-alphanumeric-paren-counter` (COBOL2532), and the
control `pb4765-index-name-sole-function` (prints EQ, NE). No inventory row claimed or closed.
