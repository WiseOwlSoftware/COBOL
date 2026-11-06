## PB4770: a foreign run ended by SECTION or DIVISION is named again

PB4706 added an `at.WritesAHeader` early return at the top of `CobolErrorStrategy.ClosedFormatResidue`, ahead of the
`at.ResidueRun` check, so a foreign clause run that a header word ended (`01 B PIC 9 VALEU SECTION.`,
`SELECT F1 ASIGN SECTION.`, `01 B PIC 9 VALEU 5 DIVISION.`) fell to the generic reports (COBOL0001, COBOL0312) instead of
naming the run, against PB4301's rule that a foreign run is refused whatever ended it. Re-probed: all three reproduced.

Fix: the `WritesAHeader` return now follows the run check, so an entry slot that writes a header still yields (the PB4706
goldens are unchanged) while a run ended by one is named: COBOL1941 at VALEU, COBOL1970 at ASIGN. Two refusal goldens
(`pb4770-foreign-run-ended-by-section-in-an-entry`, `...-in-file-control`) at every edition. No inventory row closed.
