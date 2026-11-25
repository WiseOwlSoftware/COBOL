## Entry 4064 — 2026-11-25 02:52 PST — PB6177: a floating string of an extended editing sign symbol after a digit position is refused (Table 10)

`PIC 99LL EDITING L FOR NEGATIVE IS "-"` bound clean and the sign was lost, while `PIC 99++` and `PIC 99$$` were refused COBOL1935.
§2023:13.18.40.5 GR6 lists the extended editing sign control symbols among the floating insertion symbols, and a floating string stands
for "the leading numeric character positions", so Table 10's blank cell for a digit position before a floating insertion symbol
applies to it. The Table-10 walk had given a floating extended string no role at all (`live = false`), so nothing was asked of it.

Fix in `PictureComposition.Precedence`: a floating extended string takes the floating currency role (left or right of the decimal point
position) and is asked what may precede it, using the union of the floating-currency and floating-sign rows. It does not enter the walk's
`seen` set, so what follows it stays unasked: Annex D.24's `LLLL9.99F` and the adjudicated `PIC LLLZZ9` stay legal (a first attempt in
PB6116 that gave it the full role refused both). `99LL`, `99LL9`, `ZZLL9` and `XXLL` are now COBOL1935; `LL99`, `$LL99`, `-LL99`,
`LLLZZ9` and `LLLL9.99F` are accepted.

R2-1 settled in kb/Work PB6177: SR27's list names '+', '-', currency, '*' and 'Z' strings only, so `LLZZ9` is not two SR27 strings (SR12 b
admits Z beside character-1); `LL99FF` and `LL9LL` are refused because GR6 makes a floating extended string a floating insertion string and
a character-string holds one.

Goldens: `pb6177_picture_floating_extended_after_insertion_2023` (`$LL99`, -5 gives `$ -05`) and `pb6177-picture-floating-extended-after-digit`
(COBOL1935 at 2023); unit theory `PictureCompositionTests.AFloatingExtendedString_FollowingADigitPositionIsRefusedByTable10`. No inventory row closed.

