## PB4133, PB4134, PB4173: the parse-error recovery draws one diagnostic per fault: it declines a deletion the missing element would let follow, and discards a refused operand's tail to the unit's period or to where the structure resumes

**One root, three symptoms** (`CobolErrorStrategy.cs`, the recovery that follows a reported fault; wave 20261780, group F).

**PB4133, the deletion repaired the wrong element.** `IF DISPLAY "X" END-IF`, `EVALUATE W-N WHEN DISPLAY "X"` and `PERFORM UNTIL
DISPLAY "X"` (PBGF04, 07, 10) drew COBOL0001 and then a false COBOL2072, and `MERGE SW ON ASCENDING KEY USING F1 F2 GIVING F3` (PBGF32)
a second diagnostic at the GIVING (COBOL0001 when the note was filed, COBOL5506 since PB3428). The re-probe showed one cause in two
arms of ANTLR's dispatch: `SingleTokenDeletion` is reached from `Sync` (a decision) and from `RecoverInline` (a match), and both
delete a token when the one after it is what the rule expects. A statement written where a header operand is required satisfies that
test (`"X"` is a legal condition), and the repair made another program: an IF with an empty block, a key list that took F1 for its key
and met GIVING where USING was due. The note proposed suppressing the derived COBOL2072; that treats the symptom. The override now
declines when the token may follow the element the parser stands before (`TheMissingElementLetsItFollow`): the tokens that follow the
state after a matched token (ANTLR's own insertion test, so a token insertion would take is never deleted instead), after a block's
end, or after the whole rule when it has consumed nothing (the parser stood at the optional sign of an operand, inside a condition
that was missing as a whole). The PB911 empty-block test stays, one element further downstream. The missing element is the one fault
and the statement parses as written: `IF DISPLAY "X" ELSE …` also lost a false COBOL2072 at the ELSE and `WHEN DISPLAY "X" WHEN OTHER`
a false COBOL2073.

**PB4173, a refused operand orphans its phrase.** The note had no repro. Probing every COBOL5506 shape with a phrase word after the
operand found it: `OCCURS "A" TIMES` drew a false COBOL1941 at TIMES (INDEXED likewise), `RECORD CONTAINS "A" CHARACTERS`, `LINAGE IS
"A" LINES` and `BLOCK CONTAINS "A" RECORDS` a false COBOL1970, and `PERFORM "A" TIMES … END-PERFORM` a false COBOL0306 at the
END-PERFORM. The rule that failed returns, and the enclosing list meets the rest of the phrase as a clause or a statement of its own.
PB3843 had fixed the same thing for constructs closed early by discarding the faulted SENTENCE's tail; that is now one policy for
every fault whose tail the parser cannot place (`OrphansItsTail`: those three, and a COBOL5506 refusal that is not the missing token,
which the recovery conjures and goes on from), and the unit it discards to is any rule that ends at a separator period
(`EndsWithPeriod`, read from the ATN): the sentence and the entries. A refused operand that is a statement verb
(`MOVE DISPLAY "X" TO W-N`) is deliberately left alone: resuming at the verb is the right repair when the operand was only forgotten.

**PB4134, the discard ran past the structure.** The discard stops at the unit's period, but a unit may lack it, and the period it ran
to then belonged to the structure around it: after `IF … END-ADD` with no period, `END PROGRAM 123.` was eaten whole and its true
second fault lost (reproduced by switching the new stop off). `UnitTailDiscard.Resumes` now stops the discard, without consuming,
where some rule around the unit, completed, takes the next two tokens: a `ResimulationWalk.After` positioned at the end of each
enclosing rule in its real context, asked only for tokens in that rule's follow set. It is the ATN's follow and no list of words, so
`END PROGRAM`, the next program's header, `END DECLARATIVES`, the next `name SECTION` and the `PROCEDURE DIVISION` after an entry
without its period are all resumption points. Two tokens, because END alone is the AT END phrase. A word and the period after it is
never one when the unit can hold the word: the last operand and the period of the sentence, and a paragraph header, are the same
two tokens and the parser knows no Area A, so a missing period before a paragraph header still costs the header (pinned). The
note's "next paragraph header" is therefore not a stop, and the reason is in `DESIGN-frontend-grammar.md` section 3.11.

**Tests and goldens.** `ParseRecoveryDeletionTests` (the four rows, their siblings, a second fault after the declined deletion, the
repeated TO / USING / THEN still deleted), `ParseRecoveryUnitTailTests` (nine refused operands in entries and statements, the next
entry and sentence parsed afresh, the missing token not discarding, the five resumption points, a paragraph header, and a drift test
that reads the period-terminated units from the ATN), and every row of `GeneralFormatViolationTests` now asserts exactly one
diagnostic. Conformance negatives `pb4133-if-condition-omitted`, `pb4173-occurs-alphanumeric-literal-then-times` and
`pb4134-fault-before-a-missing-period-then-a-malformed-end-marker` (the last expects the second fault, the one that was lost).
`RecoverySources` is the one program shape the recovery tests share. Local: the whole Unit population ran (38,807 of 38,809; the two
are the external-corpus censuses, absent from a fresh worktree) and the whole Conformance population (19,988 of 19,988).

**Not fixed here, left as leads** (each registered in the report): any error inside a NESTED program draws a false COBOL0001 at the
host's END PROGRAM (adaptive prediction at `nestedProgram*` chooses the exit, not the recovery); `OCCURS 1 TO "A" TIMES` is reported
as COBOL1941 at TO where the refused operand should be COBOL5506; a refused operand that is a verb (`MOVE DISPLAY "X" TO W-N`,
`SEARCH WHEN DISPLAY "X" END-SEARCH`) cascades; `COMPUTE W-N = ("A" + 1) * 2` names the `+` ("expected `)`") instead of the literal.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN — Conformance 843/19,988 · Unit 6,035/38,809 · Characterization 3/36 cases ran · Linux half (WSL clone of HEAD) GREEN` (run `000001-20261031T024053Z`, head fd448a47b). Semgrep `verify.py`: PASS (no rule's count rose; `raw-diagnostic-code-literal` fell 225 to 216).

**Self-review.** Architecture: one override for both arms of the deletion; one policy and one unit definition for the tail discard,
read from the ATN, with a drift test; `ResimulationWalk.After` reuses the walk rather than a second follow computation. Full code: the
rule cited is §5.2.6.2 / §5.2.6.3 for the required operand and §14.5.3.2 for the construct closed early; the stop looks at two tokens
because a one-token follow takes `END` of AT END. Performance: all of it runs on the error path; the ATN scans are cached per rule.
Duplication: `EndsWithPeriod` replaces the sentence-only test; the PB3843 flag is the same flag, renamed. Silent failure: a unit that
cannot be found leaves the base recovery in place.

**Merge with main (finisher, CONFLICT).** Main gained PB3248's reserved-operand consume in the same `ConsumeUntil`: a
reservation-gated word the last report named as an operand is consumed before the resynchronization asks its set, unless
the statement it would begin parses. Both are kept. `BeginUnitTailDiscard` runs first, because it resets the fault's flag.
Main's consume runs next, against the effective set, and then the loop with its `Resumes` stop. A discard's set is the
period alone, which is never such a word, so the two never decide the same token, and outside a discard the method is
exactly main's. Review leads answered: R1-2, `ParseRecoveryTailTests` now compiles through `RecoverySources` (its private
copy of the compile is gone); R1-3, PB4134's `closes_rows_reason` now says the landing restores a lost true diagnostic.
R1-1 (an entry without its period discards the next named entry) stays open as a lead. A pair test over the entry's ATN
cannot tell the next entry's level-number and data-name from the entry's own tail: `OCCURS 5 STEP 2` (STEP is a
predicated `cobolWord`) and `EDITING literal valueClauseOperand` both hold an integer then a word.

**Review R3-1 (finisher): PB4133's deletion decline met PB3248's reserved operand.** On main,
`PERFORM UNTIL RECEIVE W-X = 1 DISPLAY "X" END-PERFORM.` drew one COBOL0901, because ANTLR's single-token deletion
dropped RECEIVE and W-X stood in the condition. The branch declines to delete a token that may follow the missing
element (PB4133), and a reserved verb may follow a missing condition, as an inline PERFORM's first statement. So the
branch kept RECEIVE and lost the PERFORM's reading, which drew a false COBOL5506 "expected TIMES" at DISPLAY. The same
happened with SEND and VALIDATE, after OR, and with WITH TEST AFTER. I bisected it with environment toggles on a
scratch build: neither the unit-tail discard nor its `Resumes` stop matters, only the decline. The decline now yields
where the token is a reserved word written as an operand. `IsReservedOperand` is the one predicate PB3248's
resynchronization and the deletion share: a §8.9 violation whose statement reading does not parse. Five rows in
`ReservedWordPositionConformanceTests.AReservedVerbWrittenAsAnOperand_DrawsOneDiagnostic` pin the case, and
`EditionReadDriftTests` records the moved read.
