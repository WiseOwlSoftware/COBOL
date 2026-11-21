## Entry 3899 — 2026-11-21 05:48 PST — The 1985 debugging line in a unit without the mode is read as source with every screen source text gets (PB5990)

§1985:VI.7.3 requires a debugging line to leave a syntactically correct program "with or without" the lines taken as comment
lines. PB5212 checked only the direction where the mode is on (COBOL7061); PB5466 added a token-copy reading of the other
direction while this branch was open. That reading could not see what the lexer and the token screens decide (an unterminated
literal, literals with no separator, an operator with no space) nor a line inside a PICTURE or subscript region, so `DISPLAY "A"`
followed by a D line `) BROKEN (` or `"A""B"'C'` was caught only by the parse. Probed before the change: exit 0.

Fix, one mechanism for the direction: `DebuggingLineRewriter.Rewrite` returns the lines it made comment (`hiddenLines`); after a
clean parse at a 1985 dialect level `Frontend.ReportDebuggingLinesBreakingTheProgramAsSource` blanks the carriers of every
debugging line (`WithCarriersBlanked`, which replaces the single-line `WithCarrierBlanked`), lexes and parses the text again and runs
the same screens source text gets (`ReportTokenScreens`, `ReportArithmeticOperatorScreen`, the latter including the PB5989 condition
operators). A parse failure or a screen error ON a hidden line is COBOL7061 at the last hidden line before the first error; ONE code
and one reporter (`ReportIfDebuggingLinesBreak`) serve both directions. The branch's own COBOL7675 and the commentLines block of
PB5466's reader were deleted, and the branch's two duplicates of PB5466's goldens dropped. A misplaced D line before
OBJECT-COMPUTER stays COBOL7060 alone; a valid D line is not blamed for an error on a plain line. The pb1913 comment twin takes valid
D-line text (`IS`, `W-E(W-I`) since region lines are now read as source too. The closes_rows are empty: the 1985 inventory holds
store records (PB5094).

