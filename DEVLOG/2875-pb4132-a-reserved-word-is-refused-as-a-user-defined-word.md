## Entry 2875 — 2026-10-31 13:21 PDT — PB4132: a reserved word is refused as a user-defined word only where one could stand (a stray END-DISPLAY is COBOL0306, a misplaced RETRY COBOL2269)

**Re-probe.** At --std 2023 `MOVE 1 TO W-N END-DISPLAY.` drew `COBOL0901: 'END-DISPLAY' is a reserved word in COBOL-2023 and
cannot be used as a user-defined word`, and END-ACCEPT, END-SEND and END-RECEIVE the same; `READ F ADVANCING ON LOCK RETRY 3
TIMES.` drew COBOL0901 at RETRY. A probe build that printed the error strategy's own message beneath the re-code showed the
strategy had named both faults correctly: `[COBOL0306] … END-DISPLAY appears without a matching DISPLAY statement` and
`[COBOL2269] … The READ statement ends before this token`.

**Root.** The note blamed `reservedGatedWord` (a stray terminator "parses as a data-name"). Nothing parsed: the parse failed at the
word, and `CobolErrorListener.SyntaxError` re-coded EVERY syntax error on a reserved reservation-gated word to COBOL0901 after the
strategy had ranked its hints. ISO §8.3.2.1 1) ("Reserved words shall not be used as user-defined words or system-names") is broken
only by a USE as a user-defined word; where §8.9 reserves the word the token is its keyword.

**Fix.** The §8.9 arm moved into the strategy's one report path, `CobolErrorStrategy.ReservedWordUse`, ranked with the token's other
diagnoses. COBOL0901 is the report only where a user-defined word could stand at the token (`OffendingPosition.TakesAUserWord`: the
parse there takes an IDENTIFIER, the type the gate retypes a free word to; the construct closed just before it would, as in
`MOVE 1 TO W-N CONSTANT W-K`; or a period would let a paragraph header begin there, `STOP RUN CONSTANT.`), and never where the
token is a scope terminator no open statement owns written where a statement may end (`AStatementMayEndBefore`: a period, a
statement start or a terminator is expected; ISO §14.5.3.2). `DISPLAY END-RECEIVE.`, a DISPLAY still waiting for its operand, keeps
COBOL0901 (`user-word-end-receive`, which an earlier draft turned red, is the arm's witness). The recovery's
`IsReportedReservedOperand` reads the same decision instead of asking the parser a second time, and the listener now only drops a
second COBOL0901 at one token. The shared closed-construct prologue of `CarryForward` and `StatementThatEndedBefore` became
`OffendingPosition.ClosedJustBefore`, which the user-word test also uses. Everything is read from the ATN (expected sets, the
terminator map); there is no word list.

**Goldens and tests.** `cases/14.5.3.2-Explicit-scope-termination/pb4132-stray-end-display` (COBOL0306 at 2002, 2014, 2023) and
`cases/14.9.30-READ/pb4132-read-retry-after-advancing-on-lock` (COBOL2269 at 2002, 2014, 2023); the comment of
`pb331-read-two-from-one-lock-bracket`, which documented the misdiagnosis, now points at the RETRY case.
`ParseRecoveryTailTests` lost its `IsGatedWord` carve-out (it pinned COBOL0901 for the four gated terminators) and gained a sweep
of every gated terminator, read from the ATN, at every edition that reserves it. `ReservedWordPositionConformanceTests` gained the
RETRY rows and the arm that keeps COBOL0901 where a user word stands. `EditionReadDriftTests` records the moved
`ReservedUserWordViolation` read. Docs: DESIGN-frontend-grammar.md (the reservation gate and the parse-error path), DESIGN.md.

**Merge with main (PB4133, PB4134, PB4173 and PB3248's review R3-1).** Main had gained, in the same members, the unit-tail
discard (`_orphansItsTail` replaced `_constructEndedEarly`) and `IsReservedOperand`: PB4133's single-token deletion declines to
delete a token the missing element would let follow, except a reserved word written as the operand
(`PERFORM UNTIL RECEIVE W-X = 1 …`), which it deletes. Main's predicate asked the parser's `ReservedUserWordViolation`, the
listener's old test, which this branch had replaced by `ReservedWordUse`. Kept both: the report sets the tail-discard flag and
makes the §8.9 decision; the deletion, which decides before its report, asks `ReservedWordUse` of the token's own position
(`IsReservedOperand`), and both recoveries share the statement test (`BeginsNoStatementThatParses`). To let the decision be
asked before a report, it no longer reads the hint list: hint 13's test became `UnownedScopeTerminator`, which hint 13 and
`ReservedWordUse` share. So a word the report calls a misplaced keyword (a stray scope terminator, a phrase word where no
user-defined word can stand) is never deleted as a refused name. `ParseRecoveryTailTests` compiles through main's shared
`RecoverySources`, whose `Errors` gained the edition its gated-terminator sweep passes; its private compile is gone.
`EditionReadDriftTests` records the one read, in `ReservedWordUse`. The landed note's title, which named the refuted
mechanism (review lead R1-1), now names the listener's re-code.

**Rows.** None re-verdicted: every program involved was already refused; the landing names the true fault.

**Self-review.** Architecture: the §8.9 diagnosis is now one decision in the strategy's one report path, read by the report and
its recovery; the listener no longer overrides a ranked diagnosis. Duplication: the closed-construct prologue two members shared
is one member. Performance: the new reads run only on a syntax error at a reserved gated word. Silent failure: an expected set the
parse cannot compute keeps COBOL0901, the earlier behaviour.

**Gate.** `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` on 1546da903 (run 000001-20261031T174911Z:
Conformance 10,376, Unit 7,127 and Characterization 6 cases; Linux half (WSL clone of HEAD) GREEN). After the merge with main, the
same verdict on 0595f7edc (run 000003-20261031T183641Z: Conformance 10,305, Unit 7,153 and Characterization 6 cases; Linux half
(WSL clone of HEAD) GREEN).

**Review finisher (round 1: R1-1 blocking, R1-2 and R1-3 leads).** The branch was merged, with PB4206's branch under it
(`procedureName` now takes `reservedGatedWord` in every procedure-name slot), into a fresh finisher branch; three conflicts
(DESIGN-frontend-grammar, the `ReservationGateRewriter` summary, `ReservedWordPositionConformanceTests`) kept both sides.
R1-1 reproduced on that build: at --std 2023 a stray END-DISPLAY beginning a sentence (`MOVE 1 TO W-N.` then
`END-DISPLAY MOVE 2 TO W-K.`) drew `COBOL0307: cannot parse construct near 'MOVE'. A period may be missing at the end of the
previous sentence.`, where END-ADD drew COBOL0306 at the word; END-ACCEPT, END-SEND and END-RECEIVE the same, as the paragraph's
first sentence, the procedure division's first and after a SECTION header too. The header reading (`paragraphName` →
`procedureName` → `reservedGatedWord`) was the only reading the prediction had for the word, so it was taken and the parse died
at the next token; the period the message advised turned the word into a reserved paragraph-name. Fixed in the grammar:
`paragraphName` and `sectionName` carry `{procedureHeaderHere(DOT|SECTION)}?`, which takes a word §8.9 reserves here as a header's
name only where the header follows it (ISO §14.4.3: "A paragraph consists of a paragraph-name followed by a separator period").
Elsewhere the word fails at itself and `ReservedWordUse` decides: COBOL0306 for every stray terminator, COBOL0901 where the
header's user word stands (`CONSTANT MOVE 1 TO W-N.` at 2002, which also used to be COBOL0307 at MOVE). A written header
(`END-DISPLAY.`) still parses and draws the funnel's COBOL0901: it matches the paragraph format and no other. The sibling sweep
(every `reservedGatedWord` slot in the grammar) finds the two header slots the only ones at a sentence start. New golden
`cases/14.5.3.2-Explicit-scope-termination/pb4132-stray-end-display-sentence-start` (COBOL0306 at 2002, 2014, 2023). R1-3:
the gated-terminator theory re-ran the 2023 rows of the all-terminator sweep; the all-terminator sweep now also covers both
sentence starts at the default edition (`AStrayTerminatorBeginningASentence_DrawsExactlyOneDiagnostic`), and the gated sweep runs
all three positions at every OTHER edition that reserves the word (`RecoverySources.DefaultEdition` names the split). R1-2: the
generated `CobolLexer.IsReservationGated` summary (written by `scripts/gen-cobol-words.ps1`) no longer says every error on a gated
word is COBOL0901 from the listener; it names `ReservedWordUse`. `EditionReadDriftTests` records the two new word-status sites.
Self-review: the predicate costs one token lookup and one hash probe per header prediction; the section-header citation in its
summary was corrected to the §14.2.1 general format. Gate on 0eebca4b8: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating,
PB2515): GREEN` (run 000001-20261031T195428Z: Conformance 10,551, Unit 35,181 and Characterization 8 cases; Linux half (WSL clone
of HEAD) GREEN).

