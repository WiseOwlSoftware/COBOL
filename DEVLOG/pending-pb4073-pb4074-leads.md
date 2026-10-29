## PB4073 and PB4074: two leads of the PB1092 wave filed

The review of wave 20261755 (PB1092, PB1397: the one source-text decoder) was CLEAN, with two leads. PB4073 records
that `Frontend.Parse`'s summary still names only a fatal syntax error as its null return, though a main source whose
byte-order mark names an encoding its content is not well-formed in (COBOL5434) now returns null before any parse.
PB4074 records that `CopyProcessor` takes an optional diagnostics bag. Every error it reports through
`_diagnostics?` therefore vanishes for a caller that builds it without one, which now includes PB1092's
malformed-library-text COBOL5434. The only such caller, the line-map replay, runs after a pass that already reported,
but nothing enforces that.
