## A reserved verb written as an operand is one COBOL0901, a verb that begins the next statement is not, and the next entry of a faulted one is parsed afresh (PB4208, PB4218, PB4207, PB4214)

Four notes with one root, the error strategy (`CobolErrorStrategy`, `CobolParserCoreBase`), fixed together in harm order.

**PB4208, the word class.** `MOVE CONTINUE TO W-X.` drew COBOL0001 and a COBOL2269 at TO, `MOVE EVALUATE TO W-X.` three diagnostics: the §8.9 decision
(`ReservedUserWordViolation`) answered only for a reservation-GATED token, so a verb reserved at every edition was never named, and its cascade (a verb is in
every statement-level recovery set) was the one PB3248 had removed for RECEIVE and SEND. The class is now a gated word or a verb, a token `statement` can begin
with, read off the ATN (`StatementOpeners`); `MOVE TO W-X.` (a reserved word that begins no statement) keeps its own diagnosis. A sweep of 55 verbs in 13
operand slots at 85, 2002 and 2023 (scratch `sweep.py`, a before and after build side by side) went from 528 of 648 cells not one COBOL0901 at 2023 to 74 of
756, none of the 74 a regression but the PB4207 reading below. The first conformance run found the sibling: `PROPERTY WITH NO GET SET.` drew COBOL0901
instead of COBOL1941, because a value clause before the verb could still take a name and the closed format's own refusal lost; `ReservedWordUse` now
declines a word where `ClosedFormatResidue` names a closed format's clause list.

**PB4207, one decision for the report and every recovery.** The report named the wrong cause (the listener no longer re-codes since PB4132; the strategy's
`ReservedWordUse` did not ask the question only the recovery asked). `ReservedWordUse` now also asks `ReadStatementAt(offendingToken)` (from the
offending token, never the lookahead: a failed prediction reports a token the parser has not reached), and the recovery's second copy of the question is
deleted. `MOVE W-X TO` then `RECEIVE FROM W-X GIVING W-Y W-N` is the ordinary `unexpected 'RECEIVE'`. Consequence, deliberate and measured: `DISPLAY GOBACK.`
(a verb that is a complete statement alone) is the next statement, as its recovery already read it. **Review round 1 (R1-1, blocking):** the statement
question was "does it parse completely", so a correct verb beginning a next statement with an operand missing of its own (`MOVE W-X TO` then `ADD 1 TO.`,
`DISPLAY W-Y UPON.`) was called a misused name and consumed as the MOVE's receiver, and the second statement's fault was lost, for all ~55 verbs. The
reading is now three-valued (`ReadStatementAt`: None, Complete, CutShort, the last being refused only by the sentence's end after the verb and one token or
more), and a CutShort reading is the next statement unless the word read as a name carries the whole sentence to its period
(`OffendingPosition.NameCarriesTheSentence`): the reading that goes further is named. `DISPLAY RECEIVE W-X.` and `MOVE W-X TO MOVE W-Y.` stay COBOL0901
(both are whole sentences as names); `SEND TO W-X.` after an incomplete MOVE, the old "control" row, is now the next statement (a SEND cut short). R1-2
(a doc comment of `facilityWord` the insertion had cut in two) repaired.

**PB4218, single-token substitution.** `PERFORM UNTIL RECEIVE = 1 DISPLAY "X" END-PERFORM.` drew a false COBOL5506 "expected TIMES": nothing remains of the
condition once the word is deleted, and the resynchronization stopped at the `1`. The re-probe traced it to a `Sync` mismatch, and the note's fix direction
held: after the report the word is retyped IDENTIFIER (`ReadAsUserWord`) in `SingleTokenDeletion` (Sync and RecoverInline), and in `Report` for a prediction
that died at the word ahead of the parser (`PERFORM VARYING RECEIVE FROM …`), only where the name lets the source go on (`NameLetsTheSourceContinue`, a walk
that takes an IDENTIFIER and then the next token); `MOVE W-X TO` then `SEND TO W-X.` keeps PB3248's consumption. The repair is the pass's own: the version
matrix's `user-word-as-2002` permissive row failed because the gate retypes keyword tokens between passes and a repair left standing hid them, so
`RestoreWordsReadAsNames` runs after every pass of `ParseToFixpoint`.

**PB4214, the next entry resumes.** The note's described pair test no longer existed on main: `UnitTailDiscard.Resumes` resumed only at structure around the
unit, so the entry after a faulted entry without its period was eaten to ITS period and its fault lost. A unit that opens with one token type (`SoleOpener`,
the level-number; a sentence cannot, its next statement begins with a verb it may hold) resumes at that token when it and the two after it are what the unit's
own follow takes, with the error productions left out (`ResimulationWalk.WithoutErrorProductions`, since ANTLR's closure is not overridable): the three-token test
the note named. `OCCURS 2 STEP 3` and `2 STEP W-STEP` do not resume; `01 Y PIK 9.` (a head the list does not take) is eaten as before.

Goldens: `ReservedWordPositionConformanceTests` (23 rows added to the operand theory, 3 to the next-statement theory, which now asserts the code), four
`ParseRecoveryUnitTailTests`, three refusals in `conformance-suite/cases/8.3.2-COBOL-words/` (pb4207, pb4208, pb4218), `EditionReadDriftTests` records the new
word-status read `ReservedWord`. Self-review (architecture, full code, performance, duplication): one decision under `ReservedUserWordViolation` and
`ReservedGatedWord`, the second statement-parse question deleted, the new parses run only on the error path for a reserved verb or gated word. No diagnostic
code used. Gate: see the landing's record.
