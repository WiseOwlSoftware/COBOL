- **D-B4 - a numeric literal, and the value of a numeric function, sent to a group store their DIGITS ONLY** (kb/Work
  PB3570, PB3613; owner decision 2026-10-23, on a point of implementor latitude). A MOVE to a group item is §2023:14.9.25.4 GR4's move, "treated exactly as if it were an
  alphanumeric to alphanumeric elementary move, except that there is no conversion of data from one form of internal
  representation to another": it preserves the sender's representation, and a numeric literal has none, so the standard
  does not say what `MOVE -456 TO G` stores. CLAUDE.md rule 1's precedence for implementor latitude is the standard,
  then GnuCOBOL, whose literal data holds the digits without the sign or the decimal point - and an elementary
  alphanumeric receiver already drops the sign by GR6 a) ("the operational sign is not moved"), so the group move now
  agrees with it. WiseOwl COBOL sends the digits of the literal's value, in the order written, with neither sign nor
  decimal point: `MOVE -456 TO G`, `MOVE +456 TO G` and `MOVE 456 TO G` store 456, `MOVE -4.56 TO G` stores 456,
  `MOVE -0.05 TO G` stores 005 (leading zeros are digits), and a floating-point literal sends the digits of its
  SIGNIFICAND as written, never its exponent nor the digits of its scaled value (`1.5E+3` sends 15, `12.0E+2` sends 120,
  `0.5E-1` sends 05, each measured against cobc 3.2.0; the same value written `0.05` sends 005, so the digits follow the
  notation, as GnuCOBOL's literal data does, and `1.0E+6144` does not become 6145 characters); the group then aligns left with space fill or truncation to the right
  (§2023:14.6.8.5). The same characters cross a CALL ... AS NESTED or INVOKE BY CONTENT argument into a plain-group formal
  (§2023:14.2.3 GR9, "otherwise, a MOVE statement"). This supersedes kb/Work PB1113's determination, which kept the literal's
  own characters, sign included. **The value of a numeric or integer FUNCTION is the same sender** (kb/Work PB3613): §2023:15.2 4)-5)
  give it "an operational sign" and no usage, and §2023:15.4.1 leaves a returned value's "characteristics and representation" to
  the implementor under native arithmetic, so §2023:14.9.25.4 GR4 has no internal representation of it to preserve, exactly as for
  a literal, and the one determination covers both: `MOVE FUNCTION MIN(N 3) TO G` (N = -15) stores 15 and `FUNCTION MIN(M 3)`
  (M = -1.5) stores 15, the same characters an elementary alphanumeric receiver takes (GR6 a); under DECIMAL-POINT IS COMMA
  the separator is no digit either, and a floating-point value sends its significand digits as a literal does). A call the
  compiler folds to a literal and the same call evaluated at run time send the same characters. GnuCOBOL sends the function's
  own internal field instead (`1u` for the S99 minimum, a 4-byte binary for INTEGER, `15` for ABS), which is no rule for a value
  the standard gives no representation, so the determination follows the literal, not the engine. The same characters cross
  a CALL ... AS NESTED, a Format-1 CALL and an INVOKE argument BY CONTENT into a plain-group formal; a function-identifier is a
  MOVE sender (§2023:14.9.25.4 GR1), so §2023:14.8.2.2 rule 2 admits it there (only an arithmetic expression, which is no MOVE sender, is
  refused; so is the same function, item or numeric literal ENCLOSED IN PARENTHESES, `(FUNCTION MIN(N 3))`, `(N)` or `(3)`, which is
  no longer an argument that "consists merely of a single identifier or literal", §2023:14.9.4.4 GR8, whatever tree the binder reduces it
  to: kb/Work PB3678, `pb3678_parenthesized_function_group_formal_refused`; kb/Work PB3760,
  `pb3760_parenthesized_literal_group_formal_refused` and its INVOKE and user-defined function siblings. A literal's sign is part of
  it, §2023:8.3.3.3.2 rule 2, so `-3` is still the literal and `(-3)` an expression). An ITEM sends its storage image (a signed DISPLAY item its overpunched sign), which D-B3's codec already gives.
  Written once, in `OperandText.NonElementaryMoveSender`, whose digit rule is `CobolNum.GroupDigits`; witnessed by
  `conformance:wiseowl/cases/14.9.25-MOVE/pb3570_numeric_literal_group_move`,
  `conformance:wiseowl/cases/14.9.25-MOVE/pb3570_floating_literal_group_move`,
  `conformance:wiseowl/cases/14.2-Procedure-division-structure/pb3570_numeric_literal_group_formal`,
  `conformance:wiseowl/cases/14.9.25-MOVE/pb3613_numeric_function_group_move`,
  `conformance:wiseowl/cases/14.9.25-MOVE/pb3613_numeric_function_group_move_comma` and
  `conformance:wiseowl/cases/14.2-Procedure-division-structure/pb3613_numeric_function_group_formal`.
  **A Format-1 CALL sends the SAME digits** (kb/Work PB3677): the activating element cannot see the formal there, and it used to
  cut an SDIDI value (a numeric function that returned one, or a STANDARD-DECIMAL expression) to the receiver-less 6 fraction
  digits before any formal could (`0.0001` sent `0000100`, the value 2 sent `2000000`). GR9 moves the argument "without
  conversion" and fills a numeric formal by a COMPUTE "without the ROUNDED phrase", so only the formal cuts the value: it
  crosses at its own exponent (`CobolArgAdapt.SdidiValue`), witnessed by
  `conformance:wiseowl/cases/14.2-Procedure-division-structure/pb3677_format1_sdidi_function_value`.
  **A numeric function's value reaches a numeric or character formal as its VALUE in a Format-1 CALL too** (kb/Work PB3969): §2023:15.4.1
  leaves "the characteristics and representation of the returned value" to the implementor under native arithmetic, so GR9's
  first regime has no image of it to move "without conversion", and the value is the sender exactly as in the second regime and
  in GR10 ("if the formal parameter is numeric, a COMPUTE statement without the ROUNDED phrase"; otherwise the MOVE of the value).
  `CALL "Q" USING BY CONTENT FUNCTION MAX(W 0)` with W = 1.25 shows `+1.25` in a PIC S9V99 formal (it showed `+0.00`, the leading
  positions of the value's 38-digit container), `FUNCTION INTEGER(W)` shows `1` in a PIC X(5) formal as `MOVE FUNCTION INTEGER(W)
  TO X5` does. Witnessed by `conformance:wiseowl/cases/14.2-Procedure-division-structure/pb3969_format1_numeric_function_value`.
