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
