---
cells: ["DOC-A.1-56", "**DISPLAY statement — conversion of data**, §2023:14.9.11.4 GR1, required + documented", "Each operand converts to its character image (kb/Work PB148 completed the register): a **fixed-point numeric item** (an elementary item of category numeric that is not a float, of ANY usage) displays in the form the compile option **`--numeric-display`** selects (kb/Work PB3505; owner decision PB3467, 2026-10-21: \"yes\" to GnuCOBOL's default form as the default and today's digit image behind an option). The form is a property of the COMPILED program, baked into it as `--sign-encoding` is, at every `--std`, and it reaches DISPLAY's GR1 conversion ONLY: a MOVE of the same item to an alphanumeric receiver is §2023:14.9.25.4 GR6's, a comparison's text is §2023:8.8.4.2's, and a record image is the item's storage, whatever the option says. **`decimal` (the DEFAULT)** shows the value as a reader writes it, GnuCOBOL 3.2.0's default configuration (`-std=default`, `cobol85`, `cobol2002`, `cobol2014` and `mf`): (a) the item's digit positions, zero-padded to the PICTURE's count (a BINARY-CAPACITY item, COMP-5 and BINARY-CHAR/-SHORT/-LONG/-DOUBLE, shows its CONTAINER's digits - 3, 5, 10 and 20 for 1, 2, 4 and 8 bytes, signed or unsigned, so a COMP-5 value beyond its PICTURE shows whole and never cut to the picture: this reverses the cut kb/Work R13 had taken from GnuCOBOL, whose 3.2.0 prints the whole value, measured 2026-10-22 - the cut stays the `digits` form's); (b) the decimal point at the scale's place, the PROGRAM's separator (a comma under DECIMAL-POINT IS COMMA, §2023:12.3.7.4 GR14 a); a scale past the digits (leading P) writes the point and the zeros the Ps stand for (`PIC PP9(3)` holding .00123 shows `.00123`, `PIC V9(3)` shows `.500`), a negative scale (trailing P) writes the zeros (`PIC 9(3)PP` holding 1200 shows `01200`); DISPLAY is none of the operations §2023:13.18.40.4 GR14 (symbol P) a) to d) that use the algebraic value, so how the Ps appear is this GR1 conversion's choice, and the GR6 transfer size is the sum of the converted sizes; (c) a SIGNED item (PIC S or a SIGN clause, any usage) shows its sign as a leading `+` or `-` character, `+` for zero, except that SIGN TRAILING SEPARATE shows it trailing, where the program put it (`001.50-`); an unsigned item shows none; (d) content a group MOVE deposited in a DISPLAY item's positions that is not a digit is carried through as stored (§2023:14.6.13.2: undefined), only the sign position being read, by the item's own SIGN and `--sign-encoding`. **`digits`** is the DIGIT IMAGE of every earlier revision: the digits as stored with no decimal point (it is implied), an unseparated sign over-punched onto a digit per `--sign-encoding` (A.1 items 177/178), a SIGN SEPARATE sign as its character, a signed non-DISPLAY item (COMP/COMP-3/COMP-5/BINARY-*) with a leading `-` when negative and no sign character otherwise, a P scaling's digits without its zeros, and a BinaryCapacity item's beyond-PICTURE value as the PICTURE-digit image, the value modulo 10^digits (kb/Work R13; the form IBM Enterprise COBOL's default `DISPSIGN(COMPAT)` gives, up to the sign), pinned by `pb148_display_forms` for the digit image, `pb3505_display_numeric_digits_form` and `pb3505_display_comp5_beyond_picture_digits`. The decimal form is pinned by `pb3505_display_numeric_decimal_form`, `pb3505_display_numeric_decimal_comma` and `pb3505_display_numeric_container_widths`, the measured table below by `NumericDisplayDecimalTests`, and the option's reach by `NumericDisplayFormDriftTests`. Where WiseOwl COBOL does NOT copy GnuCOBOL: a scaled COMP-5 (GnuCOBOL prints its unscaled integer, `0000000150` for 1.5 in `PIC 9(3)V99 COMP-5`, losing the scale; the decimal form shows `00000001.50`), the digits of invalid DISPLAY content (GnuCOBOL replaces the fraction's non-digits with zeros), a USAGE INDEX item (refused at compile time, §2023:13.18.60.3 SR10), the 100-digit form of FUNCTION PI and the other binary64 function images (a numeric function's returned value is a temporary item that DISPLAY shows by its description under `decimal`, item 92: `DISPLAY FUNCTION ABS(S)` shows `+00012.50` as GnuCOBOL does, and under `digits` item 92's literal form, `12.50`), and a float item (COMP-1/COMP-2/FLOAT-*), which keeps `CobolFloat.Display`'s shortest round-trip image (GnuCOBOL prints `0.09999999999999999` for 0.1 in FLOAT-LONG). Not numeric items, so unchanged: a numeric-edited item (its editing is the display), a group (its storage image), a numeric literal (as written) and the figurative ZERO (`0`). a **PICTURE-less float** (COMP-1/COMP-2/FLOAT-*) renders the invariant-culture shortest-round-trip image (`CobolFloat.Display` — the same image a function result uses, item 92); **alphanumeric/edited/national/boolean** items transfer their character content verbatim (the UTF-16 repertoire is the device repertoire; a boolean prints its '0'/'1' characters, and so does a boolean literal: `DISPLAY ALL B\"101\"` prints `101`, one occurrence under GR3, kb/Work PB243); a **numeric literal**, and the constant-name that substitutes one (§2023:13.10.4 GR1), displays **as written** (kb/Work PB1643): its sign as written, its trailing zeros and its decimal separator the PROGRAM's, so `DISPLAY 1,5` under DECIMAL-POINT IS COMMA prints `1,5` (§2023:12.3.7.4 GR14 a: the comma is \"the character written in numeric literals to represent the decimal separator\"; a contained program inherits the clause, §2023:12.3.4 GR1), and a floating-point literal prints its significand the same way (`1,5E3`); the one exception to \"as written\" is the SIGN OF A ZERO VALUE (kb/Work PB2508): a literal whose digits are all zero shows `+` for a written `-` (`DISPLAY -0` prints `+0`, `-0.0` prints `+0.0`, `-00`, `-0.00` and `-.0` print `+00`, `+0.00` and `+.0`, and `-0,0` under DECIMAL-POINT IS COMMA prints `+0,0`; so does a constant-name declared AS -0), because a zero is neither negative nor positive (GnuCOBOL 3.2.0 in `default` and `cobol2014`, measured), while `BoundNumericLiteral.Text`, the value, keeps the sign written (a floating-point literal cannot be a negative zero, §2023:8.3.3.3.3 SR4). The §2023:14.9.11.4 GR1 latitude is settled GnuCOBOL's way (precedence tiebreaker): GnuCOBOL 3.2.0 prints the literal as written, with the program's separator, in its `default`, `cobol85`, `cobol2002`, `cobol2014`, `xopen` and `mf` configurations, and only the vendor configurations `ibm`, `mvs`, `bs2000` and `acu` (`pretty-display: no`) print digits only; WiseOwl COBOL follows the first group. The value the numeric decoders read stays the canonical dot-decimal text (`BoundNumericLiteral.Text`); the image is `BoundNumericLiteral.Image`. Class **object, pointer and index** operands are compile-time rejections (`COBOL1694` — §2023:14.9.11.3 SR1 / §2023:13.18.60.3 SR10), never converted, and so is the word NULL, the predefined address and object reference (`COBOL2576` — §2023:8.4.3.10.3 SR1 admits it in no DISPLAY operand position; kb/Work PB1427). The variable-length-group format is A.1 item 57. ⛔ **A strongly-typed group with a class pointer/object leaf** (the only conforming spelling of such a group, §2023:13.18.60.3 SR14) is a legal identifier-1 — §2023:14.9.11.3 SR1 bars only an item OF class message-tag, object or pointer, and a strongly-typed group's class is its type-name (§2023:8.5.2.1) — and **displays its STORAGE image: each pointer leaf contributes its 8 positions holding the pointer's storage image of A.1 item 216 (NULL as eight zero positions, an address as its binary token — kb/Work PB1071; they were 8 reserved spaces before), each object leaf its 8 reserved placeholder positions as SPACES, never the managed reference** (kb/Work PB244). This is the byte extent D-SLOT reserves for the leaf in a byte-addressed storage area, so the group shows the same characters from a record struct as from a BASED/ADDRESS-OF cell, and `FUNCTION LENGTH` equals the displayed width. GnuCOBOL (the precedence tiebreaker) displays a group's raw storage, including a pointer's address bytes, and so now does WiseOwl COBOL, with the token of item 216 standing for the address. The same one-way image is a MOVE's sending group (§2023:14.9.25.4 GR4) and an intrinsic function's string argument (§2023:15.3 item 2 treats a strongly-typed group as class and category alphanumeric unless a function prohibits it, as MAX, MIN, ORD-MAX and ORD-MIN do; `FUNCTION UPPER-CASE(G)` reads these same characters — kb/Work PB1901, `conformance:cases/_unassigned/pb1901_strong_group_pointer_leaf_string_argument`). Comparison of two such groups and every read-back still refuse it, because the image is neither injective (an object leaf's placeholder is the same for every object) nor invertible (a pointer's token decodes to no reference).", "`conformance:cases/14.9.11-DISPLAY/pb148_display_forms`; `conformance:cases/14.9.11-DISPLAY/pb243_display_all_literal_kinds`; `conformance:cases/14.9.11-DISPLAY/comp5_display_beyond_picture`; `conformance:cases/14.9.11-DISPLAY/pb244_pointer_group_transfer`; `conformance:cases/_unassigned/pb1643_display_literal_decimal_comma`; `conformance:cases/_unassigned/pb1643_display_literal_comma_constant_invoke`; `conformance:cases/_unassigned/pb2508_display_negative_zero_literal`; `conformance:cases/_unassigned/pb2508_display_negative_zero_literal_comma`; `conformance:wiseowl/cases/14.9.11-DISPLAY/pb3505_display_numeric_decimal_form`; `conformance:wiseowl/cases/14.9.11-DISPLAY/pb3505_display_numeric_digits_form`; `conformance:wiseowl/cases/14.9.11-DISPLAY/pb3505_display_numeric_decimal_comma`; `conformance:wiseowl/cases/14.9.11-DISPLAY/pb3505_display_numeric_container_widths`; `conformance:wiseowl/cases/14.9.11-DISPLAY/pb3505_display_numeric_views`; `conformance:wiseowl/cases/14.9.11-DISPLAY/pb3505_display_comp5_beyond_picture_digits`"]
---
**A.1 item 56 - the numeric DISPLAY survey (kb/Work PB3505, measured 2026-10-22).** Each entry gives the declaration and its value, then GnuCOBOL 3.2.0's `cobc -x -std=default` output (run in WSL), WiseOwl COBOL's `--numeric-display decimal` and its `digits`.
A row marked `*` is where WiseOwl COBOL does not copy GnuCOBOL (the rule above).

```text
`PIC 9(5)V99`  =  12.5
      GnuCOBOL: `00012.50`   decimal: `00012.50`   digits: `0001250`
`PIC S9(5)V99`  =  +12.5 / -12.5
      GnuCOBOL: `+00012.50` / `-00012.50`   decimal: same   digits: `000125{` / `000125}`
`PIC S9(3)`  =  +5 / -5 / 0
      GnuCOBOL: `+005` / `-005` / `+000`   decimal: same   digits: `00E` / `00N` / `00{`
`PIC 9(3)`  =  7
      GnuCOBOL: `007`   decimal: `007`   digits: `007`
`PIC S9(3)V99 SIGN LEADING`  =  -1.5
      GnuCOBOL: `-001.50`   decimal: same   digits: `}0150`
`PIC S9(3)V99 SIGN LEADING SEPARATE`  =  -1.5
      GnuCOBOL: `-001.50`   decimal: same   digits: `-00150`
`PIC S9(3)V99 SIGN TRAILING SEPARATE`  =  -1.5 / +1.5
      GnuCOBOL: `001.50-` / `001.50+`   decimal: same   digits: `00150-` / `00150+`
`PIC 9(3)V99 COMP` / `COMP-3`  =  1.5
      GnuCOBOL: `001.50`   decimal: same   digits: `00150`
`PIC S9(3)V99 COMP` / `COMP-3`  =  -1.5 / +1.5
      GnuCOBOL: `-001.50` / `+001.50`   decimal: same   digits: `-00150` / `00150`
`PIC 9(3) COMP-5`  =  7
      GnuCOBOL: `00007`   decimal: `00007`   digits: `007`
`PIC 9(3) COMP-5`  =  12345
      GnuCOBOL: `12345`   decimal: `12345`   digits: `345`
`PIC S9(4) COMP-5`  =  -7
      GnuCOBOL: `-00007`   decimal: `-00007`   digits: `-0007`
`PIC 9(3)V99 COMP-5` *  =  1.5
      GnuCOBOL: `0000000150`   decimal: `00000001.50`   digits: `00150`
`BINARY-CHAR` / `-SHORT` / `-LONG`  =  -5 / -300 / -70000
      GnuCOBOL: `-005` / `-00300` / `-0000070000`   decimal: same   digits: same
`BINARY-DOUBLE`  =  -5000000000
      GnuCOBOL: `-00000000005000000000`   decimal: same   digits: `-0000000005000000000`
`BINARY-DOUBLE UNSIGNED`  =  18446744073709551615
      GnuCOBOL: `18446744073709551615`   decimal: same   digits: same
`PIC 9(3)PP`  =  1200
      GnuCOBOL: `01200`   decimal: `01200`   digits: `012`
`PIC S9(3)PP`  =  -1200
      GnuCOBOL: `-01200`   decimal: same   digits: `01K`
`PIC PP9(3)` / `PIC SPP9(3)`  =  .00123 / -.00123
      GnuCOBOL: `.00123` / `-.00123`   decimal: same   digits: `123` / `12L`
`PIC V9(3)` / `PIC 9V9(3)`  =  .5
      GnuCOBOL: `.500` / `0.500`   decimal: same   digits: `500` / `0500`
`PIC 9V9(8)`  =  .00000001
      GnuCOBOL: `0.00000001`   decimal: same   digits: `000000001`
`PIC S9(18)V9(9) COMP-3`  =  -123456789012345678.123456789
      GnuCOBOL: `-123456789012345678.123456789`   decimal: same   digits: `-123456789012345678123456789`
`PIC S9(3)V99`, DECIMAL-POINT IS COMMA  =  -1,5
      GnuCOBOL: `-001,50`   decimal: `-001,50`   digits: `0015}`
`PIC 9(3) USAGE NATIONAL` / `PIC S9(3)V9 USAGE NATIONAL`  =  123 / -12.5
      GnuCOBOL: `123` / `-012.5`   decimal: same   digits: `123` / `012N`
`PIC ZZ9.99` (numeric-edited)  =  12.5
      GnuCOBOL: ` 12.50`   decimal: ` 12.50`   digits: ` 12.50`
group of `9(3)V99` and `S9(3)`  =  1.5, -5
      GnuCOBOL: its storage characters   decimal: its storage characters   digits: same
numeric literal / `ZERO`  =  12.50 / -5 / 0.5
      GnuCOBOL: `12.50` / `-5` / `0.5` / `0`   decimal: same   digits: same
`COMP-1` / `COMP-2` / `FLOAT-LONG`  =  1.5 / -2.25 / 0.1
      GnuCOBOL: `1.5` / `-2.25` / `0.09999999999999999`   decimal: `1.5` / `-2.25` / `0.1`   digits: same
`FUNCTION NUMVAL("12.50")`, `FUNCTION ABS(S)` *  =  12.5
      GnuCOBOL: `00000012.5`, `+00012.50`   decimal: `12.50`, `12.50`   digits: same
`USAGE INDEX` *  =  3
      GnuCOBOL: `+000000003`   decimal: refused (COBOL1694)   digits: refused
```

GnuCOBOL's `-std=ibm` and `-std=mvs` print `0001250`, `0001250+` and `0001250-` (a trailing separate sign), IBM Enterprise COBOL 6.4 the digits with
the sign over-punched under `DISPSIGN(COMPAT)` and as a leading separate sign under `DISPSIGN(SEP)`, and Micro Focus a separate sign (an older
mirrored page; the current documentation did not load): the `digits` form is the closest of the three to the first two.
