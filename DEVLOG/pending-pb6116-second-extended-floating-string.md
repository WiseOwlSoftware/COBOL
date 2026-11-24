## PB6116: a second floating string of an extended editing sign control symbol is refused (SR27)

`PIC LL99FF EDITING L FOR NEGATIVE IS "-" EDITING F FOR NEGATIVE IS "-"` bound and showed `[ -0005]` for -5. §2023:13.18.40.3 SR27
allows one floating insertion string in character-string-1, and §2023:13.18.40.5 GR6 lists the extended editing sign control symbols
among the floating insertion symbols. The Table-10 walk gives floating roles to `+`, `-` and the currency symbol only, so a second
floating extended string never reached a rule.

Re-probe on this build reproduced the note. First attempt: give a floating extended string the floating-cs roles of Table 10. It refused
Annex D.24's `LLLL9.99F` (the fixed `F` is the cs-trailing role, which Table 10 refuses after a floating cs), so I reverted it. The fix
counts floating extended strings in `PictureComposition.Validate` beside SR24's extended count: a run of one extended character-1 that
only transparent symbols (simple insertion, the decimal point, a character-1) join to the earlier run of its kind continues that
string; any other floating extended run starts a new one, and a second is COBOL1934. `MarkFloating` and the new count read one
`IsEmbedded` predicate. `LL99FF`, `LL9.99FF`, `LL9LL`, `LL99LL`, `LLFF99` and `FFLL99` are refused; `LL99`, `LLLL9.99F`, `LL,LL9` and
`LLL.LL` stay legal. A second `+`, `-`, currency, `*` or `Z` string was already refused by the walk.

Goldens: `pb6116_picture_one_extended_floating_string_2023` (accepted, from 2023) and `pb6116-picture-two-extended-floating-strings`
(refused at 2023 with COBOL1934); unit theory `PictureCompositionTests.ASecondExtendedFloatingString_IsRefusedBySR27`. No inventory
row closed.

Review round 1 (R1-1): the SR27 count now also counts a string of Z, of *, and a floating +, - or currency string against a floating extended string, so `PIC LLZZ9` and `PIC LL$$99` are refused (COBOL1934). Unit theory rows added. R1-2 (a lone trailing floating extended string) is left as a lead.
