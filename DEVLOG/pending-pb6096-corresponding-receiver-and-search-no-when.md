## PB6096: a second CORRESPONDING receiver and a SEARCH with no WHEN are refused by name

Both programs were already refused (the inventory rows 1985-FMT-VI.6.19.2 and 1985-FMT-VI.6.22.2 stayed CONFORM); the
diagnostic named the wrong mistake. Re-probe on this build before the change: `MOVE CORR G1 TO G2 G3.` drew COBOL0307
"A period may be missing", and `SEARCH E AT END DISPLAY "X" END-SEARCH.` drew the bare COBOL0001 "unexpected
'END-SEARCH'"; the sibling probes showed the same for `ADD CORR` and `SUBTRACT CORR` (Format 3 prints one receiver) and
a SEARCH or SEARCH ALL ended by its period, which read "The DISPLAY statement ends before this token" or "expected WHEN".

Fix, both in `CobolErrorStrategy.cs`, both COBOL2269 (the one statement-format-shape code the other SEARCH shapes use, so
no new code was spent): `CorrespondingReceiverMessage` finds the MOVE, ADD or SUBTRACT CORRESPONDING statement the parser
completed immediately before the token (the walk the SEARCH ALL second-WHEN arm uses) and, for an identifier or comma,
says the format names exactly one receiving item. A word that opens area A in the 1985 reference format keeps COBOL0307,
which is the correct reading there. `SearchFormatShapeMessage` gained a no-WHEN arm: the token is END-SEARCH or the
period, the nearest SEARCH has no WHEN phrase yet, and the ATN expected set holds WHEN, so a fault inside the AT END
statement (`AT END MOVE 1 TO.`) is not claimed.

Goldens (refused at 85, 2002, 2014, 2023): `pb6096-search-no-when`, `pb6096-search-all-no-when`,
`pb6096-move-corr-two-receivers`, `pb6096-add-corr-two-receivers`, `pb6096-subtract-corr-two-receivers`.
