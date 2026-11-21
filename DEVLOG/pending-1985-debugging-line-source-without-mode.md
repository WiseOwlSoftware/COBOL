## The 1985 debugging line in a unit without the mode is read as source too (PB5990)

§1985:VI.7.3 requires a debugging line to leave a syntactically correct program "with or without" the lines taken as comment
lines. PB5212 checked only the direction where the mode is on (COBOL7061). A unit without WITH DEBUGGING MODE hides its D lines
as comments and parsed clean however broken the lines were: `DISPLAY "A"` followed by a D line `) BROKEN (` compiled and ran at
`--std 85` (exit 0, probed on this build before the change).

Fix: `DebuggingLineRewriter.Rewrite` now also returns the lines it made comment (`hiddenLines`). After a clean parse at a 1985
dialect level, `Frontend.ReportDebuggingLinesBreakingTheProgramAsSource` blanks the carriers of every debugging line
(`WithCarriersBlanked`, which replaces the single-line `WithCarrierBlanked` and is lenient about an already blank carrier), lexes
and parses the text again, and a failure is COBOL7675 at the last hidden line before the first syntax error. A source with an
embedded precompiler block is not asked, as in the COBOL7061 direction. Region lines (inside a PICTURE) and a second unit without
the mode after a unit with it were probed and refused the same way. Goldens: pb5990-debugging-line-source-both-ways (accepted)
and pb5990-debugging-line-not-source-without-mode (refused at 85). Unit test:
HiddenLines_AreTheLinesOfAUnitWithoutTheMode_AndBlankingTheirCarriersLeavesNoMarker. The closes_rows are empty: the 1985 inventory
holds store records (PB5094).

Review follow-up: the as-source reading first only parsed, so a hidden D line holding an unterminated literal (COBOL5563) or
literals with no separator (COBOL2633) exited 0. `Frontend.ReportTokenScreens` is now the one pipeline of the token-level
screens, run by the main parse and by the as-source reading (lexer errors included); goldens pb5990-debugging-line-unterminated-
literal-as-source and pb5990-debugging-line-separator-as-source witness each. A misplaced D line before OBJECT-COMPUTER in a unit
without the mode drew COBOL7060 and COBOL7675 for one cause; it is now COBOL7060 alone (pb5990-misplaced-debugging-line-reported-once).
The arithmetic-operator separator screen needs the parse tree and is not part of the as-source reading.

Second review: the as-source reading also runs the parse-tree arithmetic-operator screen (COBOL2993, `Frontend.ReportArithmeticOperatorScreen`, one implementation called by the main parse and the as-source reading; `ParseCopyOf` now returns its tree), so the earlier stated gap is closed (golden pb5990-debugging-line-arithmetic-operator-as-source). A valid D line is no longer blamed for an error on a plain line: only errors on a hidden line count (golden pb5990-valid-debugging-line-not-blamed-for-a-plain-line-error, COBOL2633 alone).
