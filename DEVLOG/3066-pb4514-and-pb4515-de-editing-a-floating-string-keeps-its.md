## Entry 3066 — 2026-11-03 17:56 PST — PB4514 and PB4515: de-editing a floating '+' string keeps its minus, and an all-suppressed picture tests the stored digits

Two defects of `CobolEdit.cs` from the 3,000-program fuzz batch (PB4506), both wrong answers through MOVE.

**PB4514.** `PIC ++++9.99` holding `-1234.50`, MOVEd to `S9(5)V99`, gave `+1234.50`. `DeEdit` recovered the sign of a
floating string with `minus > 1 && image.Contains('-')`, a scan of the whole image gated on the mask having a floating
`-`, so a floating `+` string (which lands `-` for a negative value, Table 9 of 13.18.40.5) was never read. The fix is the
structural one: the scan is gone and the sign is read AT the digit position of a floating `+` or `-` member, where it
landed (rule 6 a). That also removes the old scan's known weakness, a `-` belonging to a currency string or an editing
literal read as the item's sign. `DeEditFloat` already read the sign by position and needed nothing. Rule:
14.9.25.4 6) d) 1 ("de-editing establishes the operand's numeric value, which may be signed").

**PB4515.** `MOVE 1000 TO PIC ***.**` gave `***.00` and `ZZZ.ZZ` gave `   .00`. Rule 7 b) (and 6 b) for an all-floating
picture) say the result for a zero value is all asterisks with the point kept, or all spaces. The whole-field test in
`Render` read the pre-truncation `scaled` value, while PB566's BLANK WHEN ZERO guard a hundred lines above reads the
stored digit string. Both now read the stored digit string, which is the value 14.6.8.2 5) leaves in the item.
`++++.++` and `----.--` share the test and are fixed with it.

**Re-probe.** Both notes' repros reproduced on this build before the change and print `-01234.50` and
`***.**` / six spaces after it.

**Goldens.** `13.18.40.5-Editing-rules/pb4514_deedit_floating_plus_sign` (eight legs, floating `+` and `-`, negative,
positive, zero, small magnitude) and `pb4515_zero_suppress_stored_value_zero` (truncating-to-zero legs for `*`, `Z`,
BLANK WHEN ZERO, floating `+` and `-`, with nonzero controls). Both accept from COBOL-85, where the editing rules
exist, so there is no refused-below program. Unit theories in `CobolEditStoredSignTests` pin the same values at the
`Format` and `DeEdit` seam, including a comma insertion inside an all-`*` picture.

**Review round 1.** The reviewer (R1-1, blocking) showed the position-based read missed one landing spot: a PICTURE
EDITING character-1 (IS form) embedded in the floating string, where rule 6 a) lands the sign (`PIC --T--9 EDITING T IS ":"`
holding -555 is `  -555`). The deleted whole-image scan used to catch that for a floating `-` string, so it was a
regression, and the floating `+` string had the same hole. `DeEdit`'s non-floating character-1 arm now reads a `-` that
is not the rule's own literal as the landed sign when the mask has a floating `+`/`-` string. New golden
`pb4514_deedit_embedded_char1_sign` (accept 2023, five legs) with its refused-below program, and a round-trip unit
theory. R1-2 (a non-verbatim quote) and R1-3 (a Table 9 comment) were fixed in the files already open.

**Review round 2.** R2-1 (blocking): the round-1 arm refused a `-` that matches the rule's own literal, so an IS-form
character-1 whose literal IS `-` (`PIC --T--9 EDITING T IS "-"`, -555 is `  -555`) still lost its landed sign (the
`++T++9` form lost it on main too). The two are the same character and are told apart by position, as rule 6 a)
orders the string: positions before the first nonzero digit hold only spaces or the landed sign, so `DeEdit` keeps a
`digitSeen` flag read at the digit positions and takes a `-` at an embedded character-1 as the sign unless a digit has
already been read AND the `-` matches the literal. Golden cases F to I added to `pb4514_deedit_embedded_char1_sign`
(literal `-`, with and without a preceding digit, both string forms) and the unit theory gained a literal argument.
R2-2 (the refused-below program named a nonexistent `2023/` path) was corrected in the file.

**Register.** Both notes landed with no inventory rows claimed or closed.

