## Entry 3141 — 2026-11-04 23:13 PST — PB4703: a floating string's landing on a simple insertion symbol embedded in it is rendered and de-edited as the landing

kb/Work PB4703 was filed from the PB4621 review: `CobolEdit.DeEdit` misses a sign landed on an IS-form PICTURE EDITING
character-1 embedded in a FOR-form floating string (R1-1), and the upper bound `i < signEnd` of PB4621's fix had no witness
(R1-2). Re-probed on a Debug build, `PIC LLTLL9 EDITING L FOR NEGATIVE IS "-" EDITING T IS ":"` holding -555 is `  -555` and
de-edited to +555; R1-2 held (removing the bound left every test green).

The probe found the same root three more ways. `Render` marked a landing `placed[i] = true` and `Materialize` wrote the string
of the symbol AT that position, so a floating string that landed on an embedded symbol wrote the wrong symbol's string:
`PIC $$B$$9` with CURRENCY SIGN IS "USD" held 555 as `    $555` (and de-edited to 5), and a FOR literal landing on a wider
IS-form character-1 (`T IS "::"`) lost the sign of -555 (`  ::555`). `DeEdit` read a floating FOR string's blank negative
text (a FOR phrase naming only POSITIVE, SR12c) at every suppressed position, so `PIC LLL9 EDITING L FOR POSITIVE IS "+"`
holding +5 de-edited to -5. A blank landing wider than one space (`FOR NEGATIVE IS "(("`, positive defaulted) was matched
greedily at each suppressed position and put the walk out of step, so 555 in `LLTLL9` de-edited to 55.

Fix: `Render` now records WHICH symbol's string stands at a position (`placedBy[i]`, replacing the bool), and `Materialize`
writes the landed text of that symbol at the end of the embedded symbol's slot (the slot's other characters hold the
replacement space; GR14 still counts both widths, so the size is unchanged; a latitude choice recorded in
docs/CONFORMANCE.md). `DeEdit` reads that layout back through `LandingZones`/`LandedStringAt` over the one `FloatingZone`
extent, tells a blank negative text from suppression by the positive text never landing, reads a blank wide landing as the
string's whole suppressed prefix (`BlankLandingRun`), and sets `digitSeen` at a floating FOR or currency digit position.
Golden `2023/pb4703_deedit_for_floating_embedded_landing` and the refused-below `pb4703-deedit-for-floating-embedded-at-85`
(COBOL0900); unit rows in `CobolEditStoredSignTests` for each shape, and for R1-2 the masks `--.T9`, `++.T9` and `--.T99`
(a decimal point ends the string with no digit before T), mutation-checked: dropping `i < signEnd` fails three of the six
rows. No inventory row is claimed or closed.

The branch review of this change found two more (its R1-1 and R1-2, not the PB4621 pair above). A literal that begins with a
space (`L FOR NEGATIVE IS " -"`) was never recognised by `BlankLandingRun`, which compared a nonblank landed text at the first
nonspace character of the suppressed run instead of at the text's own first nonspace character; `PIC LLL9` holding -5 de-edited
to +5, a regression against the previous tree, and under EC-DATA-INCOMPATIBLE checking ended the run unit on an image `Format`
itself wrote. `LandsAt` now aligns the text by its leading spaces. And the floating `+`/`-` string, the arm beside the one fixed,
still wrote its sign across a wider embedded character-1's whole slot (`PIC --T--9 EDITING T IS "::"` holding -555 gave
`  --555`, two signs where rule 6 a) places "a single occurrence"). `Render` records `placedBy` at the two floating-sign
placements, `Materialize` writes the one sign at the end of the slot, and `DeEdit` reads it there; the determination in
docs/CONFORMANCE.md now says it covers every floating symbol. Unit theories and golden lines J1-J5 and K1-K4 cover both.

