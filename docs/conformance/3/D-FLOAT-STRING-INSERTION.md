- **D-FLOAT-STRING-INSERTION — A floating string's landed text on a simple insertion symbol embedded in it (§2023:13.18.40.5 6), 6 a); §2023:13.18.40.4 14);
  kb/Work PB4703).** Rule 6 makes "the simple insertion editing symbols embedded in this string" part of a floating
  string, and rule 6 a) lands the replacement character(s) immediately before the first nonzero digit, so the landing
  can be an embedded `B`, `,`, `0`, `/` or an IS-form PICTURE EDITING character-1. What lands there is the FLOATING
  symbol's text (the currency string; literal-2 or literal-3 of an extended editing sign control symbol), written
  whole, and `DeEdit` reads it back, so `PIC LLTLL9 EDITING L FOR NEGATIVE IS "-" EDITING T IS ":"` holding -555 is
  `  -555` and de-edits to -555, and `PIC $$B$$9` with CURRENCY SIGN IS "USD" holding 555 is `  USD555`. The standard
  does not say how an embedded symbol wider than one character (SR9 allows a 50-character literal) shares its slot with
  a landed text of another width, only that GR14 counts both widths in the item's size. WiseOwl COBOL keeps the
  size and puts the landed text at the END of the slot, immediately before the next position, the slot's other
  characters holding the replacement space (`T IS "::"` with `L FOR NEGATIVE IS "("` gives `   (555`); the choice is
  the implementor's latitude, and no GnuCOBOL or IBM/Micro Focus behaviour for the PICTURE EDITING phrase was
  consulted (the phrase is a COBOL-2023 introduction, Annex E.3.3 item 19). The determination covers EVERY floating
  symbol, the floating `+` and `-` strings included: rule 6 a) places "a single occurrence" of the sign, so
  `PIC --T--9 EDITING T IS "::"` holding -555 is `   -555` (one sign at the end of T's two-character slot), not a sign
  repeated across the slot.
  A literal of the extended editing sign control symbol may begin with spaces (SR9 and SR12 limit its kind and
  width): `L FOR NEGATIVE IS " -"` lands the two characters, and the de-edit finds them by their first nonspace
  character, the leading space belonging to the suppressed run before it.
  A FOR phrase naming only POSITIVE has a blank negative text (SR12c): the sign of a de-edited floating string is
  then read from the positive text landing, since a blank cannot be told from the suppressed positions.
  Witnesses: `conformance:cases/13.18.40.5-Editing-rules/pb4703_deedit_for_floating_embedded_landing`, `CobolEditStoredSignTests`.
