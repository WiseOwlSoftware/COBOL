## Entry 3288 — 2026-11-07 18:01 PST — PB4934, PB4938: the CALL arm of the message-tag gate, and the CALL storage-section rule as one value

**PB4934.** Below 2023 a Format-2 CALL passing a non-tag argument to a same-file message-tag formal drew COBOL1688 beside COBOL0900
(`callnest.cob --source-format free --std 2014`, reproduced on this build). `StatementBinder.NamesGatedMessageTag` had arms for data
references, functions, inline invocations and INVOKE (PB4893) but none for CALL. Fix: `CallBinder.CalleeSignatureOf` answers the callee
signature without binding (AS NESTED through the contained-program table, AS program-prototype-name-1 and the bare prototype word through
the prototype table), the literal-1 read through the new diagnostic-free `ExpressionBinder.PeekNonNumericLiteralOperand` (a first attempt with `EditionContext.Muted` was refused by `EditionMutedDriftTests`, which keeps Muted to the usage walk; `BindCall` reports a malformed literal when the statement binds); `CalleeSignature`
gained `SignatureItems` like `UserFunctionSignature`; the funnel has a `CallStatementContext` arm. The probe now draws COBOL0900 alone.
Witness: `AFormatTwoCallToAMessageTagFormal_IsTheGateAloneBelowTheIntroduction` (three spellings; 2014 the gate alone, 2023 COBOL1688 and no COBOL0900).

**PB4938.** `ScreenCallOperand` chose the clause and `ScreenSection` chose the section list by two independent edition tests.
`StorageSectionRule(Clause, Sections)` is now one value made by `Cobol85` or `Of2023(clause)`; `ScreenSection` takes it, and the CALL (two
sites) and INVOKE (four sites) callers pass it. The golden `pb4901-call-debug-register-at-85.json` now expects the citation and the list in
one substring, so a regression of the clause to a 2023 one turns it red.

No inventory row closes in either note.

