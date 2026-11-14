## Entry 3605 — 2026-11-14 07:15 PST — The PICTURE clause's IS is a token written once, and the apostrophe is a COBOL-2002 quotation symbol (PB5213, PB5160)

Wave 20262029 group G: two lexer-rooted under-rejections from the 1985 refuter pass and code read, both re-probed on
this branch's build before the fix.

**PB5213.** `01 A PIC IS IS IS X(4) VALUE "WXYZ".` compiled and printed `WXYZ` at 85 and 2023: PICMODE's
`PIC_IS : 'IS' -> skip` dropped every IS, where the format `{PICTURE | PIC} IS character-string-1` (§2023:13.18.40.2,
§1985:VI.5.9.2) prints one optional IS. The same skip was a sibling bug: the CURRENCY SIGN clause's
`[ WITH PICTURE SYMBOL literal-8 ]` (§2023:12.3.7.2) prints no IS, yet `WITH PICTURE IS SYMBOL "#"` compiled (probed:
`EUR1.50`). The lexer no longer decides that syntax question: `PIC_IS`'s action `OnPictureIs` makes the first IS of the
region the IS token, which `pictureClause : PIC IS? PIC_STRING …` takes and `currencySignClause` refuses, and a second IS
the character-string (it types the token PIC_STRING and ends the mode), which the PICTURE rules refuse. An action, not a
lexer predicate, so the DFA cache holds. `PIC IS IS.` is now COBOL0808 at every edition, `PIC IS IS IS X(4)` COBOL5506
near X, `PICTURE IS SYMBOL` COBOL5506 at IS. Goldens in `13.18.40-PICTURE` (one accepted, two refused),
`12.3.7-SPECIAL-NAMES` and the 1985 `VI.5.9` folder; a lexer theory in `LexerTotalityDriftTests`. 1985-FMT-VI.5.9.2
PARTIAL to CONFORMS. DESIGN-conformance-suite.md no longer plans to move `PIC_IS` to the hidden channel (it is the IS
token now).

**PB5160.** `DISPLAY 'ABC''D'.` compiled at 85 and printed `ABC'D`. The 2002 text quotes the edge (§2002:F.2 item 3,
"Apostrophe as quotation symbol"; §1985:IV.4.2.2.2.1 delimits by "the separator quotation mark"), so a new construct
`apostrophe-quotation-symbol-2002` (COBOL0900, VCR row 7.72) is asked by one more arm of `LiteralScreen.GateFormat`
through one codec predicate, `CobolLiteral.IsApostropheDelimited`, which reads the opening delimiter the existing prefix
scan finds; no new token kinds. Every format and position is refused at 85 (probed: DISPLAY, MOVE, VALUE, level-88,
ALL, X'…'). Three 85 goldens' legs whose subject was the apostrophe moved to the new 2002 golden
`8.3.5-Separators/pb5160_apostrophe_quotation_symbol`; three 85 goldens that used one incidentally write the quotation
mark. None of the 459 NIST CCVS-85 programs writes one. 1985-FMT-IV.4.2.2.2.1.1 and 1985-SR-IV.4.2.1-5 DIVERGES to
CONFORMS. 1985 GAP 207 to 204.

**Found, not fixed (a lead).** Every literal-format gate is a token gate after text manipulation, so a literal a REPLACE
operand replaces before lexing is never screened: at 85 `DISPLAY 'QQ'` under `REPLACE =='QQ'== BY =="RR"==.` and
`DISPLAY X"5151"` under `REPLACE ==X"5151"== BY =="RR"==.` both print RR.

No diagnostic codes claimed (COBOL7180-7182 returned). Gate: see the branch's gate record.

