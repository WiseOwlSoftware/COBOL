## A separator period where an operand is due names the statement for every verb, and a float-edited bound no longer displaces the 1985 construct gate

PB6119. Hint 0i's operand arm (PB6113) still left the bare COBOL0001 on `SET I TO.`, `COMPUTE N =.`, `IF N = .`, `EVALUATE.`, `PERFORM VARYING I FROM 1 BY.` and on twelve bare verbs
(`ACCEPT.`, `SEARCH.`, `DELETE.`, `RECEIVE.`, `SEND.`, `RESUME.`, `UNLOCK.`, `FREE.`, `VALIDATE.`, `RAISE.`, `INVOKE.`, `ALLOCATE.`). A sweep of all 59 statement verbs bare and of the operand shapes found two
causes. (1) The positions that take an arithmetic expression admit the grouping `(` (LPAREN), and the arm's symbol test, which reads a symbol in the set as "the element
before the token is complete", took it for completion; `AdmittedKinds.Refusal` now treats the grouping paren as one more alternative of the operand due and lists it
(`'('`). (2) A verb whose formats the parser chooses among (`statement` dispatcher) or weighs against a paragraph header (`paragraphDefinition`) fails before any statement rule is open, so the
period named no statement; `CobolErrorStrategy.StatementOpenAtAPeriod` names the verb the parser stands at when the compile edition reserves it (§2023:8.9, `ReservedWords.IsReservedAt`), because a reserved word cannot be the
paragraph-name the period would end (§2023:8.3.2.1 SR1). The "deliberate" list of PB6119 (FREE, VALIDATE, RAISE, INVOKE, ALLOCATE) held only where the edition does not reserve the verb: `UNLOCK.` at COBOL-85 and `RECEIVE.` at COBOL-2014 still
name no statement. Inline `PERFORM.` keeps COBOL0001. Five refusal goldens (`pb6119-set-index-no-sending-operand`, `-compute-no-expression`, `-if-no-condition-operand`, `-accept-bare-verb`,
`-unlock-bare-verb`) and a 15-row theory (`ABareVerbOrAnOperandStartingWithAParen_AtThePeriod_NamesTheStatement`) that asserts the statement named, which the older `ABareVerbSentence_IsAnIncompleteStatement_NeverNamedCobol0901`
could not (it accepts COBOL0001 as well as COBOL5506; review lead R1-3 of PB6113).

PB6147. `PictureAnalyzer.AnalyzeFloatEdited` forced `wideBounds` true when the construct is unavailable, which chose the digit count by the 2014 rule (significand alone, limit 36) while the message text was chosen by
edition (the 2002 sum): at `--std 85` `+9(40)E+99` drew COBOL1658 "counts 40 digit position(s) (significand 40 + exponent 2) — the limit is 1 to 36", and the bound error displaced the COBOL0900 construct gate (the gate reads
`PicInfo.IsFloatEdited`, which the error's recovery result lacks). `wideBounds` is now the register row alone and a separate flag skips the two bound checks where the form is gated; the shape checks stay. The golden
`g1985-pic-float-edited-bound-yields-to-construct-gate` is refused at 85 with the gate text.
