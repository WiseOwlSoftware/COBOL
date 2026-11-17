**2026-11-17: landed (branch w20262102c).** The literal or number after `SOURCE-COMPUTER.` parses as the grammar's last
CONFIGURATION SECTION alternative, `unrecognizedClause DOT`, refused by name with COBOL1971. `AreaPlacementPass` no longer
takes that alternative for a paragraph header, so it draws no COBOL3734 beside it. Rerun at 85: `lit.cob` and `num2.cob` print
COBOL1971 alone. The witness `1985-SR-VI_4_3_3-1`, which pinned COBOL3734, now expects COBOL1971. Pinned by
`EditionCitationTests.LiteralComputerName_At85_IsOneVerdict`. Which message is right: COBOL1971 names what the parse found; a
computer-name message would be better still, but that is the closed-format pass's wording, not a second verdict.
