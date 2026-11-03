## PB4494: a verb alone in its sentence is faulted at its period, whichever verb, and a gated one no longer discards the sentences after it

**Re-probe.** At `--std 2023`, a paragraph holding `FREE.` drew `cannot parse construct near 'FREE'` at the verb; `DISPLAY.`,
`READ.`, `MOVE.`, `ADD.` and `STOP.` drew theirs at the period. The note called it cosmetic and blamed `BuildMessage`.

**Cause.** `BuildMessage` only words the message. A verb the edition reserves keeps a paragraph-header reading beside the
statement's in the `sentence*` loop's prediction. The statement dies at the period, the header is refused by `procedureHeaderHere`'s
predicate (PB4283), and ANTLR rewinds to the decision's start before it evaluates the predicate, so the failure stands at the verb. A
verb reserved at every edition has no header reading, the prediction commits to the statement, and the failure is the statement's own
at the period. The same failure had a second effect nobody had named: it is caught in the paragraph's rule, whose recovery resumes
past the paragraph, so `FREE.` followed by `MOVE.` and `MOVE.` reported one fault in place of three.

**Fix.** `CobolParserCoreBase.SentenceIsTheVerbsStatement` is the one decision (three copies of `IsBareVerbSentence &&
!NamedAsAProcedureElsewhere` became calls to it), and `IsIncompleteVerbSentence` adds that the verb alone is no statement
(`VerbAloneIsAStatement`, one parse of `statement` per verb type). Branch review R1-1 found the recovery faulting and consuming a
legal `GOBACK.`, `COMMIT.` or `ROLLBACK.` beside any other fault; `ACompleteVerbAlone_BesideAFault_DrawsNoDiagnostic` pins it. `CobolErrorStrategy.Sync` reports a gated bare-verb sentence at its period and
consumes the verb and the period, each of a run of them, ahead of the failing prediction; `ReportNoViableAlternative` moves the
report to the period for a failure that still reaches it. Both ask the same decision. `DESIGN-frontend-grammar.md`'s bare-verb
paragraph describes both.

**Goldens.** Rows for FREE, VALIDATE, ALLOCATE, RAISE and INVOKE in
`ABareVerbSentence_IsAnIncompleteStatement_NeverNamedCobol0901`; `ARunOfBareVerbSentences_IsFaultedEachAtItsPeriod` (three
sentences, three COBOL0001, each at its own period, gated and ungated mixed); the PB4283 refusal golden's `expect` reads `near '.'`.
No inventory row closes.
