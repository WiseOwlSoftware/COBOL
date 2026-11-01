## PB4373: the margin-R warning states margin R as the position the format fixes, not the column the file holds it at

`MarginRCut.Margin` was the PHYSICAL column of margin R (`PhysicalLine.ColumnOf`, the unit a source location names, PB2983) but
its doc comment and the COBOL5788 message called it the 1-based position of the last position of the program-text area (72).
A fixed-form line that holds a tab before margin R is shorter in characters than in positions (the tab fills the positions to
its tab stop, DOC-A.1-157), so the note printed a margin other than 72.

**Re-probe (wave 20261808, group H).** A CALL line starting with a tab, the statement running past position 72: the warning read
"it ends at position 65 ... 'CONTENT WS-REPLY' (positions 66 to 81)" against a margin of 72. The same line without the tab read
72 and 73 to 88.

**Fix.** `MarginRCut(Column, LastProgramTextPosition, Text)` carries both: `Column` stays the location's unit (the cut's
`CutLocation`, `EndsAtMarginR`), `LastProgramTextPosition` is the position the reference format fixes
(`FixedFormLine.MarginR`, passed by `ReferenceFormatMap.CutOf`). `FirstCutPosition` and `LastCutPosition` continue from it. A
line whose column differs from its position now says so in the message ("a tab counts the positions it fills, so the cut stands
at column 66 of the line as the file holds it"), because the warning's location prints the column and the text prints the
position. Cite: ISO §6.3.1 (cite.py --check OK for "Margin R is immediately to the right of the rightmost character position of
the program-text area").

**Siblings found in the same file and fixed.** `LastCutPosition` added `Text.Length`, and the quote of the cut text cut at
`MaxDroppedShown` UTF-16 units, so a cut holding a supplementary-plane character stated one position too many and could quote
half a surrogate pair; both now count characters (`CharacterPositions`, PB1966).

**Tests.** Three in `MarginRCutNoteTests`: the tab line (position 72, positions 73 to 88, column 66), the supplementary-plane cut
(positions 73 to 84) and the quote shortened at a supplementary character. No conformance golden: the change is the wording of a
warning beside an error, which no golden's output carries. No inventory row changes verdict (`closes_rows: []`, with its reason).
