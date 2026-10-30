## Entry 2816 — 2026-10-30 15:24 PDT — EVALUATE's Table 15 reads the predefined NULL as an identifier, not a literal (PB1682)

**Mechanism.** `EvaluateBinder.OperandKindOf` classified a bare operand as a LITERAL whenever its `nonNumericLiteral`
parse node was present (ISO §14.9.13.4 GR1). Since PB1427 the grammar carries the predefined NULL as the
`predefinedNull` arm of that node, only as a parse carrier; NULL is an identifier (§8.4.3.1.2 Format 8). So
`EVALUATE NULL WHEN "A"` drew COBOL1634 "the selection subject is a literal", a false reason, and hid the true one.

**Re-probe.** On this tree `EVALUATE NULL WHEN "A"` produced COBOL1634 alone (no COBOL0869 beside it, unlike the
note's older tree), and `EVALUATE NULL WHEN P` (P a pointer) compiled and printed EQ. `EVALUATE NULL WHEN NULL` also
drew COBOL1634; it is now refused as COBOL2576 (§8.4.3.10.3 SR1: no data-item operand for NULL), its true reason.

**Fix.** `OperandKindOf` answers Identifier for `nonNumericLiteral().predefinedNull()`. Sibling sweep: every
PROCEDURE DIVISION site that tests `nonNumericLiteral() is …` (CallBinder, IntrinsicBinder, SetBinder) already decides
NULL or binds through `NonNumericLiteralOperand`, which refuses it.

**Guard.** `PredefinedNullContextDriftTests.EveryProcedureLiteralClassifier_DecidesTheNullArm`: a PROCEDURE DIVISION
file that tests a `nonNumericLiteral` node's presence must decide `predefinedNull()` or be listed with its reason
(the existing decoder fact keyed on the figurative arm and could not see this classifier).

**Goldens.** `14.9.13-EVALUATE/pb1682_evaluate_null_subject_is_identifier` (accepted at 2002; four legs, values from
§8.4.3.10.4 GR2 and §14.9.13.4 GR4 a) 6.) and `pb1682-evaluate-null-subject-literal-object` (refused COBOL0869 at
2002, 2014, 2023). Rows: none claimed, none closed.

