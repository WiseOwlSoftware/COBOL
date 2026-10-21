# Differential fuzz — triage report

Programs: 5599 · judged: 5599 · usable: 3348 (60% of judged; 1635 of them after a repair round) · the model's failures: 2251 · GnuCOBOL alone refused (THEY_REJECT leads): 176
Model seconds per program (median): 16

| Class | Count |
|---|---|
| BOTH_REJECT | 2249 |
| OUTPUT_DIFFERS | 1141 |
| AGREE | 1023 |
| WE_REJECT | 950 |
| THEY_REJECT | 176 |
| THEY_FAIL_RUN | 41 |
| WE_HANG | 8 |
| EXIT_DIFFERS | 8 |
| NO_PROGRAM | 2 |
| WE_FAIL_RUN | 1 |

⛔ TRIAGE RULE: the spec decides (CLAUDE.md rule 1). GnuCOBOL is a net: an OUTPUT_DIFFERS row says the
compilers disagree, not which is right, and a WE_REJECT row is ours only where the construct is ISO. A
confirmed defect becomes a `kb/Work/` note (search the register first); a confirmed program becomes a
suite case with its expected output derived from the standard, never copied from either compiler.

## Survivors: 2325 programs in 378 signatures

### `WE_REJECT:COBOLNET0900` × 132 — first: fz00017 (clause 14.9.51 WRITE statement)
- programs: fz00017, fz00058, fz00109, fz00108, fz00131, fz00139, fz00140, fz00158, fz00487, fz00488, fz00525, fz00588 …
- source: `programs/fz00017.cob`
- WiseOwl: `fz00017.cob(7,9): error COBOLNET0900: ORGANIZATION LINE SEQUENTIAL requires COBOL-2023 (targeting COBOL-2014) — the LINE SEQUENTIAL phrase of the ORGANIZATION clause (ISO §12.4.5.10.3 GR2; §9.1.7.2 splits sequential files into record- and line-delimited types)`

### `WE_REJECT:COBOLNET1625` × 85 — first: fz00034 (clause 15.55 LOG function)
- programs: fz00034, fz00056, fz00214, fz00293, fz00407, fz00500, fz00637, fz00710, fz00809, fz00956, fz01025, fz01440 …
- source: `programs/fz00034.cob`
- WiseOwl: `fz00034.cob(5,1): error COBOLNET1625: data item 'WS-ARG': the numeric literal 2.718281828 in the VALUE clause is not a permissible value in the range the PICTURE indicates — it is not representable without truncation of leading or trailing nonzero digits (ISO §13.18.63.3 SR2)`

### `WE_REJECT:COBOLNET1627` × 79 — first: fz00025 (clause 15.71 ORD-MAX function)
- programs: fz00025, fz00039, fz00161, fz00279, fz00280, fz00281, fz00331, fz00343, fz00344, fz00664, fz00754, fz00777 …
- source: `programs/fz00025.cob`
- WiseOwl: `fz00025.cob(45,1): error COBOLNET1627: FUNCTION ORD-MAX argument-2 is of class numeric, which cannot agree with the earlier arguments; ISO §15.71.3 r3 requires all arguments to be of the same class. --permissive accepts it as a coercion extension`

### `WE_REJECT:COBOLNET0898` × 62 — first: fz00004 (clause 15.85 STANDARD-COMPARE function)
- programs: fz00004, fz00008, fz00300, fz00328, fz00429, fz00480, fz00618, fz00619, fz00620, fz00628, fz00629, fz00780 …
- source: `programs/fz00004.cob`
- WiseOwl: `fz00004.cob(16,1): error COBOLNET0898: data item 'WS-K': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET2993` × 60 — first: fz00147 (clause 8.8.1.5.3 Arithmetic expressions > Standard-decimal arithmetic > Basic arithmetic operations in standard-decimal arithmetic)
- programs: fz00147, fz00148, fz00456, fz00463, fz00633, fz00634, fz00769, fz00922, fz00923, fz01231, fz01232, fz01375 …
- source: `programs/fz00147.cob`
- WiseOwl: `fz00147.cob(32,18): error COBOLNET2993: the unary arithmetic operator '-' is not immediately followed by a separator space or a left parenthesis — ISO §8.7.1 requires it to be immediately preceded and followed by a separator space or a left parenthesis; write a space after it`

### `WE_REJECT:COBOLNET1740` × 46 — first: fz00070 (clause 8.8.4.2.9 Conditional expressions > Simple relation conditions > Comparison of national operands)
- programs: fz00070, fz00083, fz00306, fz00427, fz00430, fz00598, fz00726, fz00798, fz00824, fz00843, fz00844, fz00846 …
- source: `programs/fz00070.cob`
- WiseOwl: `fz00070.cob(8,1): error COBOLNET1740: data item 'WS-ND': the VALUE alphanumeric literal (5 characters) exceeds the item's 4 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET1517,COBOLNET1627` × 38 — first: fz00167 (clause 15.3.1.6 Format arguments to international date and time functions > Week date formats)
- programs: fz00167, fz00284, fz00492, fz00493, fz00756, fz00875, fz00873, fz00941, fz01339, fz01531, fz01649, fz01708 …
- source: `programs/fz00167.cob`
- WiseOwl: `fz00167.cob(12,1): error COBOLNET1627: FUNCTION FORMATTED-DATE argument-2 is of class alphanumeric; ISO §15.3 requires class numeric (§15.39.3 r1/r3). --permissive accepts it as a coercion extension`

### `WE_REJECT:COBOLNET0881` × 37 — first: fz00006 (clause 8.8.4.2.5 Conditional expressions > Simple relation conditions > Comparison of a numeric integer operand with an operand of class alphanumeric or national)
- programs: fz00006, fz00007, fz00069, fz00459, fz00781, fz00782, fz01485, fz01511, fz01555, fz01556, fz01594, fz01617 …
- source: `programs/fz00006.cob`
- WiseOwl: `fz00006.cob(7,1): error COBOLNET0881: data item 'WS-NATIONAL': USAGE NATIONAL may not be specified with an alphabetic or alphanumeric PICTURE (X(5)) — it admits boolean, national, national-edited, numeric, and numeric-edited pictures only (ISO §13.18.60.3 SR12; §13.18.40.3 SR30)`

### `WE_REJECT:COBOLNET1517` × 29 — first: fz00184 (clause 15.40 FORMATTED-DATETIME function)
- programs: fz00184, fz00362, fz00387, fz00958, fz01134, fz01136, fz01326, fz02344, fz02345, fz03118, fz03279, fz03280 …
- source: `programs/fz00184.cob`
- WiseOwl: `fz00184.cob(23,1): error COBOLNET1517: FUNCTION FORMATTED-DATETIME argument-1 shall be a literal date/time format (ISO §15 — the FORMATTED-*/INTEGER-OF-FORMATTED-DATE/SECONDS-FROM-FORMATTED-TIME/TEST-FORMATTED-DATETIME format is a literal)`

### `WE_REJECT:COBOLNET0819` × 24 — first: fz00329 (clause 13.18.44 REDEFINES clause)
- programs: fz00329, fz00601, fz00613, fz01095, fz01096, fz01164, fz01179, fz01869, fz02643, fz02645, fz02884, fz02970 …
- source: `programs/fz00329.cob`
- WiseOwl: `fz00329.cob(24,1): error COBOLNET0819: MOVE … TO WS-REDEF-A: MOVE is invalid — a noninteger numeric sending operand does not move to an alphabetic, alphanumeric or alphanumeric-edited receiver (ISO §14.9.25.3 SR10, Table 16) (a NUMERIC-typed function is the Noninteger sender, §15.2 item 4 / §8.4.3.2`

### `OUTPUT_DIFFERS:15.36` × 22 — first: fz00075 (clause 15.36 FACTORIAL function)
- programs: fz00075, fz00076, fz00077, fz00849, fz00850, fz00851, fz01623, fz01624, fz01625, fz02397, fz02398, fz02399 …
- source: `programs/fz00075.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `FACT(0)=000000000A` | `FACT(0)=+0000000001`
  - line 2: `FACT(1)=000000000A` | `FACT(1)=+0000000001`
  - line 3: `FACT(2)=000000000B` | `FACT(2)=+0000000002`
  - line 4: `FACT(5)=000000012{` | `FACT(5)=+0000000120`
  - line 5: `FACT(10)=000036288{` | `FACT(10)=+0000362880`

### `OUTPUT_DIFFERS:15.47` × 21 — first: fz00032 (clause 15.47 INTEGER-OF-DAY function)
- programs: fz00032, fz00804, fz00805, fz01578, fz01579, fz01580, fz02352, fz02353, fz02354, fz03126, fz03127, fz03128 …
- source: `programs/fz00032.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `JAN 1 2023: 00015413C` | `JAN 1 2023: +000154133`
  - line 2: `DEC 31 2023: 00015449G` | `DEC 31 2023: +000154497`
  - line 3: `DEC 31 2024: 00015486C` | `DEC 31 2024: +000154863`
  - line 4: `JAN 1 1601: 00000000A` | `JAN 1 1601: +000000001`
  - line 5: `DEC 31 9999: 00306767A` | `DEC 31 9999: +003067671`

### `THEY_REJECT:13.18.15` × 21 — first: fz00066 (clause 13.18.15 CONSTANT RECORD clause)
- programs: fz00066, fz00067, fz00068, fz00840, fz00841, fz00842, fz01614, fz01615, fz01616, fz02388, fz02389, fz03162 …
- source: `programs/fz00066.cob`
- GnuCOBOL: `fz00066.cob:5: error: syntax error, unexpected RECORD`

### `OUTPUT_DIFFERS:15.46` × 19 — first: fz00324 (clause 15.46 INTEGER-OF-DATE function)
- programs: fz00324, fz00325, fz01098, fz01099, fz01100, fz01872, fz01873, fz01874, fz02646, fz02647, fz03420, fz03421 …
- source: `programs/fz00324.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `20000101 -> 00014573B` | `20000101 -> +000145732`
  - line 2: `16010101 -> 00000000A` | `16010101 -> +000000001`
  - line 3: `99991231 -> 00306767A` | `99991231 -> +003067671`
  - line 4: `50000101 -> 00124146{` | `50000101 -> +001241460`
  - line 5: `20000229 -> 00014579A` | `20000229 -> +000145791`

### `OUTPUT_DIFFERS:15.67` × 19 — first: fz00045 (clause 15.67 NUMVAL function)
- programs: fz00045, fz00047, fz00046, fz00819, fz00821, fz00820, fz01593, fz01595, fz02368, fz03142, fz03141, fz03916 …
- source: `programs/fz00045.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `NEG: 001234N` | `NEG: -00123.45`
  - line 2: `POS: 007890A` | `POS: +00789.01`
  - line 3: `CR : 001000}` | `CR : -00100.00`
  - line 4: `DB : 002000}` | `DB : -00200.00`
  - line 5: `SPC: 000500{` | `SPC: +00050.00`

### `THEY_REJECT:13.18.19` × 19 — first: fz00465 (clause 13.18.19 DYNAMIC LENGTH clause)
- programs: fz00465, fz00466, fz00467, fz01239, fz01240, fz01241, fz02013, fz02014, fz02015, fz02788, fz02787, fz03561 …
- source: `programs/fz00465.cob`
- GnuCOBOL: `fz00465.cob:5: error: syntax error, unexpected DYNAMIC`

### `WE_REJECT:COBOLNET1517,COBOLNET1740` × 19 — first: fz00183 (clause 15.40 FORMATTED-DATETIME function)
- programs: fz00183, fz00361, fz00389, fz00957, fz01163, fz01327, fz01908, fz02040, fz02506, fz02682, fz02709, fz03588 …
- source: `programs/fz00183.cob`
- WiseOwl: `fz00183.cob(9,1): error COBOLNET1740: data item 'WS-FMT2': the VALUE alphanumeric literal (23 characters) exceeds the item's 20 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `OUTPUT_DIFFERS:15.82` × 18 — first: fz00250 (clause 15.82 SIN function)
- programs: fz00250, fz00251, fz00249, fz01024, fz01023, fz01797, fz01798, fz01799, fz02571, fz02572, fz02573, fz03345 …
- source: `programs/fz00250.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `SIN(0) = 000000000000000{` | `SIN(0) = +00000000.00000000`
  - line 2: `SIN(1) = 000000008414709H` | `SIN(1) = +00000000.84147098`
  - line 3: `SIN(-1) = 000000008414709Q` | `SIN(-1) = -00000000.84147098`
  - line 4: `SIN(00001{) = 000000000998334A` | `SIN(+0000.10) = +00000000.09983341`
  - line 5: `SIN(00002{) = 000000001986693C` | `SIN(+0000.20) = +00000000.19866933`

### `THEY_REJECT:15.26` × 18 — first: fz00766 (clause 15.26 DISPLAY-OF function)
- programs: fz00766, fz00765, fz00767, fz01539, fz01541, fz01540, fz02313, fz02314, fz02315, fz03088, fz03087, fz03089 …
- source: `programs/fz00766.cob`
- GnuCOBOL: `fz00766.cob:12: error: syntax error, unexpected FUNCTION DISPLAY-OF`

### `THEY_REJECT:15.66` × 18 — first: fz00168 (clause 15.66 NATIONAL-OF function)
- programs: fz00168, fz00169, fz00170, fz00942, fz00944, fz00943, fz01718, fz02490, fz02491, fz02492, fz03265, fz03266 …
- source: `programs/fz00168.cob`
- GnuCOBOL: `fz00168.cob:11: error: syntax error, unexpected FUNCTION NATIONAL-OF`

### `OUTPUT_DIFFERS:14.9.12` × 17 — first: fz00339 (clause 14.9.12 DIVIDE statement)
- programs: fz00339, fz00341, fz01114, fz01113, fz01889, fz01887, fz02663, fz02662, fz03435, fz03436, fz03437, fz04209 …
- source: `programs/fz00339.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `FORMAT1: Q 00000000{` | `FORMAT1: Q -0081004.45`
  - line 2: `FORMAT2: Q 00000000{ R 00000000{` | `FORMAT2: Q +       .00 R -0081004.45`
  - line 3: `FORMAT3: Q 00000050{ R 00000000{` | `FORMAT3: Q +0000005.00 R +0000000.00`
  - line 6: `ROUNDED Q 00078166G R 00000000B` | `ROUNDED Q +0007816.67 R +0000000.02`

### `OUTPUT_DIFFERS:15.34` × 17 — first: fz00396 (clause 15.34 EXP function)
- programs: fz00396, fz00398, fz00397, fz01171, fz01172, fz01944, fz01945, fz01946, fz02718, fz02719, fz03492, fz03494 …
- source: `programs/fz00396.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `EXP(0)=00001000` | `EXP(0)=00001.000`
  - line 2: `EXP(1)=00002718` | `EXP(1)=00002.718`
  - line 3: `EXP(-1)=00000367` | `EXP(-1)=00000.367`
  - line 4: `EXP(2)=00007389` | `EXP(2)=00007.389`
  - line 5: `EXP(-2)=00000135` | `EXP(-2)=00000.135`

### `OUTPUT_DIFFERS:15.44` × 17 — first: fz00474 (clause 15.44 INTEGER function)
- programs: fz00474, fz00475, fz00476, fz01248, fz01250, fz01249, fz02022, fz02024, fz02796, fz02797, fz03572, fz04346 …
- source: `programs/fz00474.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `INTEGER(1.5)=000000A` | `INTEGER(1.5)=+0000001`
  - line 2: `INTEGER(-1.5)=000000K` | `INTEGER(-1.5)=-0000002`
  - line 3: `INTEGER(0)=000000{` | `INTEGER(0)=+0000000`
  - line 4: `INTEGER(2.99)=000000B` | `INTEGER(2.99)=+0000002`
  - line 5: `INTEGER(-2.99)=000000L` | `INTEGER(-2.99)=-0000003`

### `OUTPUT_DIFFERS:15.5.2` × 17 — first: fz00399 (clause 15.5.2 Integer date form)
- programs: fz00399, fz00400, fz00401, fz01173, fz01174, fz01175, fz01947, fz01948, fz01949, fz02721, fz02723, fz03497 …
- source: `programs/fz00399.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `INT OF DATE: 0154420` | `INT OF DATE: +0154420`
  - line 4: `DAY TO YYYYDDD: 0000000` | `DAY TO YYYYDDD: +3923001`
  - line 6: `MAX INT DATE: 00000000` | `MAX INT DATE: 22067671`
  - line 9: `MAX INT VALUE: 3067671` | `MAX INT VALUE: +3067671`

### `OUTPUT_DIFFERS:15.56` × 17 — first: fz00102 (clause 15.56 LOG10 function)
- programs: fz00102, fz00104, fz00103, fz00876, fz00877, fz00878, fz02425, fz03198, fz03199, fz03973, fz03974, fz04746 …
- source: `programs/fz00102.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `LOG10(1) = 000000{` | `LOG10(1) = +00000.00`
  - line 2: `LOG10(10) = 000010{` | `LOG10(10) = +00001.00`
  - line 3: `LOG10(100) = 000020{` | `LOG10(100) = +00002.00`
  - line 4: `LOG10(0.1) = 000010}` | `LOG10(0.1) = -00001.00`
  - line 5: `LOG10(1000) = 000030{` | `LOG10(1000) = +00003.00`

### `OUTPUT_DIFFERS:15.86` × 17 — first: fz00612 (clause 15.86 STANDARD-DEVIATION function)
- programs: fz00612, fz00614, fz01387, fz01386, fz02161, fz02162, fz02934, fz02935, fz03708, fz03709, fz03710, fz04482 …
- source: `programs/fz00612.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `3 VALUES: 00000008164I` | `3 VALUES: +00000008.1649`
  - line 2: `1 VALUE: 00000000000{` | `1 VALUE: +00000000.0000`
  - line 3: `ZERO VAR: 00000000000{` | `ZERO VAR: +00000000.0000`
  - line 4: `5 LITERALS: 00000001414B` | `5 LITERALS: +00000001.4142`

### `OUTPUT_DIFFERS:15.89` × 17 — first: fz00180 (clause 15.89 TAN function)
- programs: fz00180, fz00182, fz00181, fz00954, fz00955, fz01728, fz01730, fz02503, fz03277, fz03276, fz03278, fz04050 …
- source: `programs/fz00180.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `TAN(0) = 0000000000{` | `TAN(0) = +0000000.0000`
  - line 3: `TAN(1) = 0000001557D` | `TAN(1) = +0000001.5574`
  - line 4: `TAN(-1) = 0000001557M` | `TAN(-1) = -0000001.5574`
  - line 5: `TAN(PI/2) = 0001255765E` | `TAN(PI/2) = +0001255.7655`
  - line 6: `TAN(-PI/2) = 0001255765N` | `TAN(-PI/2) = -0001255.7655`

### `OUTPUT_DIFFERS:15.91` × 17 — first: fz00285 (clause 15.91 TEST-DAY-YYYYDDD function)
- programs: fz00285, fz00286, fz00287, fz01059, fz01061, fz01834, fz01835, fz02607, fz02608, fz02609, fz03382, fz03381 …
- source: `programs/fz00285.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `VALID-1601-001: 000{` | `VALID-1601-001: +0000`
  - line 2: `VALID-9999-365: 000{` | `VALID-9999-365: +0000`
  - line 3: `INVALID-YEAR-LOW: 000A` | `INVALID-YEAR-LOW: +0001`
  - line 4: `INVALID-YEAR-HIGH: 000A` | `INVALID-YEAR-HIGH: +0001`
  - line 5: `INVALID-LEAP-1: 000{` | `INVALID-LEAP-1: +0000`

### `OUTPUT_DIFFERS:15.20` × 16 — first: fz00636 (clause 15.20 COS function)
- programs: fz00636, fz00638, fz01410, fz01412, fz01411, fz02184, fz02186, fz02185, fz02958, fz03732, fz03733, fz03734 …
- source: `programs/fz00636.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `COS(0)=00000010000000{` | `COS(0)=+0000001.00000000`
  - line 2: `COS(PI)=00000010000000}` | `COS(PI)=-0000000.99999999`
  - line 3: `COS(-PI)=00000010000000}` | `COS(-PI)=-0000000.99999999`
  - line 4: `COS(PI/2)=00000000000000{` | `COS(PI/2)=+0000000.00000000`
  - line 5: `COS(-PI/2)=00000000000000{` | `COS(-PI/2)=+0000000.00000000`

### `OUTPUT_DIFFERS:15.42` × 16 — first: fz00040 (clause 15.42 FRACTION-PART function)
- programs: fz00040, fz00041, fz00813, fz00815, fz01587, fz01589, fz02361, fz02362, fz02363, fz03136, fz03137, fz03910 …
- source: `programs/fz00040.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `FRAC 1.5: 000005{` | `FRAC 1.5: +00000.50`
  - line 2: `FRAC -1.5: 000005}` | `FRAC -1.5: -00000.50`
  - line 3: `FRAC 0: 000000{` | `FRAC 0: +00000.00`
  - line 4: `FRAC -0: 000000{` | `FRAC -0: +00000.00`
  - line 5: `FRAC 10.0: 000000{` | `FRAC 10.0: +00000.00`

### `OUTPUT_DIFFERS:15.60` × 16 — first: fz00609 (clause 15.60 MEAN function)
- programs: fz00609, fz00610, fz00611, fz01383, fz01384, fz01385, fz02158, fz02932, fz03705, fz03706, fz04479, fz04480 …
- source: `programs/fz00609.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `MEAN(1): 0000105{` | `MEAN(1): +000010.50`
  - line 2: `MEAN(2): 0000156B` | `MEAN(2): +000015.62`
  - line 3: `MEAN(3): 0000086F` | `MEAN(3): +000008.66`
  - line 4: `MEAN(ZEROS): 0000000{` | `MEAN(ZEROS): +000000.00`
  - line 5: `MEAN(SYM): 0000000{` | `MEAN(SYM): +000000.00`

### `OUTPUT_DIFFERS:15.64` × 16 — first: fz00315 (clause 15.64 MOD function)
- programs: fz00315, fz00316, fz00317, fz01089, fz01090, fz01864, fz01865, fz01863, fz02638, fz02639, fz03413, fz03411 …
- source: `programs/fz00315.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `MOD(11,5)=0000A` | `MOD(11,5)=+00001`
  - line 2: `MOD(-11,5)=0000D` | `MOD(-11,5)=+00004`
  - line 3: `MOD(11,-5)=0000M` | `MOD(11,-5)=-00004`
  - line 4: `MOD(-11,-5)=0000J` | `MOD(-11,-5)=-00001`
  - line 5: `MOD(0,7)=0000{` | `MOD(0,7)=+00000`

### `OUTPUT_DIFFERS:15.74` × 16 — first: fz00667 (clause 15.74 PRESENT-VALUE function)
- programs: fz00667, fz00666, fz00668, fz01442, fz02215, fz02988, fz02989, fz03762, fz03763, fz03764, fz04536, fz04538 …
- source: `programs/fz00667.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `PV1: 000000009523809E` | `PV1: +0000000095.238095`
  - line 2: `PV2: 000000004988662A` | `PV2: +0000000049.886621`
  - line 3: `PV3: 000000022265414A` | `PV3: +0000000222.654141`
  - line 4: `PV0: 000000005000000{` | `PV0: +0000000050.000000`

### `OUTPUT_DIFFERS:15.76` × 16 — first: fz00273 (clause 15.76 RANGE function)
- programs: fz00273, fz00274, fz00275, fz01049, fz01822, fz02595, fz02596, fz02597, fz03369, fz03370, fz03371, fz04143 …
- source: `programs/fz00273.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `RANGE INT: 000005{` | `RANGE INT: +0000050`
  - line 2: `RANGE DEC: 00000510{` | `RANGE DEC: +0000051.00`
  - line 3: `RANGE SINGLE INT: 000000{` | `RANGE SINGLE INT: +0000000`
  - line 4: `RANGE SINGLE DEC: 00000000{` | `RANGE SINGLE DEC: +0000000.00`
  - line 5: `RANGE NEG: 00000050{` | `RANGE NEG: +0000005.00`

### `OUTPUT_DIFFERS:15.8` × 16 — first: fz00090 (clause 15.8 ACOS function)
- programs: fz00090, fz00091, fz00864, fz00865, fz00866, fz01638, fz02412, fz03186, fz03187, fz03188, fz03960, fz03961 …
- source: `programs/fz00090.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ACOS(0)= 00000001570G` | `ACOS(0)= +00000001.5707`
  - line 2: `ACOS(1)= 00000000000{` | `ACOS(1)= +00000000.0000`
  - line 3: `ACOS(-1)= 00000003141E` | `ACOS(-1)= +00000003.1415`
  - line 4: `ACOS(0.5)= 00000001047A` | `ACOS(0.5)= +00000001.0471`
  - line 5: `ACOS(-0.5)= 00000002094C` | `ACOS(-0.5)= +00000002.0943`

### `OUTPUT_DIFFERS:15.98` × 16 — first: fz00269 (clause 15.98 VARIANCE function)
- programs: fz00269, fz01041, fz01042, fz01043, fz01816, fz01817, fz02590, fz02591, fz03363, fz03364, fz03365, fz04137 …
- source: `programs/fz00269.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `VARIANCE(10.00) = 00000000{` | `VARIANCE(10.00) = +0000000.00`
  - line 2: `VARIANCE(10.00, 20.00) = 00000250{` | `VARIANCE(10.00, 20.00) = +0000025.00`
  - line 3: `VARIANCE(10.00, 20.00, 30.00) = 00000666F` | `VARIANCE(10.00, 20.00, 30.00) = +0000066.66`
  - line 4: `VARIANCE(0.00) = 00000000{` | `VARIANCE(0.00) = +0000000.00`
  - line 5: `VARIANCE(0.00, 0.00) = 00000000{` | `VARIANCE(0.00, 0.00) = +0000000.00`

### `OUTPUT_DIFFERS:8.8.1.4.4` × 16 — first: fz00337 (clause 8.8.1.4.4 Arithmetic expressions > Standard-binary arithmetic > Exponentiation in standard-binary arithmetic)
- programs: fz00337, fz00338, fz00336, fz01111, fz01110, fz01112, fz01884, fz02660, fz02659, fz03432, fz03434, fz04206 …
- source: `programs/fz00337.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 4: `RESULT: 00000010{` | `RESULT: +0000001.00`
  - line 6: `RESULT: 00000010{` | `RESULT: +0000001.00`
  - line 8: `RESULT: 00000040{` | `RESULT: +0000004.00`
  - line 10: `RESULT: 00000270{` | `RESULT: +0000027.00`
  - line 12: `RESULT: 00000160{` | `RESULT: +0000016.00`

### `OUTPUT_DIFFERS:8.8.1.5.4` × 16 — first: fz00916 (clause 8.8.1.5.4 Arithmetic expressions > Standard-decimal arithmetic > Exponentiation in standard-decimal arithmetic)
- programs: fz00916, fz00915, fz00917, fz01689, fz01691, fz02465, fz02463, fz03237, fz03239, fz03238, fz04011, fz04013 …
- source: `programs/fz00916.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `BASE ** 0 = 000000010{` | `BASE ** 0 = +00000001.00`
  - line 3: `BASE ** 1 = 000000020{` | `BASE ** 1 = +00000002.00`
  - line 4: `BASE ** 2 = 000000040{` | `BASE ** 2 = +00000004.00`
  - line 5: `BASE ** 3 = 000000080{` | `BASE ** 3 = +00000008.00`
  - line 6: `BASE ** 4 = 000000160{` | `BASE ** 4 = +00000016.00`

### `WE_REJECT:COBOL0001` × 16 — first: fz00441 (clause 8.8.4.12 Conditional expressions > Abbreviated combined relation conditions)
- programs: fz00441, fz00461, fz01233, fz01401, fz01788, fz02242, fz02623, fz02782, fz02876, fz03556, fz03613, fz04279 …
- source: `programs/fz00441.cob`
- WiseOwl: `fz00441.cob(83,42): error COBOL0001: cannot parse construct near 'OR'`

### `WE_REJECT:COBOLNET2796` × 16 — first: fz00310 (clause 15.88 SUM function)
- programs: fz00310, fz00483, fz00806, fz01016, fz01073, fz01419, fz01847, fz02039, fz02278, fz02539, fz02758, fz03156 …
- source: `programs/fz00310.cob`
- WiseOwl: `fz00310.cob(38,1): error COBOLNET2796: PERFORM … PERFORM: this IF statement is written without its explicit scope terminator END-IF, so it is a conditional statement, and the operand of this phrase is an imperative-statement — ISO §14.5.1: "Any statement with a conditional phrase that is not termina`

### `OUTPUT_DIFFERS:15.27` × 15 — first: fz00462 (clause 15.27 E function)
- programs: fz00462, fz00464, fz01236, fz01238, fz01237, fz02011, fz02012, fz02784, fz02785, fz02786, fz03558, fz03560 …
- source: `programs/fz00462.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `E VALUE: 0000271828182845904509079` | `E VALUE: 00002.71828182845904523536`
  - line 2: `E * 1E20: 0000000000000000000000000` | `E * 1E20: 23536.02874713526624977572`
  - line 4: `E MINUS CONSTANT: 0000000000000000000023536` | `E MINUS CONSTANT: 00000.00000000000000000000`

### `OUTPUT_DIFFERS:15.35` × 15 — first: fz00678 (clause 15.35 EXP10 function)
- programs: fz00678, fz00680, fz01452, fz01454, fz02227, fz03000, fz03001, fz03002, fz03774, fz03775, fz03776, fz04549 …
- source: `programs/fz00678.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `EXP10(0)=000000010{` | `EXP10(0)=+00000001.00`
  - line 2: `EXP10(1)=000000100{` | `EXP10(1)=+00000010.00`
  - line 3: `EXP10(2)=000001000{` | `EXP10(2)=+00000100.00`
  - line 4: `EXP10(-1)=000000001{` | `EXP10(-1)=+00000000.10`
  - line 5: `EXP10(3)=000010000{` | `EXP10(3)=+00001000.00`

### `OUTPUT_DIFFERS:15.5.5` × 15 — first: fz00247 (clause 15.5.5 Standard numeric time form)
- programs: fz00247, fz00246, fz01020, fz01794, fz01795, fz01796, fz02570, fz03342, fz03344, fz04116, fz04117, fz04118 …
- source: `programs/fz00247.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ZERO PLUS ONE: 000000A` | `ZERO PLUS ONE: +0000001`
  - line 2: `MAX NORMAL PLUS ONE: 008640{` | `MAX NORMAL PLUS ONE: +0086400`
  - line 3: `MAX LEAP MINUS ONE: 008639I` | `MAX LEAP MINUS ONE: +0086399`
  - line 4: `LEAP BOUNDARY AFTER SUB: 8639I` | `LEAP BOUNDARY AFTER SUB: +86399`
  - line 5: `86400 MINUS ONE: 8639I` | `86400 MINUS ONE: +86399`

### `OUTPUT_DIFFERS:15.9` × 15 — first: fz00498 (clause 15.9 ANNUITY function)
- programs: fz00498, fz01273, fz01274, fz02048, fz02820, fz02821, fz02822, fz03594, fz03595, fz03596, fz04368, fz04369 …
- source: `programs/fz00498.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ZERO RATE ONE PERIOD: 000000100000{` | `ZERO RATE ONE PERIOD: +0000001.000000`
  - line 2: `ZERO RATE TWO PERIODS: 000000050000{` | `ZERO RATE TWO PERIODS: +0000000.500000`
  - line 3: `FIVE PERCENT ONE PERIOD: 000000105000{` | `FIVE PERCENT ONE PERIOD: +0000001.049999`
  - line 4: `FIVE PERCENT TWO PERIODS: 000000053780D` | `FIVE PERCENT TWO PERIODS: +0000000.537804`
  - line 5: `FIVE PERCENT THREE PERIODS: 000000036720H` | `FIVE PERCENT THREE PERIODS: +0000000.367208`

### `OUTPUT_DIFFERS:15.17` × 14 — first: fz00472 (clause 15.17 COMBINED-DATETIME function)
- programs: fz00472, fz00473, fz01245, fz01246, fz02020, fz02019, fz02794, fz02795, fz03568, fz03567, fz04341, fz04343 …
- source: `programs/fz00472.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `COMBINED NORMAL: 00000000000000000{` | `COMBINED NORMAL: +0000002451545.86400`
  - line 2: `DERIVED DATE: 000000{` | `DERIVED DATE: +2451545`
  - line 3: `DERIVED TIME: 00000{` | `DERIVED TIME: +86400.0`
  - line 4: `COMBINED NEG DATE ZERO TIME: 00000000000000000{` | `COMBINED NEG DATE ZERO TIME: +0000000000000.00000`
  - line 5: `COMBINED MAX TIME: 00000024515458639I` | `COMBINED MAX TIME: +0000002451545.86399`

### `OUTPUT_DIFFERS:15.30` × 14 — first: fz00491 (clause 15.30 EXCEPTION-LOCATION function)
- programs: fz00491, fz01264, fz01263, fz01265, fz02038, fz02811, fz02813, fz03585, fz03587, fz04360, fz04361, fz05134 …
- source: `programs/fz00491.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `IN EXCEPTION: [ ]` | `IN EXCEPTION: [FUZZ; ; 0]`

### `OUTPUT_DIFFERS:15.33` × 14 — first: fz01050 (clause 15.33 EXCEPTION-STATUS function)
- programs: fz01050, fz01051, fz01052, fz01824, fz01825, fz01826, fz02598, fz02600, fz03372, fz03373, fz03374, fz04920 …
- source: `programs/fz01050.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `SIZE EXC: [                               ]` | `SIZE EXC: [EC-SIZE-ZERO-DIVIDE            ]`

### `OUTPUT_DIFFERS:15.4` × 14 — first: fz00042 (clause 15.4 Returned values)
- programs: fz00042, fz00044, fz00817, fz00818, fz01591, fz01592, fz02365, fz02364, fz03138, fz03140, fz03913, fz03914 …
- source: `programs/fz00042.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ABS: 00001234E` | `ABS: +0000123.45`
  - line 2: `SIGN POS: 010{` | `SIGN POS: +01.00`
  - line 3: `SIGN NEG: 010}` | `SIGN NEG: -01.00`
  - line 4: `SIGN ZERO: 000{` | `SIGN ZERO: +00.00`
  - line 6: `ORD A: 006F` | `ORD A: +0066`

### `OUTPUT_DIFFERS:15.68` × 14 — first: fz00195 (clause 15.68 NUMVAL-C function)
- programs: fz00195, fz00196, fz00197, fz00969, fz00971, fz01745, fz02518, fz03292, fz03293, fz04066, fz04067, fz04839 …
- source: `programs/fz00195.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `1: 00000012345F` | `1: +0000001234.56`
  - line 2: `2: 00000000999R` | `2: -0000000099.99`
  - line 3: `3: 00000000000{` | `3: +0000000000.00`
  - line 4: `4: 00000000000{` | `4: +0000000000.00`
  - line 5: `5: 00000000000{` | `5: +0000000000.00`

### `OUTPUT_DIFFERS:15.88` × 14 — first: fz00309 (clause 15.88 SUM function)
- programs: fz00309, fz00311, fz01083, fz01084, fz01085, fz01857, fz01858, fz02632, fz03407, fz03406, fz04180, fz04953 …
- source: `programs/fz00309.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `SUM ONE INT: 0010{` | `SUM ONE INT: +00100`
  - line 2: `SUM TWO INT: 0030{` | `SUM TWO INT: +00300`
  - line 3: `SUM THREE INT: 0060{` | `SUM THREE INT: +00600`
  - line 4: `SUM ONE NUM: 000507N` | `SUM ONE NUM: -00050.75`
  - line 5: `SUM TWO NUM: 001015}` | `SUM TWO NUM: -00101.50`

### `OUTPUT_DIFFERS:15.95` × 14 — first: fz00507 (clause 15.95 TEST-NUMVAL-F function)
- programs: fz00507, fz00508, fz01283, fz02055, fz02056, fz02057, fz02831, fz02830, fz03604, fz03605, fz04378, fz04379 …
- source: `programs/fz00507.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `BAD SIGN: ERR 000B` | `BAD SIGN: ERR +0002`
  - line 4: `BAD DIGIT: ERR 000C` | `BAD DIGIT: ERR +0003`
  - line 5: `SPACE IN: ERR 000C` | `SPACE IN: ERR +0003`
  - line 6: `ZERO LEN: ERR 000B` | `ZERO LEN: ERR +0002`
  - line 7: `ONLY SP: ERR 000F` | `ONLY SP: ERR +0006`

### `OUTPUT_DIFFERS:8.8.1.3` × 14 — first: fz00055 (clause 8.8.1.3 Arithmetic expressions > Native arithmetic)
- programs: fz00055, fz00054, fz00828, fz00829, fz00830, fz01602, fz02378, fz03151, fz03152, fz03150, fz03924, fz04699 …
- source: `programs/fz00055.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ADD: 00444M` | `ADD: -004444`
  - line 2: `MUL: 98000J` | `MUL: -980001`
  - line 3: `DIV: 00000002J` | `DIV: -0000000.21`
  - line 4: `POW: 0000A` | `POW: +00001`
  - line 5: `POW2:000A` | `POW2:+0001`

### `OUTPUT_DIFFERS:13.18.22` × 13 — first: fz00520 (clause 13.18.22 EXTERNAL clause)
- programs: fz00520, fz00521, fz00519, fz01295, fz02069, fz02842, fz02843, fz03616, fz03617, fz04389, fz04391, fz05165 …
- source: `programs/fz00520.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `MOVED:     00` | `MOVED:       `
  - line 4: `FIELD B AFTER ADD: 01` | `FIELD B AFTER ADD: 99`

### `OUTPUT_DIFFERS:15.10` × 13 — first: fz00354 (clause 15.10 ASIN function)
- programs: fz00354, fz00356, fz01128, fz01130, fz01129, fz01902, fz01903, fz01904, fz03450, fz03451, fz04224, fz04225 …
- source: `programs/fz00354.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ASIN(0)=00000000{` | `ASIN(0)=+00000.0000`
  - line 2: `ASIN(1)=00001570G` | `ASIN(1)=+00001.5707`
  - line 3: `ASIN(-1)=00001570P` | `ASIN(-1)=-00001.5707`
  - line 4: `ASIN(0.5)=00000523E` | `ASIN(0.5)=+00000.5235`
  - line 5: `ASIN(-0.5)=00000523N` | `ASIN(-0.5)=-00000.5235`

### `OUTPUT_DIFFERS:15.55` × 13 — first: fz00033 (clause 15.55 LOG function)
- programs: fz00033, fz00035, fz00807, fz00808, fz01581, fz02355, fz02357, fz03129, fz03130, fz03131, fz03905, fz04677 …
- source: `programs/fz00033.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `LOG(1.00) = 000000{` | `LOG(1.00) = +0000000`
  - line 2: `LOG(2.718282) = 000000A` | `LOG(2.718282) = +0000001`
  - line 3: `LOG(100.00) = 000000D` | `LOG(100.00) = +0000004`

### `OUTPUT_DIFFERS:15.61` × 13 — first: fz00191 (clause 15.61 MEDIAN function)
- programs: fz00191, fz00190, fz01737, fz01739, fz01738, fz02513, fz02512, fz03285, fz03287, fz04059, fz04060, fz04834 …
- source: `programs/fz00191.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `MEDIAN ODD INT: 0050{` | `MEDIAN ODD INT: +005.00`
  - line 2: `MEDIAN EVEN INT: 0050{` | `MEDIAN EVEN INT: +005.00`
  - line 3: `MEDIAN NEG ODD: 0040}` | `MEDIAN NEG ODD: -004.00`
  - line 4: `MEDIAN NEG EVEN: 0040}` | `MEDIAN NEG EVEN: -004.00`
  - line 5: `MEDIAN SAME ODD: 0050{` | `MEDIAN SAME ODD: +005.00`

### `OUTPUT_DIFFERS:15.69` × 13 — first: fz00073 (clause 15.69 NUMVAL-F function)
- programs: fz00073, fz00074, fz00847, fz01621, fz01622, fz02395, fz03169, fz03170, fz03942, fz03943, fz04717, fz04716 …
- source: `programs/fz00073.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `NUMVAL-F BASIC: 00123456{` | `NUMVAL-F BASIC: +00123.4560`
  - line 2: `NUMVAL-F SPACES: 00789010{` | `NUMVAL-F SPACES: +00789.0100`
  - line 3: `NUMVAL-F EXP POS: 12300000}` | `NUMVAL-F EXP POS: -12300.0000`
  - line 4: `NUMVAL-F EXP NEG: 00000056G` | `NUMVAL-F EXP NEG: +00000.0567`
  - line 5: `NUMVAL-F INT SPACES: 00100000{` | `NUMVAL-F INT SPACES: +00100.0000`

### `THEY_REJECT:13.18.29` × 13 — first: fz00544 (clause 13.18.29 GROUP-USAGE clause)
- programs: fz00544, fz00545, fz01317, fz01318, fz01319, fz02091, fz02866, fz02867, fz03639, fz03640, fz05188, fz05189 …
- source: `programs/fz00544.cob`
- GnuCOBOL: `fz00544.cob:5: error: 'GROUP-USAGE' is a reserved word, but isn't supported`

### `WE_REJECT:COBOLNET0819,COBOLNET0881` × 13 — first: fz00648 (clause 15.16 CHAR-NATIONAL function)
- programs: fz00648, fz00649, fz01422, fz01423, fz01424, fz02196, fz02197, fz02971, fz03418, fz03744, fz03745, fz03746 …
- source: `programs/fz00648.cob`
- WiseOwl: `fz00648.cob(6,1): error COBOLNET0881: data item 'WS-RESULT': USAGE NATIONAL may not be specified with an alphabetic or alphanumeric PICTURE (X(1)) — it admits boolean, national, national-edited, numeric, and numeric-edited pictures only (ISO §13.18.60.3 SR12; §13.18.40.3 SR30)`

### `OUTPUT_DIFFERS:13.18.44` × 12 — first: fz01101 (clause 13.18.44 REDEFINES clause)
- programs: fz01101, fz01102, fz01103, fz01875, fz02650, fz02651, fz03425, fz03424, fz04198, fz04199, fz04973, fz04971
- source: `programs/fz01101.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 4: `NEGATIVE HIGH=00 LOW=0J` | `NEGATIVE HIGH=00 LOW=0q`

### `OUTPUT_DIFFERS:14.9.9` × 12 — first: fz00048 (clause 14.9.9 CONTINUE statement)
- programs: fz00048, fz00049, fz00822, fz00823, fz01596, fz01597, fz01598, fz03144, fz03146, fz03918, fz03919, fz04693
- source: `programs/fz00048.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `STEP 001 RESULT 000105{` | `STEP 001 RESULT +00010.50`
  - line 2: `STEP 002 RESULT 000210{` | `STEP 002 RESULT +00021.00`
  - line 3: `STEP 003 RESULT 000315{` | `STEP 003 RESULT +00031.50`

### `OUTPUT_DIFFERS:15.15` × 12 — first: fz00121 (clause 15.15 CHAR function)
- programs: fz00121, fz00895, fz01670, fz02442, fz02444, fz03216, fz03991, fz04764, fz04765, fz05539, fz05538, fz05540
- source: `programs/fz00121.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 5: `CHAR(256)=ÿ` | `CHAR(256)=�`

### `OUTPUT_DIFFERS:15.43` × 12 — first: fz00561 (clause 15.43 HIGHEST-ALGEBRAIC function)
- programs: fz00561, fz00562, fz01335, fz02110, fz02109, fz02883, fz02885, fz03657, fz03658, fz04431, fz04432, fz05207
- source: `programs/fz00561.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `HIGHEST(S99): 00000000990{` | `HIGHEST(S99): +0000000099.00`
  - line 2: `HIGHEST(INT8): 000000009I` | `HIGHEST(INT8): +0000000099`
  - line 3: `HIGHEST(INT16): 000000999I` | `HIGHEST(INT16): +0000009999`
  - line 4: `HIGHEST(INT32): 099999999I` | `HIGHEST(INT32): +0999999999`
  - line 5: `HIGHEST(9V99): 00000000099I` | `HIGHEST(9V99): +0000000009.99`

### `OUTPUT_DIFFERS:15.5` × 12 — first: fz00159 (clause 15.5 Date and time conversion functions)
- programs: fz00159, fz00160, fz00934, fz01709, fz02481, fz02482, fz02483, fz03255, fz03256, fz04803, fz05578, fz05577
- source: `programs/fz00159.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `INT-OF-DATE 2000 -> YYYYMMDD: 0` | `INT-OF-DATE 2000 -> YYYYMMDD: 02000000`

### `OUTPUT_DIFFERS:15.58` × 12 — first: fz00000 (clause 15.58 LOWEST-ALGEBRAIC function)
- programs: fz00000, fz00775, fz01548, fz01550, fz02322, fz02323, fz03098, fz03096, fz03870, fz04645, fz05418, fz05420
- source: `programs/fz00000.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `LOWEST S9: R` | `LOWEST S9: -9`
  - line 2: `LOWEST S99: 9R` | `LOWEST S99: -99`
  - line 3: `LOWEST S9(3): 99R` | `LOWEST S9(3): -999`
  - line 5: `LOWEST UV9: 00` | `LOWEST UV9: 0.0`
  - line 6: `LOWEST UV99: 0000` | `LOWEST UV99: 00.00`

### `OUTPUT_DIFFERS:15.77` × 12 — first: fz00405 (clause 15.77 REM function)
- programs: fz00405, fz01181, fz01954, fz01955, fz02727, fz03501, fz04275, fz04277, fz04276, fz05049, fz05051, fz05050
- source: `programs/fz00405.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `POS/POS: 00000034E` | `POS/POS: +0000003.45`
  - line 3: `NEG/POS: 00000034N` | `NEG/POS: -0000003.45`
  - line 4: `POS/NEG: 00000034E` | `POS/NEG: +0000003.45`
  - line 5: `NEG/NEG: 00000034N` | `NEG/NEG: -0000003.45`
  - line 6: `ZERO/NON-ZERO: 00000000{` | `ZERO/NON-ZERO: +0000000.00`

### `OUTPUT_DIFFERS:15.93` × 12 — first: fz00581 (clause 15.93 TEST-NUMVAL function)
- programs: fz00581, fz00580, fz00579, fz01354, fz02127, fz02128, fz02901, fz02902, fz04449, fz04450, fz04451, fz05224
- source: `programs/fz00581.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ZERO-LENGTH: 41` | `ZERO-LENGTH: 000000041`
  - line 3: `SPACES-ONLY: 41` | `SPACES-ONLY: 000000041`
  - line 4: `VALID-INT: 0` | `VALID-INT: 000000000`
  - line 5: `NEG-INT: 0` | `NEG-INT: 000000000`
  - line 6: `POS-INT: 0` | `POS-INT: 000000000`

### `OUTPUT_DIFFERS:15.94` × 12 — first: fz00348 (clause 15.94 TEST-NUMVAL-C function)
- programs: fz00348, fz00349, fz00350, fz01123, fz01124, fz01898, fz03444, fz03445, fz03446, fz04219, fz04993, fz04994
- source: `programs/fz00348.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `EMPTY LEN: 0000` | `EMPTY LEN: 0001`
  - line 2: `TEST-NUMVAL-C EMPTY:    1` | `TEST-NUMVAL-C EMPTY:    2`

### `OUTPUT_DIFFERS:13.18.55` × 11 — first: fz00297 (clause 13.18.55 SYNCHRONIZED clause)
- programs: fz00297, fz00298, fz01071, fz01072, fz01846, fz02619, fz04168, fz04167, fz04941, fz04943, fz04942
- source: `programs/fz00297.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `<end>` | `LEFT SYNC: L                   `
  - line 2: `<end>` | `RIGHT SYNC: R9                  `
  - line 3: `<end>` | `NORM SYNC: N                   `
  - WiseOwl run exit 1

### `OUTPUT_DIFFERS:14.9.2` × 11 — first: fz00060 (clause 14.9.2 ADD statement)
- programs: fz00060, fz00834, fz00836, fz01608, fz01610, fz02383, fz02384, fz03930, fz03931, fz04704, fz05479
- source: `programs/fz00060.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `FORMAT 1 ACCUM:` | `FORMAT 1 ACCUM:      `
  - line 2: `FORMAT 2 GIVING:` | `FORMAT 2 GIVING:      `
  - line 3: `ZERO RESULT:` | `ZERO RESULT:      `
  - line 4: `CORR RESULT:` | `CORR RESULT:      `

### `OUTPUT_DIFFERS:14.9.8` × 11 — first: fz00217 (clause 14.9.8 COMPUTE statement)
- programs: fz00217, fz00990, fz01764, fz01766, fz02538, fz03313, fz04087, fz04086, fz04860, fz04862, fz04861
- source: `programs/fz00217.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `DIV: 00000033C` | `DIV: +0000003.33`
  - line 2: `DIV-RND: 00000033C` | `DIV-RND: +0000003.33`
  - line 3: `OVERFLOW-SUM: 00000000{` | `OVERFLOW-SUM: +0000000.00`
  - line 4: `OVERFLOW-MUL: 09999900{` | `OVERFLOW-MUL: +0999990.00`
  - line 5: `NEG-EXP: 00000073H` | `NEG-EXP: +0000007.38`

### `OUTPUT_DIFFERS:15.62` × 11 — first: fz00504 (clause 15.62 MIDRANGE function)
- programs: fz00504, fz01278, fz01280, fz02052, fz02053, fz02054, fz02826, fz03601, fz04374, fz05148, fz05150
- source: `programs/fz00504.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ZERO ZERO: 00000000{` | `ZERO ZERO: +0000000.00`
  - line 2: `NEG POS: 00000000{` | `NEG POS: +0000000.00`
  - line 3: `SAME SAME: 00000420{` | `SAME SAME: +0000042.00`
  - line 4: `MIX ZERO POS: 00000247E` | `MIX ZERO POS: +0000024.75`
  - line 5: `NEG SAME: 00000290}` | `NEG SAME: -0000029.00`

### `OUTPUT_DIFFERS:15.81` × 11 — first: fz00547 (clause 15.81 SIGN function)
- programs: fz00547, fz01320, fz01322, fz02094, fz02870, fz02869, fz03643, fz04417, fz04418, fz05190, fz05192
- source: `programs/fz00547.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `POS: 1` | `POS: 000000001`
  - line 2: `NEG: -1` | `NEG: -000000001`
  - line 3: `ZERO: 0` | `ZERO: 000000000`
  - line 4: `ZRO: 0` | `ZRO: 000000000`
  - line 5: `NZR: 0` | `NZR: 000000000`

### `OUTPUT_DIFFERS:8.8.4` × 11 — first: fz00727 (clause 8.8.4 Conditional expressions)
- programs: fz00727, fz00728, fz01500, fz02274, fz02275, fz02276, fz03049, fz04596, fz04597, fz05370, fz05372
- source: `programs/fz00727.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `MAX: 0000A` | `MAX: +00001`
  - line 2: `MIN: 0000{` | `MIN: +00000`
  - line 6: `RESULT: MATCH` | `RESULT: MATCH               `

### `THEY_REJECT:8.8.4.4` × 11 — first: fz00657 (clause 8.8.4.4 Conditional expressions > Simple class condition)
- programs: fz00657, fz00659, fz01432, fz02205, fz02979, fz02980, fz02981, fz03754, fz04527, fz04528, fz05301
- source: `programs/fz00657.cob`
- GnuCOBOL: `fz00657.cob:41: error: syntax error, unexpected BOOLEAN`

### `WE_REJECT:COBOLNET1686` × 11 — first: fz00440 (clause 8.8.4.8 Conditional expressions > Simple omitted argument condition)
- programs: fz00440, fz01213, fz01212, fz01986, fz01988, fz02762, fz02761, fz03534, fz03535, fz03536, fz05082
- source: `programs/fz00440.cob`
- WiseOwl: `fz00440.cob(15,1): error COBOLNET1686: omitted-argument condition 'WS-RESULT IS OMITTED': data-name-1 shall be a formal parameter defined in the source element in which this condition is specified (ISO §8.8.4.8.3 SR1)`

### `OUTPUT_DIFFERS:14.9.11` × 10 — first: fz00179 (clause 14.9.11 DISPLAY statement)
- programs: fz00179, fz00951, fz00952, fz00953, fz01725, fz01727, fz03275, fz04048, fz05596, fz05597
- source: `programs/fz00179.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 4: `J` | `-1`

### `OUTPUT_DIFFERS:14.9.25` × 10 — first: fz00431 (clause 14.9.25 MOVE statement)
- programs: fz00431, fz01203, fz01979, fz01978, fz03526, fz04299, fz04301, fz05073, fz05075, fz05074
- source: `programs/fz00431.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 4: `DEC TO DEC: 12345` | `DEC TO DEC: 123.45`
  - line 5: `DEC-S TO DEC: 12345` | `DEC-S TO DEC: 123.45`
  - line 6: `EDIT TO DEC: 00000` | `EDIT TO DEC: 000.00`
  - line 7: `EDIT-S TO DEC: 00000` | `EDIT-S TO DEC: 000.00`
  - line 10: `GROUP TO GROUP:     000` | `GROUP TO GROUP:        `

### `OUTPUT_DIFFERS:15.84` × 10 — first: fz00208 (clause 15.84 SQRT function)
- programs: fz00208, fz00983, fz01756, fz01757, fz02531, fz02530, fz03303, fz04078, fz04079, fz04853
- source: `programs/fz00208.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `SQRT(0) BINARY: 0000000000000000000{` | `SQRT(0) BINARY: +0000000000.0000000000`
  - line 2: `SQRT(1) DECIMAL: 00100{` | `SQRT(1) DECIMAL: +001.000`
  - line 3: `SQRT(49) BINARY: 0000000007000000000{` | `SQRT(49) BINARY: +0000000007.0000000000`
  - line 4: `SQRT(2) DECIMAL: 00141D` | `SQRT(2) DECIMAL: +001.414`
  - line 5: `SQRT(01) BINARY: 0000000001000000000{` | `SQRT(01) BINARY: +0000000001.0000000000`

### `OUTPUT_DIFFERS:13.18.8` × 9 — first: fz00332 (clause 13.18.8 BLANK WHEN ZERO clause)
- programs: fz00332, fz01105, fz01106, fz03426, fz03427, fz03428, fz04200, fz04974, fz04976
- source: `programs/fz00332.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `ADD ZERO TO ZERO: [000000{]` | `ADD ZERO TO ZERO: [+00000.00]`

### `OUTPUT_DIFFERS:14.9.1` × 9 — first: fz00204 (clause 14.9.1 ACCEPT statement)
- programs: fz00204, fz00205, fz00206, fz01752, fz01754, fz02526, fz02528, fz03301, fz04075
- source: `programs/fz00204.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 4: `TIME HHMMSS: 030431` | `TIME HHMMSS: 030273`

### `OUTPUT_DIFFERS:14.9.44` × 9 — first: fz00162 (clause 14.9.44 SUBTRACT statement)
- programs: fz00162, fz00936, fz00938, fz02484, fz02486, fz03259, fz04032, fz04034, fz04807
- source: `programs/fz00162.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `FORMAT 1 RESULT: 0004{` | `FORMAT 1 RESULT: +00040`
  - line 3: `FORMAT 1 SUM RESULT: 0004{` | `FORMAT 1 SUM RESULT: +00040`
  - line 4: `FORMAT 1 SUM RESULT: 0004{` | `FORMAT 1 SUM RESULT: +00040`
  - line 5: `FORMAT 1 SUM RESULT: 0004{` | `FORMAT 1 SUM RESULT: +00040`
  - line 7: `FORMAT 2 GIVING RESULT: 000003{` | `FORMAT 2 GIVING RESULT: +0000030`

### `OUTPUT_DIFFERS:15.4.1` × 9 — first: fz00292 (clause 15.4.1 Numeric and integer functions)
- programs: fz00292, fz01065, fz01066, fz01841, fz02613, fz03387, fz03389, fz04163, fz04937
- source: `programs/fz00292.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `NUMVAL 1: 00001234E` | `NUMVAL 1: +0000123.45`
  - line 2: `NUMVAL 2: 00000678R` | `NUMVAL 2: -0000067.89`
  - line 3: `NUMVAL-C: 00000000{` | `NUMVAL-C: +0000000.00`
  - line 4: `NUMVAL-F: 00000123D` | `NUMVAL-F: +0000012.34`
  - line 5: `SIGN BIN: 000000J` | `SIGN BIN: -0000001`

### `OUTPUT_DIFFERS:15.49` × 9 — first: fz00366 (clause 15.49 INTEGER-PART function)
- programs: fz00366, fz01140, fz01142, fz02688, fz02689, fz03462, fz03463, fz04238, fz05011
- source: `programs/fz00366.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ZERO: 000000{` | `ZERO: +0000000`
  - line 2: `POS-FRAC: 000000A` | `POS-FRAC: +0000001`
  - line 3: `NEG-FRAC: 000000J` | `NEG-FRAC: -0000001`
  - line 4: `POS-INT: 000010{` | `POS-INT: +0000100`
  - line 5: `NEG-INT: 000010}` | `NEG-INT: -0000100`

### `OUTPUT_DIFFERS:15.7` × 9 — first: fz00654 (clause 15.7 ABS function)
- programs: fz00654, fz00656, fz01428, fz01430, fz02976, fz03750, fz03752, fz04525, fz04526
- source: `programs/fz00654.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ABS INT NEG: 0010{` | `ABS INT NEG: +00100`
  - line 2: `ABS INT ZERO: 0000{` | `ABS INT ZERO: +00000`
  - line 3: `ABS INT MAX: 9999I` | `ABS INT MAX: +99999`
  - line 4: `ABS INT MIN: 9999I` | `ABS INT MIN: +99999`
  - line 5: `ABS DEC NEG: 001234E` | `ABS DEC NEG: +00123.45`

### `OUTPUT_DIFFERS:15.90` × 9 — first: fz00096 (clause 15.90 TEST-DATE-YYYYMMDD function)
- programs: fz00096, fz00872, fz00871, fz01646, fz02419, fz03193, fz04740, fz05514, fz05516
- source: `programs/fz00096.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `MIN DATE VALID: {` | `MIN DATE VALID: +0`
  - line 2: `MAX DATE VALID: {` | `MAX DATE VALID: +0`
  - line 3: `BELOW MIN VALID: A` | `BELOW MIN VALID: +1`
  - line 4: `ABOVE MAX VALID: A` | `ABOVE MAX VALID: +1`
  - line 5: `FEB 30 LEAP VALID: C` | `FEB 30 LEAP VALID: +3`

### `THEY_FAIL_RUN:8.8.4.2.10` × 9 — first: fz00597 (clause 8.8.4.2.10 Conditional expressions > Simple relation conditions > Standard comparison)
- programs: fz00597, fz00599, fz01372, fz01373, fz02146, fz02147, fz02920, fz04467, fz05241
- source: `programs/fz00597.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 5: `STD COMP: G` | `<end>`
  - GnuCOBOL run exit 1: ``

### `WE_REJECT:COBOLNET0801` × 9 — first: fz00602 (clause 15.73 PI function)
- programs: fz00602, fz00981, fz00982, fz01810, fz02149, fz02585, fz03305, fz04077, fz05491
- source: `programs/fz00602.cob`
- WiseOwl: `fz00602.cob(5,1): error COBOLNET0801: data item 'WS-PI' (PICTURE 9(10)V9(25)) has 35 digit positions; ISO/IEC 1989 limits fixed-point items and literals to 31 digits (ISO §8.3.3.3.2)`

### `WE_REJECT:COBOLNET0827` × 9 — first: fz00452 (clause 14.9.14 EXIT statement)
- programs: fz00452, fz01225, fz01226, fz01999, fz02000, fz02774, fz04322, fz05095, fz05096
- source: `programs/fz00452.cob`
- WiseOwl: `fz00452.cob(32,1): error COBOLNET0827: EXIT SECTION may be specified only in a section (ISO §14.9.14.3 SR9)`

### `WE_REJECT:COBOLNET1637` × 9 — first: fz00996 (clause 13.18.64 VARYING clause)
- programs: fz00996, fz01350, fz01352, fz01772, fz02544, fz03320, fz04093, fz04867, fz04868
- source: `programs/fz00996.cob`
- WiseOwl: `fz00996.cob(14,7): error COBOLNET1637: the index-name 'WS-IDX' is not an arithmetic operand (ISO §8.8.1.1 names no index-names; §13.18.38.3 r7 admits an index-name only in a subscript, PERFORM/SEARCH VARYING, SET, or as an operand OF a relation condition, never inside an arithmetic expression, §8.8.`

### `OUTPUT_DIFFERS:13.18.53` × 8 — first: fz00345 (clause 13.18.53 SOURCE clause)
- programs: fz00345, fz00346, fz00347, fz01121, fz01893, fz03441, fz04989, fz04990
- source: `programs/fz00345.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 4: `WS-LARGE:           9999999` | `WS-LARGE:           99999.99`

### `OUTPUT_DIFFERS:13.18.54` × 8 — first: fz00485 (clause 13.18.54 SUM clause)
- programs: fz00485, fz01257, fz02033, fz02806, fz04353, fz05127, fz05128, fz05129
- source: `programs/fz00485.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `SUM OF X AND Y IS 0000002L` | `SUM OF X AND Y IS -0000002.3`
  - line 2: `SUM OF X AND Y IS 0000000G` | `SUM OF X AND Y IS +0000000.7`
  - line 3: `SUM OF X AND Y IS 0000003G` | `SUM OF X AND Y IS +0000003.7`

### `OUTPUT_DIFFERS:14.9.26` × 8 — first: fz00437 (clause 14.9.26 MULTIPLY statement)
- programs: fz00437, fz01209, fz01983, fz02757, fz03531, fz03533, fz03532, fz05080
- source: `programs/fz00437.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `BY FORMAT: RESULT=000000{ ERR=N` | `BY FORMAT: RESULT=+00000.00 ERR=N`
  - line 2: `GIVING LITERAL: RESULT=00009000}` | `GIVING LITERAL: RESULT=-0000900.00`
  - line 3: `AFTER MOVE AND COMPUTE: 00000000{` | `AFTER MOVE AND COMPUTE: +0000000.00`

### `OUTPUT_DIFFERS:15.1` × 8 — first: fz00539 (clause 15.1 General)
- programs: fz00539, fz01311, fz02086, fz02087, fz02859, fz03634, fz04407, fz05183
- source: `programs/fz00539.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ABS POS: 00001234E` | `ABS POS: +0000123.45`
  - line 2: `ABS NEG: 00001234E` | `ABS NEG: +0000123.45`
  - line 3: `ABS ZERO: 00000000{` | `ABS ZERO: +0000000.00`
  - line 5: `FRAC VAL: 45` | `FRAC VAL: .45`
  - line 6: `SIGN POS: A` | `SIGN POS: +1`

### `OUTPUT_DIFFERS:8.8.1` × 8 — first: fz00739 (clause 8.8.1 Arithmetic expressions)
- programs: fz00739, fz01514, fz01512, fz02287, fz03834, fz03836, fz04610, fz05384
- source: `programs/fz00739.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `LINEAR: 000000024{` | `LINEAR: +0000000240`
  - line 2: `POWER: 000001000{` | `POWER: +0000010000`
  - line 3: `ABS: 000000002{` | `ABS: +0000000020`
  - line 4: `SIGN: 000000000J` | `SIGN: -0000000001`
  - line 5: `DIV: 000000003C` | `DIV: +0000000033`

### `OUTPUT_DIFFERS:8.8.1.4.2` × 8 — first: fz00730 (clause 8.8.1.4.2 Arithmetic expressions > Standard-binary arithmetic > Standard-binary intermediate data item)
- programs: fz00730, fz01505, fz02279, fz03053, fz03825, fz03827, fz04600, fz05373
- source: `programs/fz00730.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `LARGE: 0000000000000{` | `LARGE: +        .000000`
  - line 4: `SMALL: 0000000000000{` | `SMALL: +00000000.000000`
  - line 6: `DIFF: 0000000000000{` | `DIFF: +00000000.000000`
  - line 7: `THIRD: 0000000033333C` | `THIRD: +00000000.333333`
  - line 8: `BACK: 0000000099999I` | `BACK: +00000000.999999`

### `OUTPUT_DIFFERS:8.8.1.4.3` × 8 — first: fz00770 (clause 8.8.1.4.3 Arithmetic expressions > Standard-binary arithmetic > Basic arithmetic operations in standard-binary arithmetic)
- programs: fz00770, fz01542, fz02318, fz03090, fz03091, fz03864, fz04640, fz05412
- source: `programs/fz00770.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ADD 1234 + -5678 = 0000444M` | `ADD 1234 + -5678 = -00004444`
  - line 2: `ADD 9999 + -9999 = 0000000{` | `ADD 9999 + -9999 = +00000000`
  - line 3: `ADD 0 + 0 = 0000000{` | `ADD 0 + 0 = +00000000`
  - line 4: `SUB 1234 - -5678 = 0000691B` | `SUB 1234 - -5678 = +00006912`
  - line 5: `SUB 9999 - -9999 = 0001999H` | `SUB 9999 - -9999 = +00019998`

### `THEY_FAIL_RUN:15.85` × 8 — first: fz01551 (clause 15.85 STANDARD-COMPARE function)
- programs: fz01551, fz01553, fz02326, fz03100, fz03874, fz03875, fz05421, fz05422
- source: `programs/fz01551.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `EQ: =` | `<end>`
  - line 2: `LT: <` | `<end>`
  - line 3: `GT: >` | `<end>`
  - line 4: `CASE1: <` | `<end>`
  - line 5: `CASE2: >` | `<end>`
  - GnuCOBOL run exit 1: ``

### `OUTPUT_DIFFERS:14.9.28` × 7 — first: fz00252 (clause 14.9.28 PERFORM statement)
- programs: fz00252, fz00253, fz01026, fz02576, fz03348, fz03349, fz04898
- source: `programs/fz00252.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `TIMES LOOP: 000E` | `TIMES LOOP: +0005`
  - line 2: `VARYING LOOP: 000F` | `VARYING LOOP: +0006`
  - line 3: `TEST BEFORE LOOP: 000F` | `TEST BEFORE LOOP: +0006`
  - line 4: `TEST AFTER LOOP: 001{` | `TEST AFTER LOOP: +0010`
  - line 5: `VARYING WITH IF: 000D` | `VARYING WITH IF: +0004`

### `OUTPUT_DIFFERS:15.97` × 7 — first: fz00301 (clause 15.97 UPPER-CASE function)
- programs: fz00301, fz00302, fz01074, fz01848, fz03397, fz04171, fz04946
- source: `programs/fz00301.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `NATIONAL UPPER: ABCDE` | `NATIONAL UPPER:  A B C D E`

### `THEY_REJECT:13.18.8` × 7 — first: fz00330 (clause 13.18.8 BLANK WHEN ZERO clause)
- programs: fz00330, fz01878, fz01879, fz02652, fz02654, fz04202, fz04975
- source: `programs/fz00330.cob`
- GnuCOBOL: `fz00330.cob:20: error: MOVE of figurative constant SPACE to numeric item used`

### `WE_REJECT:COBOLNET0809,COBOLNET0898,COBOLNET1694,COBOLNET2168` × 7 — first: fz01883 (clause 15.63 MIN function)
- programs: fz01883, fz02582, fz03430, fz04129, fz04204, fz04904, fz04978
- source: `programs/fz01883.cob`
- WiseOwl: `fz01883.cob(13,1): error COBOLNET2168: data item 'WS-INX-1': the VALUE clause shall not be specified with USAGE INDEX (a data item of class index — ISO §13.16.3 SR10)`

### `WE_REJECT:COBOLNET2326` × 7 — first: fz00001 (clause 15.58 LOWEST-ALGEBRAIC function)
- programs: fz00001, fz00563, fz01436, fz02111, fz02208, fz02983, fz04377
- source: `programs/fz00001.cob`
- WiseOwl: `fz00001.cob(62,1): error COBOLNET2326: SET 'WS-S9' TO 'FUNCTION LOWEST-ALGEBRAIC(WS-S9)' violates ISO §14.9.39.3 SR4: "If identifier-1 references a numeric data item, index-name-2 shall be specified"`

### `OUTPUT_DIFFERS:13.18.52` × 6 — first: fz00623 (clause 13.18.52 SIGN clause)
- programs: fz00623, fz01395, fz01396, fz02169, fz02943, fz04491
- source: `programs/fz00623.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 5: `LNON NEG: [20}]` | `LNON NEG: [-200]`
  - line 6: `LNON POS: [30{]` | `LNON POS: [+300]`
  - line 7: `TNON NEG: [40}]` | `TNON NEG: [-400]`
  - line 8: `TNON POS: [50{]` | `TNON POS: [+500]`
  - line 9: `GROUP ELEM ADD: [{0099]` | `GROUP ELEM ADD: [-99901]`

### `OUTPUT_DIFFERS:15.57` × 6 — first: fz01234 (clause 15.57 LOWER-CASE function)
- programs: fz01234, fz01235, fz02007, fz04329, fz05103, fz05104
- source: `programs/fz01234.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `NATL: xyzwv` | `NATL:  x y z w v`

### `OUTPUT_DIFFERS:15.72` × 6 — first: fz00308 (clause 15.72 ORD-MIN function)
- programs: fz00308, fz01082, fz01854, fz04176, fz04178, fz04952
- source: `programs/fz00308.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ORD-MIN(AAA,BBB,CCC)=000A` | `ORD-MIN(AAA,BBB,CCC)=+0001`
  - line 2: `ORD-MIN(CCC,BBB,AAA)=000C` | `ORD-MIN(CCC,BBB,AAA)=+0003`
  - line 3: `ORD-MIN(BBB,AAA,CCC)=000B` | `ORD-MIN(BBB,AAA,CCC)=+0002`
  - line 4: `ORD-MIN(1,2,3)=000A` | `ORD-MIN(1,2,3)=+0001`
  - line 5: `ORD-MIN(3,2,1)=000C` | `ORD-MIN(3,2,1)=+0003`

### `OUTPUT_DIFFERS:15.73` × 6 — first: fz00600 (clause 15.73 PI function)
- programs: fz00600, fz02922, fz02923, fz03697, fz05244, fz05246
- source: `programs/fz00600.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `PI: 0000314159265358979311599` | `PI: 00003.14159265358979323846`
  - line 2: `2*PI: 0000628318530717958600000` | `2*PI: 00006.28318530717958647692`
  - line 3: `SIN(PI): 000000000000000000001224G` | `SIN(PI): +00000.00000000000000000000`
  - line 4: `COS(PI): 000010000000000000000000}` | `COS(PI): -00000.99999999999999999999`
  - line 5: `CIRCUMFERENCE: 000062831853071795860000{` | `CIRCUMFERENCE: +00006.28318530717958647692`

### `WE_REJECT:COBOLNET0809,COBOLNET0898,COBOLNET1694,COBOLNET1740,COBOLNET2168` × 6 — first: fz00260 (clause 15.59 MAX function)
- programs: fz00260, fz02655, fz03429, fz04130, fz04977, fz04979
- source: `programs/fz00260.cob`
- WiseOwl: `fz00260.cob(9,1): error COBOLNET1740: data item 'WS-ALPHA-2': the VALUE alphanumeric literal (6 characters) exceeds the item's 5 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET0900,COBOLNET2455` × 6 — first: fz01178 (clause 14.9.10 DELETE statement)
- programs: fz01178, fz01950, fz02712, fz02910, fz05558, fz05591
- source: `programs/fz01178.cob`
- WiseOwl: `fz01178.cob(8,16): error COBOLNET2455: file 'INPUT-FILE': FILE STATUS 'WS-IN-STATUS' is a category-numeric item, not of the category alphanumeric; data-name-1 shall reference a two-character data item of the category alphanumeric, defined in the working-storage, local-storage, or linkage section (IS`

### `WE_REJECT:COBOLNET0900,COBOLNET2605` × 6 — first: fz00094 (clause 13.18.10 BLOCK CONTAINS clause)
- programs: fz00094, fz00557, fz01642, fz02417, fz03191, fz03963
- source: `programs/fz00094.cob`
- WiseOwl: `fz00094.cob(12,15): error COBOLNET2605: file 'INPUT-FILE' is LINE SEQUENTIAL and its file description entry specifies BLOCK CONTAINS 10 CHARACTERS — ISO §13.4.5.3 SR4: "If the LINE SEQUENTIAL phrase of the ORGANIZATION clause … is specified neither the BLOCK CONTAINS clause nor the RECORD CONTAINS c`

### `WE_REJECT:COBOLNET1657` × 6 — first: fz00307 (clause 15.72 ORD-MIN function)
- programs: fz00307, fz01977, fz02258, fz04333, fz04339, fz04470
- source: `programs/fz00307.cob`
- WiseOwl: `fz00307.cob(33,1): error COBOLNET1657: data item 'WS-DIGIT': the VALUE literal "01234567890123456789012345" is alphanumeric but the item is numeric — all literals in the VALUE clause of a numeric item shall be numeric (ISO §13.18.63.3 SR2); under --permissive it is stored as the number 0123456789012`

### `OUTPUT_DIFFERS:15.11` × 5 — first: fz00635 (clause 15.11 ATAN function)
- programs: fz00635, fz02183, fz03729, fz03731, fz05277
- source: `programs/fz00635.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ATAN(0) = 0000000000000{` | `ATAN(0) = +000000.00000000`
  - line 2: `ATAN(1) = 0000007853981F` | `ATAN(1) = +000000.78539816`
  - line 3: `ATAN(-1) = 0000007853981O` | `ATAN(-1) = -000000.78539816`
  - line 4: `ATAN(LARGE) = 0000015707863B` | `ATAN(LARGE) = +000001.57078632`
  - line 5: `ATAN(-LARGE) = 0000015707863K` | `ATAN(-LARGE) = -000001.57078632`

### `OUTPUT_DIFFERS:15.3.3.6.1` × 5 — first: fz00685 (clause 15.3.3.6.1 Common time formats > Offset time formats > Offset subformats)
- programs: fz00685, fz01458, fz01460, fz04554, fz05330
- source: `programs/fz00685.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `BASIC NEGATIVE: +0530` | `BASIC NEGATIVE: -0530`

### `OUTPUT_DIFFERS:8.8.1.4` × 5 — first: fz02179 (clause 8.8.1.4 Arithmetic expressions > Standard-binary arithmetic)
- programs: fz02179, fz02180, fz02952, fz04501, fz05276
- source: `programs/fz02179.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `ADD:      123.44` | `ADD: 99999876.56`
  - line 4: `GIVING:      246.89` | `GIVING: 99999753.11`

### `OUTPUT_DIFFERS:8.8.3` × 5 — first: fz01425 (clause 8.8.3 Concatenation expressions)
- programs: fz01425, fz02975, fz02974, fz03747, fz05295
- source: `programs/fz01425.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `CONCAT LIT-LIT: [HELLOWORLD                                        ]` | `CONCAT LIT-LIT: [HELLOWORLD                                        ]`

### `THEY_FAIL_RUN:15.13` × 5 — first: fz00211 (clause 15.13 BOOLEAN-OF-INTEGER function)
- programs: fz00211, fz00985, fz00984, fz03307, fz04081
- source: `programs/fz00211.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `BOOL OF 5 LEN 8: 0` | `<end>`
  - line 2: `BOOL OF 0 LEN 4: 0000` | `<end>`
  - line 3: `BOOL OF 15 LEN 4: 1111` | `<end>`
  - line 4: `BOOL OF 256 LEN 8: 00000000` | `<end>`
  - GnuCOBOL run exit 1: ``

### `WE_REJECT:COBOLNET0817` × 5 — first: fz00177 (clause 14.9.11 DISPLAY statement)
- programs: fz00177, fz01413, fz03522, fz04296, fz05595
- source: `programs/fz00177.cob`
- WiseOwl: `fz00177.cob(19,1): error COBOLNET0817: DISPLAY UPON 'SYSOUT': not a mnemonic-name declared in SPECIAL-NAMES (ISO §14.9.11.3 SR2 — mnemonic-name-1 shall be associated with an implementor device-name, §12.3.7.2 'device-name-1 IS mnemonic-name-3')`

### `WE_REJECT:COBOLNET0881,COBOLNET1517` × 5 — first: fz00552 (clause 15.41 FORMATTED-TIME function)
- programs: fz00552, fz01161, fz01266, fz02875, fz03483
- source: `programs/fz00552.cob`
- WiseOwl: `fz00552.cob(8,1): error COBOLNET0881: data item 'WS-RESULT-NATIONAL': USAGE NATIONAL may not be specified with an alphabetic or alphanumeric PICTURE (X(20)) — it admits boolean, national, national-edited, numeric, and numeric-edited pictures only (ISO §13.18.60.3 SR12; §13.18.40.3 SR30)`

### `WE_REJECT:COBOLNET0881,COBOLNET1740` × 5 — first: fz02177 (clause 15.78 REVERSE function)
- programs: fz02177, fz02325, fz02392, fz03803, fz05426
- source: `programs/fz02177.cob`
- WiseOwl: `fz02177.cob(6,1): error COBOLNET0881: data item 'WS-NATIONAL': USAGE NATIONAL may not be specified with an alphabetic or alphanumeric PICTURE (X(5)) — it admits boolean, national, national-edited, numeric, and numeric-edited pictures only (ISO §13.18.60.3 SR12; §13.18.40.3 SR30)`

### `WE_REJECT:COBOLNET0898,COBOLNET1627` × 5 — first: fz01402 (clause 15.78 REVERSE function)
- programs: fz01402, fz01403, fz04292, fz04648, fz05162
- source: `programs/fz01402.cob`
- WiseOwl: `fz01402.cob(6,1): error COBOLNET0898: data item 'WS-NATIONAL': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET0900,COBOLNET1740` × 5 — first: fz00590 (clause 13.18.16 CONTROL clause)
- programs: fz00590, fz00903, fz01687, fz03225, fz04775
- source: `programs/fz00590.cob`
- WiseOwl: `fz00590.cob(17,9): error COBOLNET1740: data item 'FILLER': the VALUE alphanumeric literal (3 characters) exceeds the item's 2 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET1651` × 5 — first: fz00380 (clause 14.9.43 STRING statement)
- programs: fz00380, fz00752, fz01152, fz01761, fz03475
- source: `programs/fz00380.cob`
- WiseOwl: `fz00380.cob(56,1): error COBOLNET1651: STRING INTO 'WS-DEST(1:3)': identifier-3 shall not be reference-modified (ISO §14.9.43.3 SR4)`

### `EXIT_DIFFERS:13.18.5` × 4 — first: fz02747 (clause 13.18.5 BASED clause)
- programs: fz02747, fz03519, fz03521, fz04295
- source: `programs/fz02747.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - WiseOwl run exit 1
  - GnuCOBOL run exit 11: ``

### `EXIT_DIFFERS:14.9.42` × 4 — first: fz00383 (clause 14.9.42 STOP statement)
- programs: fz00383, fz01155, fz01929, fz03477
- source: `programs/fz00383.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - WiseOwl run exit 1234
  - GnuCOBOL run exit 210: ``

### `OUTPUT_DIFFERS:13.18.49` × 4 — first: fz01917 (clause 13.18.49 SAME AS clause)
- programs: fz01917, fz01919, fz02693, fz04241
- source: `programs/fz01917.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 4: `GROUP ADD: A=10 B=` | `GROUP ADD: A=90 B=   `
  - line 5: `SIGNED COPY: 567Q` | `SIGNED COPY: -5678`
  - line 6: `ABS RESULT: 567H` | `ABS RESULT: +5678`

### `OUTPUT_DIFFERS:13.18.63` × 4 — first: fz00704 (clause 13.18.63 VALUE clause)
- programs: fz00704, fz03026, fz03798, fz05348
- source: `programs/fz00704.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `SIGNED: 12C` | `SIGNED: +123`

### `OUTPUT_DIFFERS:14.9.19` × 4 — first: fz00296 (clause 14.9.19 IF statement)
- programs: fz00296, fz01069, fz01844, fz04940
- source: `programs/fz00296.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `RESULT AFTER IF: 00001{` | `RESULT AFTER IF: +000010`

### `OUTPUT_DIFFERS:15.22` × 4 — first: fz00988 (clause 15.22 DATE-OF-INTEGER function)
- programs: fz00988, fz01763, fz02536, fz04084
- source: `programs/fz00988.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 4: `ZERO RESULT 0` | `ZERO RESULT 00000000`

### `OUTPUT_DIFFERS:15.25` × 4 — first: fz00314 (clause 15.25 DAY-TO-YYYYDDD function)
- programs: fz00314, fz01861, fz04182, fz04956
- source: `programs/fz00314.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 5: `TEST5: 0000000` | `TEST5: 2000000`

### `OUTPUT_DIFFERS:15.3.1.3` × 4 — first: fz00950 (clause 15.3.1.3 Format arguments to international date and time functions > Permissible values for data associated with calendar date formats)
- programs: fz00950, fz04819, fz05593, fz05594
- source: `programs/fz00950.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `Y2K: 20002000` | `Y2K: 020002000`
  - line 2: `HIGH: 20009999` | `HIGH: 020009999`
  - line 3: `LOW: 20001601` | `LOW: 020001601`

### `OUTPUT_DIFFERS:15.3.1.5` × 4 — first: fz03077 (clause 15.3.1.5 Format arguments to international date and time functions > Permissible values for data associated with ordinal date formats)
- programs: fz03077, fz03851, fz04623, fz04625
- source: `programs/fz03077.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `DATE-TO-YYYYMMDD: 20154498` | `DATE-TO-YYYYMMDD: 020154498`
  - line 2: `DAY-TO-YYYYDDD: 0` | `DAY-TO-YYYYDDD: 0020000`
  - line 3: `INTEGER-OF-DATE: 0015449H` | `INTEGER-OF-DATE: +00154498`
  - line 4: `INTEGER-OF-DAY: 00{` | `INTEGER-OF-DAY: +000`

### `OUTPUT_DIFFERS:15.78` × 4 — first: fz00627 (clause 15.78 REVERSE function)
- programs: fz00627, fz03724, fz04499, fz05273
- source: `programs/fz00627.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `NATIONAL: [HGF]` | `NATIONAL: [H G F ]`

### `OUTPUT_DIFFERS:15.96` × 4 — first: fz00741 (clause 15.96 TRIM function)
- programs: fz00741, fz01515, fz02289, fz05385
- source: `programs/fz00741.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 6: `TRIM NAT: [nat  str            ]` | `TRIM NAT: [       n a t     s t r                  ]`

### `OUTPUT_DIFFERS:8.8.1.2` × 4 — first: fz01230 (clause 8.8.1.2 Arithmetic expressions > Native, standard-binary, and standard-decimal arithmetic)
- programs: fz01230, fz02004, fz02779, fz05102
- source: `programs/fz01230.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `1: 000140{` | `1: +00014.00`
  - line 2: `2: 000200{` | `2: +00020.00`
  - line 3: `3: 000080{` | `3: +00008.00`
  - line 4: `4: 000080}` | `4: -00008.00`
  - line 5: `5: 000080}` | `5: -00008.00`

### `OUTPUT_DIFFERS:8.8.1.5` × 4 — first: fz00531 (clause 8.8.1.5 Arithmetic expressions > Standard-decimal arithmetic)
- programs: fz00531, fz01305, fz02079, fz04403
- source: `programs/fz00531.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ADD: 0057I` | `ADD: +005.79`
  - line 2: `SUB: 0033L` | `SUB: -003.33`
  - line 3: `MUL: 0056{` | `MUL: +005.60`
  - line 4: `DIV: 0037{` | `DIV: +003.70`
  - line 5: `ABS POS: 0012C` | `ABS POS: +001.23`

### `OUTPUT_DIFFERS:8.8.1.5.3` × 4 — first: fz00149 (clause 8.8.1.5.3 Arithmetic expressions > Standard-decimal arithmetic > Basic arithmetic operations in standard-decimal arithmetic)
- programs: fz00149, fz03245, fz04791, fz05565
- source: `programs/fz00149.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `ADD POS+NEG: 00000555F` | `ADD POS+NEG: +0000055.56`
  - line 2: `ADD POS+ZERO: 00001234E` | `ADD POS+ZERO: +0000123.45`
  - line 3: `ADD NEG+ZERO: 00000678R` | `ADD NEG+ZERO: -0000067.89`
  - line 4: `ADD POS-NEG: 00001913D` | `ADD POS-NEG: +0000191.34`
  - line 5: `SUB POS-NEG: 00001913D` | `SUB POS-NEG: +0000191.34`
  - WiseOwl run exit 1

### `THEY_FAIL_RUN:15.29` × 4 — first: fz00831 (clause 15.29 EXCEPTION-FILE-N function)
- programs: fz00831, fz03927, fz04701, fz04703
- source: `programs/fz00831.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `INITIAL: [00                  ]` | `<end>`
  - line 2: `STEP_1: [00                  ]` | `<end>`
  - GnuCOBOL run exit 1: ``

### `THEY_FAIL_RUN:15.31` × 4 — first: fz00321 (clause 15.31 EXCEPTION-LOCATION-N function)
- programs: fz00321, fz02644, fz03419, fz04967
- source: `programs/fz00321.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `HAS LOC` | `<end>`
  - line 2: `RESULT:` | `<end>`
  - GnuCOBOL run exit 1: ``

### `THEY_REJECT:14.9.4` × 4 — first: fz01005 (clause 14.9.4 CALL statement)
- programs: fz01005, fz01006, fz03329, fz04101
- source: `programs/fz01005.cob`
- GnuCOBOL: `unknown (signal)`

### `THEY_REJECT:8.8.4.2.4` × 4 — first: fz00712 (clause 8.8.4.2.4 Conditional expressions > Simple relation conditions > Comparison of numeric operands)
- programs: fz00712, fz00713, fz03807, fz04583
- source: `programs/fz00712.cob`
- GnuCOBOL: `fz00712.cob:9: error: syntax error, unexpected Identifier`

### `WE_REJECT:COBOLNET0711` × 4 — first: fz00523 (clause 14.9.29 RAISE statement)
- programs: fz00523, fz02846, fz03620, fz05168
- source: `programs/fz00523.cob`
- WiseOwl: `fz00523.cob(13,5): error COBOLNET0711: RAISE: 'WS-EXCEPTION-NAME' is not an exception-name of ISO/IEC 1989 §14.6.13.1 (and not a valid EC-USER- name; this implementation defines no EC-IMP- name, DOC-A.1-99)`

### `WE_REJECT:COBOLNET0809` × 4 — first: fz03756 (clause 8.8.4.2 Conditional expressions > Simple relation conditions)
- programs: fz03756, fz03758, fz04205, fz05304
- source: `programs/fz03756.cob`
- WiseOwl: `fz03756.cob(21,1): error COBOLNET0809: a MOVE operand shall not be of class index (ISO §14.9.25.3 SR1; §13.18.60.3 SR10 — only a SEARCH or SET statement, a relation condition, an intrinsic-function or inline-method argument, or a procedure-division / CALL / INVOKE USING phrase may reference an index`

### `WE_REJECT:COBOLNET0819,COBOLNET0898` × 4 — first: fz02251 (clause 13.18.63 VALUE clause)
- programs: fz02251, fz02290, fz02950, fz04488
- source: `programs/fz02251.cob`
- WiseOwl: `fz02251.cob(7,1): error COBOLNET0898: data item 'WS-NAT': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET0844,COBOLNET1627` × 4 — first: fz01495 (clause 15.3 Arguments)
- programs: fz01495, fz02269, fz03042, fz04590
- source: `programs/fz01495.cob`
- WiseOwl: `fz01495.cob(27,1): error COBOLNET1627: FUNCTION SUM argument-1 is of class alphanumeric; ISO §15.3 requires class numeric (§15.88.3 r1). --permissive accepts it as a coercion extension`

### `WE_REJECT:COBOLNET0898,COBOLNET1740` × 4 — first: fz00460 (clause 15.57 LOWER-CASE function)
- programs: fz00460, fz02656, fz03103, fz04203
- source: `programs/fz00460.cob`
- WiseOwl: `fz00460.cob(5,1): error COBOLNET1740: data item 'WS-INPUT': the VALUE alphanumeric literal (6 characters) exceeds the item's 5 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET1580,COBOLNET1708` × 4 — first: fz00762 (clause 13.18.62 VALIDATE-STATUS clause)
- programs: fz00762, fz01560, fz03884, fz04658
- source: `programs/fz00762.cob`
- WiseOwl: `fz00762.cob(7,28): error COBOLNET1708: the VALIDATE-STATUS clause of the §13.16.2 validation-clauses group: A data-division clause of the VALIDATE facility (the §13.16.2 validation-clauses group — CLASS, DEFAULT, DESTINATION, INVALID, PRESENT WHEN format 2, VARYING's validation leg, VALIDATE-STATUS/`

### `WE_REJECT:COBOLNET1627,COBOLNET1740` × 4 — first: fz00105 (clause 14.9.5 CANCEL statement)
- programs: fz00105, fz03402, fz04951, fz05444
- source: `programs/fz00105.cob`
- WiseOwl: `fz00105.cob(12,1): error COBOLNET1740: data item 'WS-TEXT': the VALUE alphanumeric literal (11 characters) exceeds the item's 10 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET2966` × 4 — first: fz01802 (clause 14.9.28 PERFORM statement)
- programs: fz01802, fz04282, fz04305, fz05057
- source: `programs/fz01802.cob`
- WiseOwl: `fz01802.cob(58,5): error COBOLNET2966: The EXIT statement shall appear in a sentence by itself that shall be the only sentence in the paragraph or in a section without paragraphs (ISO §14.9.14.3 SR1); this one shares its sentence with 2 other statements`

### `OUTPUT_DIFFERS:13.18.40` × 3 — first: fz01195 (clause 13.18.40 PICTURE clause)
- programs: fz01195, fz01968, fz03517
- source: `programs/fz01195.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `POSITIVE: 12345678I` | `POSITIVE: +123456789`
  - line 2: `NEGATIVE: 12345678R` | `NEGATIVE: -123456789`
  - line 4: `ZERO: 00000000{` | `ZERO: +000000000`

### `OUTPUT_DIFFERS:13.18.64` × 3 — first: fz00224 (clause 13.18.64 VARYING clause)
- programs: fz00224, fz00997, fz01770
- source: `programs/fz00224.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `SUM: 060{` | `SUM: +0600`

### `OUTPUT_DIFFERS:14.9.4` × 3 — first: fz01007 (clause 14.9.4 CALL statement)
- programs: fz01007, fz01781, fz04877
- source: `programs/fz01007.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `<end>` | `OK: 0000`

### `OUTPUT_DIFFERS:15.14` × 3 — first: fz00236 (clause 15.14 BYTE-LENGTH function)
- programs: fz00236, fz01783, fz04879
- source: `programs/fz00236.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `BYTE-LENGTH ALPHA: 000E` | `BYTE-LENGTH ALPHA: +0005`
  - line 2: `BYTE-LENGTH NATIONAL: 001{` | `BYTE-LENGTH NATIONAL: +0010`
  - line 3: `BYTE-LENGTH GROUP DEPENDING: 001B` | `BYTE-LENGTH GROUP DEPENDING: +0012`
  - line 4: `BYTE-LENGTH VAR GROUP LOGICAL: 001B` | `BYTE-LENGTH VAR GROUP LOGICAL: +0012`
  - line 5: `BYTE-LENGTH VAR GROUP PHYSICAL: 001B` | `BYTE-LENGTH VAR GROUP PHYSICAL: +0012`

### `OUTPUT_DIFFERS:15.48` × 3 — first: fz00494 (clause 15.48 INTEGER-OF-FORMATTED-DATE function)
- programs: fz00494, fz03590, fz04364
- source: `programs/fz00494.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `INT DATE: 000015414G` | `INT DATE: +0000154147`
  - line 2: `INT NAT DATE: 000015414G` | `INT NAT DATE: +0000154147`
  - line 4: `INT EMPTY: 000000000{` | `INT EMPTY: +0000000000`
  - line 5: `INT INVALID: 000000000{` | `INT INVALID: +0000000000`
  - line 6: `INT CT DATE: 000015414G` | `INT CT DATE: +0000154147`

### `THEY_FAIL_RUN:8.8.4.2.7` × 3 — first: fz00529 (clause 8.8.4.2.7 Conditional expressions > Simple relation conditions > Comparison of alphanumeric operands)
- programs: fz00529, fz02077, fz02851
- source: `programs/fz00529.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `STD A vs B: <` | `<end>`
  - line 2: `STD A vs C: =` | `<end>`
  - line 3: `STD D vs E: =` | `<end>`
  - line 4: `STD F vs G: =` | `<end>`
  - line 5: `STD H vs I: >` | `<end>`
  - GnuCOBOL run exit 1: ``

### `THEY_REJECT:13.18.63` × 3 — first: fz01476 (clause 13.18.63 VALUE clause)
- programs: fz01476, fz03800, fz04573
- source: `programs/fz01476.cob`
- GnuCOBOL: `fz01476.cob:9: error: syntax error, unexpected FROM`

### `THEY_REJECT:14.9.1` × 3 — first: fz00979 (clause 14.9.1 ACCEPT statement)
- programs: fz00979, fz01753, fz02527
- source: `programs/fz00979.cob`
- GnuCOBOL: `fz00979.cob:8: error: PICTURE clause required for 'WS-TIME-HHMMSS'`

### `THEY_REJECT:14.9.48` × 3 — first: fz01310 (clause 14.9.48 UNSTRING statement)
- programs: fz01310, fz03631, fz04404
- source: `programs/fz01310.cob`
- GnuCOBOL: `fz01310.cob:62: error: MOVE of figurative constant SPACE to numeric item used`

### `THEY_REJECT:15.58` × 3 — first: fz00002 (clause 15.58 LOWEST-ALGEBRAIC function)
- programs: fz00002, fz00774, fz04646
- source: `programs/fz00002.cob`
- GnuCOBOL: `fz00002.cob:12: error: syntax error, unexpected Identifier`

### `WE_REJECT:COBOL0308` × 3 — first: fz00535 (clause 14.9.48 UNSTRING statement)
- programs: fz00535, fz02083, fz02857
- source: `programs/fz00535.cob`
- WiseOwl: `fz00535.cob(52,10): error COBOL0308: unexpected '"A,B"'. A data-name is expected here, not a literal.`

### `WE_REJECT:COBOLNET0801,COBOLNET1625` × 3 — first: fz00207 (clause 15.84 SQRT function)
- programs: fz00207, fz02228, fz05245
- source: `programs/fz00207.cob`
- WiseOwl: `fz00207.cob(7,1): error COBOLNET0801: data item 'WS-RES-DECIMAL' (PICTURE S9(3)V9(34)) has 37 digit positions; ISO/IEC 1989 limits fixed-point items and literals to 31 digits (ISO §8.3.3.3.2)`

### `WE_REJECT:COBOLNET0898,COBOLNET0900,COBOLNET2455` × 3 — first: fz01940 (clause 13.18.24 FORMAT clause)
- programs: fz01940, fz01939, fz03929
- source: `programs/fz01940.cob`
- WiseOwl: `fz01940.cob(18,1): error COBOLNET0898: data item 'WS-BIT': the VALUE of a boolean data item shall be a boolean literal (B"…") or the figurative constant ZERO (ISO §13.18.63.3 SR10)`

### `WE_REJECT:COBOLNET0898,COBOLNET2306` × 3 — first: fz01153 (clause 14.9.43 STRING statement)
- programs: fz01153, fz02858, fz05178
- source: `programs/fz01153.cob`
- WiseOwl: `fz01153.cob(15,1): error COBOLNET0898: data item 'WS-NATIONAL-SRC': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET0900,COBOLNET2796` × 3 — first: fz00130 (clause 13.18.51 SELECT WHEN clause)
- programs: fz00130, fz04001, fz04782
- source: `programs/fz00130.cob`
- WiseOwl: `fz00130.cob(54,17): error COBOLNET2796: READ … NOT AT END: this IF statement is written without its explicit scope terminator END-IF, so it is a conditional statement, and the operand of this phrase is an imperative-statement — ISO §14.5.1: "Any statement with a conditional phrase that is not termin`

### `WE_REJECT:COBOLNET0900,COBOLNET3407` × 3 — first: fz00569 (clause 13.18.13 CODE-SET clause)
- programs: fz00569, fz03664, fz05211
- source: `programs/fz00569.cob`
- WiseOwl: `fz00569.cob(11,1): error COBOLNET0900: ORGANIZATION LINE SEQUENTIAL requires COBOL-2023 (targeting COBOL-2014) — the LINE SEQUENTIAL phrase of the ORGANIZATION clause (ISO §12.4.5.10.3 GR2; §9.1.7.2 splits sequential files into record- and line-delimited types)`

### `WE_REJECT:COBOLNET1517,COBOLNET1627,COBOLNET1740` × 3 — first: fz00113 (clause 15.3.3.6.2 Common time formats > Offset time formats > Permissible values for data associated with offset time formats)
- programs: fz00113, fz00886, fz01499
- source: `programs/fz00113.cob`
- WiseOwl: `fz00113.cob(5,1): error COBOLNET1740: data item 'WS-TIME-BASIC': the VALUE alphanumeric literal (11 characters) exceeds the item's 9 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET1708,COBOLNET1740` × 3 — first: fz02335 (clause 13.18.41 PRESENT WHEN clause)
- programs: fz02335, fz03108, fz05430
- source: `programs/fz02335.cob`
- WiseOwl: `fz02335.cob(9,4): error COBOLNET1740: data item 'WS-PRESENT-ITEM': the VALUE alphanumeric literal (7 characters) exceeds the item's 5 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET2406` × 3 — first: fz00327 (clause 13.18.44 REDEFINES clause)
- programs: fz00327, fz02790, fz03565
- source: `programs/fz00327.cob`
- WiseOwl: `fz00327.cob(17,1): error COBOLNET2406: data item 'WS-LARGE': the VALUE clause shall not be specified in a data description entry that contains a REDEFINES clause (ISO §13.18.63.3 SR12) — the redefined entry 'WS-SMALL' is the one that may carry an initial value`

### `OUTPUT_DIFFERS:13.18.21` × 2 — first: fz03247 (clause 13.18.21 ERASE clause)
- programs: fz03247, fz04022
- source: `programs/fz03247.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `BEFORE: AAAABBBBCCCCDDDDEEEE` | `BEFORE: AAAABBBBCCCCDDDDEEEE                                                            `
  - line 3: `AFTER: XXXXXXXXXXXXXXXXXXXX` | `AFTER: XXXXXXXXXXXXXXXXXXXX                                                            `

### `OUTPUT_DIFFERS:13.18.39` × 2 — first: fz03017 (clause 13.18.39 PAGE clause)
- programs: fz03017, fz05339
- source: `programs/fz03017.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `TC1: H=0001 FD=0000` | `TC1: H=0001 FD=    `
  - line 4: `TC1: LCH=0000 LD=0000` | `TC1: LCH=     LD=    `
  - line 5: `TC1: F=0000` | `TC1: F=    `

### `OUTPUT_DIFFERS:13.18.40.5` × 2 — first: fz00761 (clause 13.18.40.5 PICTURE clause > Editing rules)
- programs: fz00761, fz01534
- source: `programs/fz00761.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `FIXED POS:` | `FIXED POS:             `
  - line 2: `FIXED NEG:` | `FIXED NEG:             `
  - line 3: `FIXED ZERO:` | `FIXED ZERO:             `
  - line 4: `Z-SUP POS:` | `Z-SUP POS:             `
  - line 5: `Z-SUP NEG:` | `Z-SUP NEG:             `

### `OUTPUT_DIFFERS:14.9.20` × 2 — first: fz05469 (clause 14.9.20 INITIALIZE statement)
- programs: fz05469, fz05471
- source: `programs/fz05469.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `INIT REPL: NUM2=999I ALPHA2=WXYZ` | `INIT REPL: NUM2=+9999 ALPHA2=WXYZ`

### `OUTPUT_DIFFERS:14.9.36` × 2 — first: fz01229 (clause 14.9.36 ROLLBACK statement)
- programs: fz01229, fz02776
- source: `programs/fz01229.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `COUNT 01 RESULT 0000B` | `COUNT 01 RESULT +00002`

### `OUTPUT_DIFFERS:14.9.7` × 2 — first: fz04227 (clause 14.9.7 COMMIT statement)
- programs: fz04227, fz05001
- source: `programs/fz04227.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `AFTER COMMIT RESULT: 0001{` | `AFTER COMMIT RESULT: +00010`
  - line 9: `FINAL RESULT: 0002{` | `FINAL RESULT: +00020`

### `OUTPUT_DIFFERS:15.24` × 2 — first: fz00540 (clause 15.24 DAY-OF-INTEGER function)
- programs: fz00540, fz04410
- source: `programs/fz00540.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `DAY OF INT 00245154E IS 8313038` | `DAY OF INT +002451545 IS 8313038`

### `OUTPUT_DIFFERS:15.28` × 2 — first: fz02321 (clause 15.28 EXCEPTION-FILE function)
- programs: fz02321, fz03867
- source: `programs/fz02321.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `EXC-FILE NO ARG: [00                       ]` | `EXC-FILE NO ARG: [35TEST-FILE              ]`
  - line 3: `EXC-FILE LEN: 02` | `EXC-FILE LEN: 11`
  - line 4: `EXC-FILE NO ARG AFTER READ: [00                       ]` | `EXC-FILE NO ARG AFTER READ: [47TEST-FILE              ]`
  - line 6: `FINAL EXC-FILE: [00                       ]` | `FINAL EXC-FILE: [42TEST-FILE              ]`

### `OUTPUT_DIFFERS:15.32` × 2 — first: fz02564 (clause 15.32 EXCEPTION-STATEMENT function)
- programs: fz02564, fz03338
- source: `programs/fz02564.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `SIZE ERROR IN: [                                                               ]` | `SIZE ERROR IN: [                               ]`

### `OUTPUT_DIFFERS:15.59` × 2 — first: fz00258 (clause 15.59 MAX function)
- programs: fz00258, fz04128
- source: `programs/fz00258.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 5: `MAX NUM NEG: 05}` | `MAX NUM NEG: -050`
  - line 8: `MAX NUM BOUNDS: 99I` | `MAX NUM BOUNDS: +999`

### `OUTPUT_DIFFERS:15.71` × 2 — first: fz00024 (clause 15.71 ORD-MAX function)
- programs: fz00024, fz02347
- source: `programs/fz00024.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `ORD-MAX(A,B,C)= 000C` | `ORD-MAX(A,B,C)= +0003`
  - line 4: `ORD-MAX(F,G,H)= 000B` | `ORD-MAX(F,G,H)= +0002`
  - line 6: `ORD-MAX(I,J,K)= 000A` | `ORD-MAX(I,J,K)= +0001`
  - line 8: `ORD-MAX(L)= 000A` | `ORD-MAX(L)= +0001`
  - line 10: `ORD-MAX(O,P,Q)= 000A` | `ORD-MAX(O,P,Q)= +0001`

### `OUTPUT_DIFFERS:8.8.4.13` × 2 — first: fz00669 (clause 8.8.4.13 Conditional expressions > Order of evaluation of conditions)
- programs: fz00669, fz05315
- source: `programs/fz00669.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 9: `XOR FALSE` | `XOR TRUE`

### `THEY_FAIL_RUN:15.16` × 2 — first: fz04520 (clause 15.16 CHAR-NATIONAL function)
- programs: fz04520, fz05294
- source: `programs/fz04520.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `CHAR-NATIONAL(65): @` | `<end>`
  - line 2: `BYTE-LENGTH: 0002` | `<end>`
  - line 3: `EQUALITY CHECK PASSED` | `<end>`
  - GnuCOBOL run exit 1: ``

### `THEY_FAIL_RUN:8.8.4.2.11` × 2 — first: fz02349 (clause 8.8.4.2.11 Conditional expressions > Simple relation conditions > Locale-based comparison)
- programs: fz02349, fz03899
- source: `programs/fz02349.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `STANDARD ABC vs ABD: <` | `<end>`
  - line 2: `LOCALE ABC vs ABD: <` | `<end>`
  - line 3: `STANDARD ABC vs ABC : =` | `<end>`
  - line 4: `LOCALE ABC vs ABC : =` | `<end>`
  - line 5: `STANDARD ' ABC' vs ABC: <` | `<end>`
  - GnuCOBOL run exit 1: ``

### `THEY_REJECT:13.18.24` × 2 — first: fz01938 (clause 13.18.24 FORMAT clause)
- programs: fz01938, fz05035
- source: `programs/fz01938.cob`
- GnuCOBOL: `fz01938.cob:38: error: syntax error, unexpected FUNCTION DISPLAY-OF`

### `THEY_REJECT:13.18.32` × 2 — first: fz01393 (clause 13.18.32 JUSTIFIED clause)
- programs: fz01393, fz02168
- source: `programs/fz01393.cob`
- GnuCOBOL: `fz01393.cob:15: error: 'WS-DST-BIT-REC' cannot have JUSTIFIED RIGHT clause`

### `THEY_REJECT:13.18.44` × 2 — first: fz01876 (clause 13.18.44 REDEFINES clause)
- programs: fz01876, fz02649
- source: `programs/fz01876.cob`
- GnuCOBOL: `fz01876.cob:15: error: 'COMP-1' is not defined, but is a reserved word in another dialect`

### `THEY_REJECT:13.18.60` × 2 — first: fz00582 (clause 13.18.60 USAGE clause)
- programs: fz00582, fz03679
- source: `programs/fz00582.cob`
- GnuCOBOL: `fz00582.cob:6: error: syntax error, unexpected UNSIGNED`

### `THEY_REJECT:14.9.29` × 2 — first: fz02071 (clause 14.9.29 RAISE statement)
- programs: fz02071, fz04393
- source: `programs/fz02071.cob`
- GnuCOBOL: `fz02071.cob:13: error: invalid expression: unfinished expression`

### `THEY_REJECT:14.9.50` × 2 — first: fz03574 (clause 14.9.50 VALIDATE statement)
- programs: fz03574, fz04348
- source: `programs/fz03574.cob`
- GnuCOBOL: `fz03574.cob:12: error: 'END-VALIDATE' is not defined`

### `THEY_REJECT:15.16` × 2 — first: fz00650 (clause 15.16 CHAR-NATIONAL function)
- programs: fz00650, fz05293
- source: `programs/fz00650.cob`
- GnuCOBOL: `fz00650.cob:12: error: syntax error, unexpected FUNCTION DISPLAY-OF`

### `THEY_REJECT:15.27` × 2 — first: fz02010 (clause 15.27 E function)
- programs: fz02010, fz04334
- source: `programs/fz02010.cob`
- GnuCOBOL: `fz02010.cob:5: error: syntax error, unexpected Identifier`

### `THEY_REJECT:15.3` × 2 — first: fz00720 (clause 15.3 Arguments)
- programs: fz00720, fz03044
- source: `programs/fz00720.cob`
- GnuCOBOL: `fz00720.cob:25: error: syntax error, unexpected ALL`

### `THEY_REJECT:15.4.1` × 2 — first: fz01067 (clause 15.4.1 Numeric and integer functions)
- programs: fz01067, fz02615
- source: `programs/fz01067.cob`
- GnuCOBOL: `fz01067.cob:8: error: syntax error, unexpected Identifier`

### `THEY_REJECT:15.68` × 2 — first: fz02517 (clause 15.68 NUMVAL-C function)
- programs: fz02517, fz02519
- source: `programs/fz02517.cob`
- GnuCOBOL: `fz02517.cob:26: error: syntax error, unexpected comma delimiter, expecting )`

### `THEY_REJECT:15.76` × 2 — first: fz01047 (clause 15.76 RANGE function)
- programs: fz01047, fz01048
- source: `programs/fz01047.cob`
- GnuCOBOL: `fz01047.cob:16: error: 'COMP-3' is not defined, but is a reserved word in another dialect`

### `THEY_REJECT:15.84` × 2 — first: fz00209 (clause 15.84 SQRT function)
- programs: fz00209, fz04851
- source: `programs/fz00209.cob`
- GnuCOBOL: `fz00209.cob:7: error: syntax error, unexpected Identifier`

### `THEY_REJECT:8.8.4.8` × 2 — first: fz04309 (clause 8.8.4.8 Conditional expressions > Simple omitted argument condition)
- programs: fz04309, fz05083
- source: `programs/fz04309.cob`
- GnuCOBOL: `fz04309.cob:183: error: executable program requested but PROCEDURE/ENTRY has USING clause`

### `WE_HANG:13.18.64` × 2 — first: fz01771 (clause 13.18.64 VARYING clause)
- programs: fz01771, fz04092
- source: `programs/fz01771.cob`

### `WE_HANG:14.9.28` × 2 — first: fz02574 (clause 14.9.28 PERFORM statement)
- programs: fz02574, fz04123
- source: `programs/fz02574.cob`

### `WE_REJECT:COBOLNET0809,COBOLNET0898,COBOLNET1694,COBOLNET2326` × 2 — first: fz03431 (clause 15.63 MIN function)
- programs: fz03431, fz04902
- source: `programs/fz03431.cob`
- WiseOwl: `fz03431.cob(11,1): error COBOLNET0898: data item 'WS-NAT1': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET0809,COBOLNET0898,COBOLNET1740,COBOLNET2168` × 2 — first: fz01032 (clause 15.59 MAX function)
- programs: fz01032, fz01034
- source: `programs/fz01032.cob`
- WiseOwl: `fz01032.cob(9,1): error COBOLNET1740: data item 'WS-ALPHA-2': the VALUE alphanumeric literal (6 characters) exceeds the item's 5 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET0863` × 2 — first: fz02338 (clause 14.9.51 WRITE statement)
- programs: fz02338, fz04091
- source: `programs/fz02338.cob`
- WiseOwl: `fz02338.cob(11,23): error COBOLNET0863: RECORD KEY 'IDX-KEY' is a category-numeric item; data-name-1 shall reference a data item of category alphanumeric or category national (ISO §12.4.5.12.3 SR2)`

### `WE_REJECT:COBOLNET0864,COBOLNET0900` × 2 — first: fz01389 (clause 14.9.30 READ statement)
- programs: fz01389, fz03712
- source: `programs/fz01389.cob`
- WiseOwl: `fz01389.cob(65,5): error COBOLNET0864: READ … KEY on 'RND-FILE' is a Format-2 phrase and cannot combine with NEXT/PREVIOUS/AT END (ISO §14.9.30 general formats)`

### `WE_REJECT:COBOLNET0865` × 2 — first: fz01951 (clause 14.9.10 DELETE statement)
- programs: fz01951, fz05048
- source: `programs/fz01951.cob`
- WiseOwl: `fz01951.cob(21,5): error COBOLNET0865: DELETE RECORD shall not be specified for sequential-organization file 'TEST-FILE' (ISO §14.9.10.3 SR1)`

### `WE_REJECT:COBOLNET0881,COBOLNET1517,COBOLNET1740` × 2 — first: fz00388 (clause 15.39 FORMATTED-DATE function)
- programs: fz00388, fz02711
- source: `programs/fz00388.cob`
- WiseOwl: `fz00388.cob(7,1): error COBOLNET1740: data item 'WS-FMT-DYM': the VALUE alphanumeric literal (11 characters) exceeds the item's 10 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET0898,COBOLNET1517` × 2 — first: fz01937 (clause 15.39 FORMATTED-DATE function)
- programs: fz01937, fz02815
- source: `programs/fz01937.cob`
- WiseOwl: `fz01937.cob(11,1): error COBOLNET0898: data item 'WS-FMT-NAT': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET0898,COBOLNET1517,COBOLNET1740` × 2 — first: fz01935 (clause 15.39 FORMATTED-DATE function)
- programs: fz01935, fz02683
- source: `programs/fz01935.cob`
- WiseOwl: `fz01935.cob(9,1): error COBOLNET1740: data item 'WS-FMT-DYM': the VALUE alphanumeric literal (11 characters) exceeds the item's 10 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET0898,COBOLNET1694,COBOLNET2168` × 2 — first: fz00334 (clause 15.63 MIN function)
- programs: fz00334, fz00333
- source: `programs/fz00334.cob`
- WiseOwl: `fz00334.cob(13,1): error COBOLNET2168: data item 'WS-INX1': the VALUE clause shall not be specified with USAGE INDEX (a data item of class index — ISO §13.16.3 SR10)`

### `WE_REJECT:COBOLNET0899` × 2 — first: fz04876 (clause 14.9.4 CALL statement)
- programs: fz04876, fz05397
- source: `programs/fz04876.cob`
- WiseOwl: `fz04876.cob(24,1): error COBOLNET0899: CALL … USING BY VALUE belongs to the program-prototype CALL (ISO §14.9.4.2 Format 2), which the AS phrase selects — Format 1’s USING admits BY REFERENCE and BY CONTENT only`

### `WE_REJECT:COBOLNET0900,COBOLNET2966` × 2 — first: fz00589 (clause 13.18.16 CONTROL clause)
- programs: fz00589, fz04460
- source: `programs/fz00589.cob`
- WiseOwl: `fz00589.cob(32,1): error COBOLNET2966: The EXIT statement shall appear in a sentence by itself that shall be the only sentence in the paragraph or in a section without paragraphs (ISO §14.9.14.3 SR1); this one shares its sentence with 4 other statements`

### `WE_REJECT:COBOLNET1625,COBOLNET1627` × 2 — first: fz03826 (clause 8.8.1.4.2 Arithmetic expressions > Standard-binary arithmetic > Standard-binary intermediate data item)
- programs: fz03826, fz04162
- source: `programs/fz03826.cob`
- WiseOwl: `fz03826.cob(14,1): error COBOLNET1625: data item 'WS-TEN-E20': the numeric literal 100000000000000000000 in the VALUE clause is not a permissible value in the range the PICTURE indicates — it is not representable without truncation of leading or trailing nonzero digits (ISO §13.18.63.3 SR2)`

### `WE_REJECT:COBOLNET1625,COBOLNET1686` × 2 — first: fz04310 (clause 8.8.4.8 Conditional expressions > Simple omitted argument condition)
- programs: fz04310, fz05084
- source: `programs/fz04310.cob`
- WiseOwl: `fz04310.cob(6,1): error COBOLNET1625: data item 'WS-ARG1': the numeric literal 42 in the VALUE clause is not a permissible value in the range the PICTURE indicates — it is not representable without truncation of leading or trailing nonzero digits (ISO §13.18.63.3 SR2)`

### `WE_REJECT:COBOLNET1627,COBOLNET1663` × 2 — first: fz00005 (clause 15.85 STANDARD-COMPARE function)
- programs: fz00005, fz00778
- source: `programs/fz00005.cob`
- WiseOwl: `fz00005.cob(21,1): error COBOLNET1627: FUNCTION STANDARD-COMPARE argument-1 is of class numeric; ISO §15.3 requires class alphabetic, alphanumeric, alphanumeric (numeric-edited), national (numeric-edited) or national (§15.85.3 r1/r2/r6). --permissive accepts it as a coercion extension`

### `WE_REJECT:COBOLNET2159` × 2 — first: fz02067 (clause 13.18.22 EXTERNAL clause)
- programs: fz02067, fz02841
- source: `programs/fz02067.cob`
- WiseOwl: `fz02067.cob(8,1): error COBOLNET2159: data item 'WS-OTHER-DATA': the externalized name 'MY_EXTERNAL_RECORD' is already the externalized name of data item 'WS-EXT-DATA' in this source element; "In the same source element, the externalized name of the subject of the entry that includes the EXTERNAL cl`

### `WE_REJECT:COBOLNET2168,COBOLNET2576` × 2 — first: fz00518 (clause 15.2 Types of functions)
- programs: fz00518, fz02745
- source: `programs/fz00518.cob`
- WiseOwl: `fz00518.cob(8,35): error COBOLNET2576: NULL is the predefined address / object reference — an identifier (ISO §8.4.3.1.2 Format 8; §8.4.3.7), not a literal or a figurative constant — and ISO §8.4.3.10.3 SR1 admits it only as a sending operand in an INITIALIZE or a SET statement, as an argument in a `

### `WE_REJECT:COBOLNET2532` × 2 — first: fz01170 (clause 15.34 EXP function)
- programs: fz01170, fz05323
- source: `programs/fz01170.cob`
- WiseOwl: `fz01170.cob(14,1): error COBOLNET2532: a numeric operand compared with an operand of class alphanumeric, alphabetic or national shall be "an integer literal or an integer numeric data item of usage display or national" (ISO §8.8.4.2.5; §8.8.4.2.1 item 6 is the only such pair the standard defines) — `

### `WE_REJECT:COBOLNET2797` × 2 — first: fz00031 (clause 15.47 INTEGER-OF-DAY function)
- programs: fz00031, fz02812
- source: `programs/fz00031.cob`
- WiseOwl: `fz00031.cob(11,1): error COBOLNET2797: PROCEDURE DIVISION: paragraph MAIN-PARA is not in a section — the procedure division has sections, so it is §14.2.1 Format 1 (with-sections), and §14.4.1: "If one paragraph is in a section, all paragraphs shall be in sections".`

### `WE_REJECT:COBOLNET2968` × 2 — first: fz00381 (clause 14.9.42 STOP statement)
- programs: fz00381, fz01157
- source: `programs/fz00381.cob`
- WiseOwl: `fz00381.cob(13,1): error COBOLNET2968: The STOP statement shall be specified only as the last statement in any discreet block of code (ISO §14.9.42.3 SR1); this one is followed by the imperative statement STOP in the same sequence, which can never execute`

### `OUTPUT_DIFFERS:13.18.14` × 1 — first: fz02475 (clause 13.18.14 COLUMN clause)
- programs: fz02475
- source: `programs/fz02475.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `COL 01 ITEM-A AT ABS 5: AAAAA***` | `COL 01 ITEM-A AT ABS 5: AAAAA***        `

### `OUTPUT_DIFFERS:13.18.20` × 1 — first: fz01526 (clause 13.18.20 Entry-name clause)
- programs: fz01526
- source: `programs/fz01526.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `DATA MOVE:    XY` | `DATA MOVE:    XY     `

### `OUTPUT_DIFFERS:13.18.23` × 1 — first: fz00911 (clause 13.18.23 FOREGROUND-COLOR clause)
- programs: fz00911
- source: `programs/fz00911.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 5: `NEGATIVE VALUE DETECTED: 000Q` | `NEGATIVE VALUE DETECTED: -0008`

### `OUTPUT_DIFFERS:13.18.26` × 1 — first: fz00511 (clause 13.18.26 FULL clause)
- programs: fz00511
- source: `programs/fz00511.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `NUM:` | `NUM: +`

### `OUTPUT_DIFFERS:13.18.27` × 1 — first: fz00683 (clause 13.18.27 GLOBAL clause)
- programs: fz00683
- source: `programs/fz00683.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `DIFFERENCE: 0444M` | `DIFFERENCE: -04444`

### `OUTPUT_DIFFERS:13.18.33` × 1 — first: fz05114 (clause 13.18.33 Level-number)
- programs: fz05114
- source: `programs/fz05114.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `ARRAY D: 0000000099` | `ARRAY D:         99`

### `OUTPUT_DIFFERS:13.18.35` × 1 — first: fz03480 (clause 13.18.35 LINE clause)
- programs: fz03480
- source: `programs/fz03480.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `LINE NUMBER IS 5: 0000050{` | `LINE NUMBER IS 5: +000005.00`
  - line 3: `LINE PLUS 10: 0000150{` | `LINE PLUS 10: +000015.00`
  - line 5: `LINE RESULT: 0000220{` | `LINE RESULT: +000022.00`
  - line 6: `LINE NUMBER IS 10: 0000100{` | `LINE NUMBER IS 10: +000010.00`

### `OUTPUT_DIFFERS:13.18.37` × 1 — first: fz03296 (clause 13.18.37 NEXT GROUP clause)
- programs: fz03296
- source: `programs/fz03296.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `ABSOLUTE OFFSET NEEDED: 000I` | `ABSOLUTE OFFSET NEEDED: +0009`

### `OUTPUT_DIFFERS:13.18.4` × 1 — first: fz04514 (clause 13.18.4 BACKGROUND-COLOR clause)
- programs: fz04514
- source: `programs/fz04514.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 6: `UNDERFLOW DETECTED: J` | `UNDERFLOW DETECTED: -1`

### `OUTPUT_DIFFERS:13.18.40.6` × 1 — first: fz05017 (clause 13.18.40.6 PICTURE clause > Precedence rules)
- programs: fz05017
- source: `programs/fz05017.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 6: `EDIT6: 1,234.50E+001` | `EDIT6: 2,345.000000`

### `OUTPUT_DIFFERS:13.18.45` × 1 — first: fz04158 (clause 13.18.45 RENAMES clause)
- programs: fz04158
- source: `programs/fz04158.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 9: `RANGE CONTENTS: 005` | `RANGE CONTENTS: 5`
  - line 10: `FIELD-B AFTER ADD: 006` | `FIELD-B AFTER ADD: 501`

### `OUTPUT_DIFFERS:13.18.56` × 1 — first: fz02402 (clause 13.18.56 TO clause)
- programs: fz02402
- source: `programs/fz02402.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `ACCEPTED AGE: 00` | `ACCEPTED AGE:`

### `OUTPUT_DIFFERS:13.18.57` × 1 — first: fz00574 (clause 13.18.57 TYPE clause)
- programs: fz00574
- source: `programs/fz00574.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 9: `FINAL DECK CNT: 102` | `FINAL DECK CNT: 993`

### `OUTPUT_DIFFERS:13.18.58` × 1 — first: fz01169 (clause 13.18.58 TYPEDEF clause)
- programs: fz01169
- source: `programs/fz01169.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 4: `DIFFERENCE: 0444D` | `DIFFERENCE: +04444`

### `OUTPUT_DIFFERS:13.18.60` × 1 — first: fz01358 (clause 13.18.60 USAGE clause)
- programs: fz01358
- source: `programs/fz01358.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 7: `BINARY-DOUBLE SIGNED: -0000000000100000000` | `BINARY-DOUBLE SIGNED: -00000000000100000000`
  - line 15: `BINARY-DOUBLE SIGNED: -0000000000200000000` | `BINARY-DOUBLE SIGNED: -00000000000200000000`
  - line 23: `BINARY-DOUBLE SIGNED: -0000000000300000000` | `BINARY-DOUBLE SIGNED: -00000000000300000000`
  - line 31: `BINARY-DOUBLE SIGNED: -0000000000400000000` | `BINARY-DOUBLE SIGNED: -00000000000400000000`
  - line 35: `COMP SHORT: 67890` | `COMP SHORT: +67890`

### `OUTPUT_DIFFERS:13.18.62` × 1 — first: fz02311 (clause 13.18.62 VALIDATE-STATUS clause)
- programs: fz02311
- source: `programs/fz02311.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `STATUS: 000{` | `STATUS: +0000`

### `OUTPUT_DIFFERS:14.9.14` × 1 — first: fz02773 (clause 14.9.14 EXIT statement)
- programs: fz02773
- source: `programs/fz02773.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `RESULT AFTER EXIT PERFORM: 0000A` | `RESULT AFTER EXIT PERFORM: +00001`

### `OUTPUT_DIFFERS:14.9.17` × 1 — first: fz01959 (clause 14.9.17 GO TO statement)
- programs: fz01959
- source: `programs/fz01959.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `<end>` | `JUMPED FROM A WITH INDEX 1`
  - line 2: `<end>` | `UNREACHABLE B`

### `OUTPUT_DIFFERS:14.9.43` × 1 — first: fz05024 (clause 14.9.43 STRING statement)
- programs: fz05024
- source: `programs/fz05024.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 9: `EMPTY-SRC: [ABC                 ]` | `EMPTY-SRC: [ ABC                ]`

### `OUTPUT_DIFFERS:14.9.48` × 1 — first: fz02856 (clause 14.9.48 UNSTRING statement)
- programs: fz02856
- source: `programs/fz02856.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `TEST 1: DELIM=, CNT=01 TALLY=03` | `TEST 1: DELIM=, CNT=01 TALLY=97`

### `OUTPUT_DIFFERS:14.9.50` × 1 — first: fz02026 (clause 14.9.50 VALIDATE statement)
- programs: fz02026
- source: `programs/fz02026.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 4: `RELATION ITEM: 01}` | `RELATION ITEM: -010`

### `OUTPUT_DIFFERS:14.9.6` × 1 — first: fz02741 (clause 14.9.6 CLOSE statement)
- programs: fz02741
- source: `programs/fz02741.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `READ OK:` | `READ OK:           `

### `OUTPUT_DIFFERS:15.100` × 1 — first: fz00064 (clause 15.100 YEAR-TO-YYYY function)
- programs: fz00064
- source: `programs/fz00064.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `CASE 1 (arg1=0, no arg2/3): 200{` | `CASE 1 (arg1=0, no arg2/3): +2000`
  - line 2: `CASE 2 (arg1=49, no arg2/3): 204I` | `CASE 2 (arg1=49, no arg2/3): +2049`
  - line 3: `CASE 3 (arg1=50, no arg2/3): 205{` | `CASE 3 (arg1=50, no arg2/3): +2050`
  - line 4: `CASE 4 (arg1=99, no arg2/3): 199I` | `CASE 4 (arg1=99, no arg2/3): +1999`
  - line 5: `CASE 5 (arg1=0, arg2=23, arg3=2000): 200{` | `CASE 5 (arg1=0, arg2=23, arg3=2000): +2000`

### `OUTPUT_DIFFERS:15.23` × 1 — first: fz03786 (clause 15.23 DATE-TO-YYYYMMDD function)
- programs: fz03786
- source: `programs/fz03786.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 5: `YEAR 00 WIND 10: 00000000` | `YEAR 00 WIND 10: 20000000`

### `OUTPUT_DIFFERS:15.3` × 1 — first: fz02268 (clause 15.3 Arguments)
- programs: fz02268
- source: `programs/fz02268.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `SUM: 0000150{` | `SUM: +000015.00`

### `OUTPUT_DIFFERS:15.3.1` × 1 — first: fz02455 (clause 15.3.1 Format arguments to international date and time functions)
- programs: fz02455
- source: `programs/fz02455.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `TEST-DATE RESULT: 0` | `TEST-DATE RESULT: 000000000`
  - line 3: `TEST-DATE INVALID MONTH: 2` | `TEST-DATE INVALID MONTH: 000000002`
  - line 4: `TEST-DATE INVALID DAY: 3` | `TEST-DATE INVALID DAY: 000000003`
  - line 5: `TEST-DATE NON-NUMERIC-LIKE: 2` | `TEST-DATE NON-NUMERIC-LIKE: 000000002`
  - line 7: `TEST-DAY STRING: 1` | `TEST-DAY STRING: 00000`

### `OUTPUT_DIFFERS:15.3.1.7` × 1 — first: fz03760 (clause 15.3.1.7 Format arguments to international date and time functions > Permissible values for data associated with week date formats)
- programs: fz03760
- source: `programs/fz03760.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `TEST DATE 20230104: 0` | `TEST DATE 20230104: 000000000`
  - line 2: `TEST DATE 99991231: 0` | `TEST DATE 99991231: 000000000`
  - line 3: `TEST DATE 16000104: 1` | `TEST DATE 16000104: 000000001`
  - line 4: `TEST DATE 15991231: 1` | `TEST DATE 15991231: 000000001`
  - line 5: `TEST DATE 20240101: 0` | `TEST DATE 20240101: 000000000`

### `OUTPUT_DIFFERS:15.3.2` × 1 — first: fz01340 (clause 15.3.2 Time formats)
- programs: fz01340
- source: `programs/fz01340.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `FORMATTED UTC: 2026-10-21T00:39:49.186241` | `FORMATTED UTC: 2026-10-21T00:39:47.280500`
  - line 4: `FORMATTED LOCAL: 2026102100394918-0700` | `FORMATTED LOCAL: 2026102100394728-0700`
  - line 6: `OFFSET TIME: 2026-10-21T00:39:49.186615-07:00` | `OFFSET TIME: 2026-10-21T00:39:47.280520-07:00`

### `OUTPUT_DIFFERS:15.3.3.3` × 1 — first: fz04134 (clause 15.3.3.3 Common time formats > Permissible values for data associated with common time formats)
- programs: fz04134
- source: `programs/fz04134.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 6: `REV EMPTY:` | `REV EMPTY:                     `

### `OUTPUT_DIFFERS:15.3.3.6` × 1 — first: fz00550 (clause 15.3.3.6 Common time formats > Offset time formats)
- programs: fz00550
- source: `programs/fz00550.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `HOURS: 00{ MINUTES: 00` | `HOURS: +000 MINUTES: 00`
  - line 5: `INTEGER PART: 000012C` | `INTEGER PART: +0000123`
  - line 6: `EXP10 TEST: 000000099` | `EXP10 TEST: 0000000.99`

### `OUTPUT_DIFFERS:15.3.3.6.2` × 1 — first: fz01661 (clause 15.3.3.6.2 Common time formats > Offset time formats > Permissible values for data associated with offset time formats)
- programs: fz01661
- source: `programs/fz01661.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 2: `EXTENDED DT: 20261021010905` | `EXTENDED DT: 20261021010903`
  - line 3: `STEP 01 VAL 00001234N` | `STEP 01 VAL -0000123.45`
  - line 4: `STEP 02 VAL 00002469}` | `STEP 02 VAL -0000246.90`
  - line 5: `STEP 03 VAL 00003703N` | `STEP 03 VAL -0000370.35`
  - line 6: `STEP 04 VAL 00004938}` | `STEP 04 VAL -0000493.80`

### `OUTPUT_DIFFERS:15.3.3.7` × 1 — first: fz02272 (clause 15.3.3.7 Common time formats > Combined date and time formats)
- programs: fz02272
- source: `programs/fz02272.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `CURRENT DATE: 2026102102012639-0700` | `CURRENT DATE: 2026102102012462-0700`
  - line 2: `CONVERTED SPACES: 2026102102012639-0700-----` | `CONVERTED SPACES: 2026102102012462-0700-----`

### `OUTPUT_DIFFERS:15.50` × 1 — first: fz03011 (clause 15.50 LENGTH function)
- programs: fz03011
- source: `programs/fz03011.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 4: `LENGTH OF T: 15` | `LENGTH OF T: 000000015`

### `OUTPUT_DIFFERS:15.63` × 1 — first: fz01107 (clause 15.63 MIN function)
- programs: fz01107
- source: `programs/fz01107.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `MIN INT: 00001}` | `MIN INT: -000010`
  - line 2: `MIN DEC: 0000022E` | `MIN DEC: +000002.25`
  - line 4: `MIN NAT: WXY` | `MIN NAT:  W X Y`

### `OUTPUT_DIFFERS:8.8.4.11` × 1 — first: fz01448 (clause 8.8.4.11 Conditional expressions > Complex Combined conditions)
- programs: fz01448
- source: `programs/fz01448.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 3: `MAX: 000E` | `MAX: +0005`
  - line 4: `MIN: 000L` | `MIN: -0003`
  - line 5: `SUM: 000B` | `SUM: +0002`
  - line 6: `DIFF: 000N` | `DIFF: -0005`

### `OUTPUT_DIFFERS:8.8.4.2.12` × 1 — first: fz00701 (clause 8.8.4.2.12 Conditional expressions > Simple relation conditions > Comparison of strongly-typed group items)
- programs: fz00701
- source: `programs/fz00701.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 4: `E<A: LESS` | `E<A: NOT LESS`

### `OUTPUT_DIFFERS:8.8.4.2.17` × 1 — first: fz03019 (clause 8.8.4.2.17 Conditional expressions > Simple relation conditions > Comparison of a variable-length group with a compatible group)
- programs: fz03019
- source: `programs/fz03019.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `SET LEN 001 DATA` | `SET LEN 001 DATA  `

### `OUTPUT_DIFFERS:8.8.4.2.4` × 1 — first: fz02259 (clause 8.8.4.2.4 Conditional expressions > Simple relation conditions > Comparison of numeric operands)
- programs: fz02259
- source: `programs/fz02259.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 5: `SUM-ZERO: 00000000{` | `SUM-ZERO: +0000000.00`
  - line 6: `DIFF-SMALL: 00000000B` | `DIFF-SMALL: +0000000.02`

### `OUTPUT_DIFFERS:8.8.4.2.6` × 1 — first: fz01256 (clause 8.8.4.2.6 Conditional expressions > Simple relation conditions > Comparison of alphanumeric and national operands)
- programs: fz01256
- source: `programs/fz01256.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `LESS THAN` | `GREATER THAN`
  - line 2: `LESS THAN AFTER UP` | `GREATER THAN AFTER UP`

### `OUTPUT_DIFFERS:8.8.4.2.8` × 1 — first: fz04725 (clause 8.8.4.2.8 Conditional expressions > Simple relation conditions > Comparison of boolean operands)
- programs: fz04725
- source: `programs/fz04725.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 7: `FINAL MSG:` | `FINAL MSG:           `

### `OUTPUT_DIFFERS:8.8.4.9` × 1 — first: fz03242 (clause 8.8.4.9 Conditional expressions > Complex conditions)
- programs: fz03242
- source: `programs/fz03242.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `AND: AFC` | `AND: AFC                 `

### `THEY_FAIL_RUN:15.3.3.7` × 1 — first: fz04595 (clause 15.3.3.7 Common time formats > Combined date and time formats)
- programs: fz04595
- source: `programs/fz04595.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `INTEGER OF DATE: 00000000` | `<end>`
  - line 2: `SECONDS FROM TIME: 000000` | `<end>`
  - line 3: `INTEGER OF FRACTION: 1` | `<end>`
  - line 4: `SIGN OF FRACTION: J` | `<end>`
  - GnuCOBOL run exit 6: ``

### `THEY_FAIL_RUN:8.8.4.2.4` × 1 — first: fz04582 (clause 8.8.4.2.4 Conditional expressions > Simple relation conditions > Comparison of numeric operands)
- programs: fz04582
- source: `programs/fz04582.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 4: `ZERO DEC: EQUAL` | `<end>`
  - line 5: `POS ZERO: GT` | `<end>`
  - line 6: `NEG ZERO: LT` | `<end>`
  - GnuCOBOL run exit 11: ``

### `THEY_FAIL_RUN:8.8.4.2.8` × 1 — first: fz05500 (clause 8.8.4.2.8 Conditional expressions > Simple relation conditions > Comparison of boolean operands)
- programs: fz05500
- source: `programs/fz05500.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 1: `EQUAL LENGTH: EQUAL` | `<end>`
  - line 2: `EQUAL LENGTH: EQUAL` | `<end>`
  - line 3: `UNEQUAL LENGTH: EQUAL` | `<end>`
  - line 4: `UNEQUAL LENGTH: EQUAL` | `<end>`
  - line 5: `UNEQUAL LENGTH: EQUAL` | `<end>`
  - GnuCOBOL run exit 1: ``

### `THEY_FAIL_RUN:8.8.4.2.9` × 1 — first: fz05489 (clause 8.8.4.2.9 Conditional expressions > Simple relation conditions > Comparison of national operands)
- programs: fz05489
- source: `programs/fz05489.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - line 5: `STD-CMP A,B: <` | `<end>`
  - line 6: `LOC-CMP A,B: <` | `<end>`
  - line 7: `ALPHA=NAT: TRUE` | `<end>`
  - GnuCOBOL run exit 1: ``

### `THEY_REJECT:13.18.27` × 1 — first: fz05326 (clause 13.18.27 GLOBAL clause)
- programs: fz05326
- source: `programs/fz05326.cob`
- GnuCOBOL: `fz05326.cob:10: error: executable program requested but PROCEDURE/ENTRY has USING clause`

### `THEY_REJECT:13.18.38` × 1 — first: fz02925 (clause 13.18.38 OCCURS clause)
- programs: fz02925
- source: `programs/fz02925.cob`
- GnuCOBOL: `fz02925.cob:28: error: subscript of 'WS-ELEM-DYN' out of bounds: 1`

### `THEY_REJECT:13.18.40` × 1 — first: fz01196 (clause 13.18.40 PICTURE clause)
- programs: fz01196
- source: `programs/fz01196.cob`
- GnuCOBOL: `fz01196.cob:5: error: numeric literal in VALUE clause of numeric-edited item used`

### `THEY_REJECT:14.9.18` × 1 — first: fz00749 (clause 14.9.18 GOBACK statement)
- programs: fz00749
- source: `programs/fz00749.cob`
- GnuCOBOL: `unknown (signal)`

### `THEY_REJECT:14.9.20` × 1 — first: fz03147 (clause 14.9.20 INITIALIZE statement)
- programs: fz03147
- source: `programs/fz03147.cob`
- GnuCOBOL: `fz03147.cob:35: error: MOVE of figurative constant SPACE to numeric item used`

### `THEY_REJECT:14.9.22` × 1 — first: fz03282 (clause 14.9.22 INSPECT statement)
- programs: fz03282
- source: `programs/fz03282.cob`
- GnuCOBOL: `fz03282.cob:19: error: MOVE of figurative constant SPACE to numeric item used`

### `THEY_REJECT:14.9.28` × 1 — first: fz02575 (clause 14.9.28 PERFORM statement)
- programs: fz02575
- source: `programs/fz02575.cob`
- GnuCOBOL: `fz02575.cob:25: error: MOVE of figurative constant SPACE to numeric item used`

### `THEY_REJECT:14.9.30` × 1 — first: fz02163 (clause 14.9.30 READ statement)
- programs: fz02163
- source: `programs/fz02163.cob`
- GnuCOBOL: `fz02163.cob:80: error: READ PREVIOUS not allowed for this file type`

### `THEY_REJECT:14.9.41` × 1 — first: fz02543 (clause 14.9.41 START statement)
- programs: fz02543
- source: `programs/fz02543.cob`
- GnuCOBOL: `fz02543.cob:36: error: START not allowed on SEQUENTIAL files`

### `THEY_REJECT:14.9.5` × 1 — first: fz04751 (clause 14.9.5 CANCEL statement)
- programs: fz04751
- source: `programs/fz04751.cob`
- GnuCOBOL: `fz04751.cob:10: error: 'ENTRY' is not defined, but is a reserved word in another dialect`

### `THEY_REJECT:15.31` × 1 — first: fz04193 (clause 15.31 EXCEPTION-LOCATION-N function)
- programs: fz04193
- source: `programs/fz04193.cob`
- GnuCOBOL: `fz04193.cob:15: error: syntax error, unexpected FUNCTION DISPLAY-OF`

### `THEY_REJECT:15.32` × 1 — first: fz04110 (clause 15.32 EXCEPTION-STATEMENT function)
- programs: fz04110
- source: `programs/fz04110.cob`
- GnuCOBOL: `fz04110.cob: in paragraph 'PARSE-ERROR-PARA':`

### `THEY_REJECT:15.35` × 1 — first: fz01453 (clause 15.35 EXP10 function)
- programs: fz01453
- source: `programs/fz01453.cob`
- GnuCOBOL: `fz01453.cob:24: error: 'WS-RESULT' is not numeric`

### `THEY_REJECT:15.47` × 1 — first: fz00030 (clause 15.47 INTEGER-OF-DAY function)
- programs: fz00030
- source: `programs/fz00030.cob`
- GnuCOBOL: `fz00030.cob:6: error: numeric literal in VALUE clause of numeric-edited item used`

### `THEY_REJECT:15.7` × 1 — first: fz00655 (clause 15.7 ABS function)
- programs: fz00655
- source: `programs/fz00655.cob`
- GnuCOBOL: `fz00655.cob:7: error: syntax error, unexpected Identifier`

### `THEY_REJECT:15.96` × 1 — first: fz03064 (clause 15.96 TRIM function)
- programs: fz03064
- source: `programs/fz03064.cob`
- GnuCOBOL: `fz03064.cob:18: error: syntax error, unexpected FUNCTION DISPLAY-OF`

### `THEY_REJECT:8.8.1.3` × 1 — first: fz01603 (clause 8.8.1.3 Arithmetic expressions > Native arithmetic)
- programs: fz01603
- source: `programs/fz01603.cob`
- GnuCOBOL: `fz01603.cob:15: error: MOVE of figurative constant SPACE to numeric item used`

### `THEY_REJECT:8.8.1.5.3` × 1 — first: fz03243 (clause 8.8.1.5.3 Arithmetic expressions > Standard-decimal arithmetic > Basic arithmetic operations in standard-decimal arithmetic)
- programs: fz03243
- source: `programs/fz03243.cob`
- GnuCOBOL: `fz03243.cob:18: error: syntax error, unexpected Identifier`

### `THEY_REJECT:8.8.3` × 1 — first: fz05297 (clause 8.8.3 Concatenation expressions)
- programs: fz05297
- source: `programs/fz05297.cob`
- GnuCOBOL: `fz05297.cob:29: error: syntax error, unexpected ALL`

### `THEY_REJECT:8.8.4.2.14` × 1 — first: fz00594 (clause 8.8.4.2.14 Conditional expressions > Simple relation conditions > Comparisons of operands of class message-tag)
- programs: fz00594
- source: `programs/fz00594.cob`
- GnuCOBOL: `fz00594.cob:7: error: PICTURE clause required for 'WS-MSG1'`

### `THEY_REJECT:8.8.4.5` × 1 — first: fz00709 (clause 8.8.4.5 Conditional expressions > Simple condition-name condition (conditional variable))
- programs: fz00709
- source: `programs/fz00709.cob`
- GnuCOBOL: `fz00709.cob:7: error: PICTURE clause required for 'WS-COND-NAME'`

### `WE_FAIL_RUN:15.32` × 1 — first: fz03337 (clause 15.32 EXCEPTION-STATEMENT function)
- programs: fz03337
- source: `programs/fz03337.cob`
- outputs (WiseOwl | GnuCOBOL), first differing lines:
  - WiseOwl run exit 1

### `WE_HANG:14.9.17` × 1 — first: fz03508 (clause 14.9.17 GO TO statement)
- programs: fz03508
- source: `programs/fz03508.cob`

### `WE_HANG:14.9.2` × 1 — first: fz03157 (clause 14.9.2 ADD statement)
- programs: fz03157
- source: `programs/fz03157.cob`

### `WE_HANG:15.15` × 1 — first: fz00122 (clause 15.15 CHAR function)
- programs: fz00122
- source: `programs/fz00122.cob`

### `WE_HANG:15.3.1.2` × 1 — first: fz02262 (clause 15.3.1.2 Format arguments to international date and time functions > Calendar date formats)
- programs: fz02262
- source: `programs/fz02262.cob`
- runner: the compiler exceeded the hang guard (90 s)

### `WE_REJECT:COBOL0001,COBOLNET2631` × 1 — first: fz01087 (clause 15.25 DAY-TO-YYYYDDD function)
- programs: fz01087
- source: `programs/fz01087.cob`
- WiseOwl: `fz01087.cob(41,57): error COBOLNET2631: the comma is not immediately followed by a space, so it is not a separator — ISO §8.3.5 rule 2 makes a comma or semicolon a separator only when a space follows it; write a space after it`

### `WE_REJECT:COBOLNET0801,COBOLNET0805` × 1 — first: fz03696 (clause 15.73 PI function)
- programs: fz03696
- source: `programs/fz03696.cob`
- WiseOwl: `fz03696.cob(5,1): error COBOLNET0801: data item 'WS-PI' (PICTURE 9(5)V9(28)) has 33 digit positions; ISO/IEC 1989 limits fixed-point items and literals to 31 digits (ISO §8.3.3.3.2)`

### `WE_REJECT:COBOLNET0801,COBOLNET0819` × 1 — first: fz01755 (clause 15.84 SQRT function)
- programs: fz01755
- source: `programs/fz01755.cob`
- WiseOwl: `fz01755.cob(8,1): error COBOLNET0801: data item 'WS-RES-DISP' (PICTURE Z(7)9.999999999999999999999999999999999) has 41 digit positions; ISO/IEC 1989 limits fixed-point items and literals to 31 digits (ISO §8.3.3.3.2)`

### `WE_REJECT:COBOLNET0801,COBOLNET0898,COBOLNET1627,COBOLNET1657` × 1 — first: fz03143 (clause 15.67 NUMVAL function)
- programs: fz03143
- source: `programs/fz03143.cob`
- WiseOwl: `fz03143.cob(7,1): error COBOLNET0801: data item 'WS-DIGITS' (PICTURE 9(35)) has 35 digit positions; ISO/IEC 1989 limits fixed-point items and literals to 31 digits (ISO §8.3.3.3.2)`

### `WE_REJECT:COBOLNET0801,COBOLNET1627` × 1 — first: fz04471 (clause 15.73 PI function)
- programs: fz04471
- source: `programs/fz04471.cob`
- WiseOwl: `fz04471.cob(10,1): error COBOLNET0801: data item 'WS-AREA' (PICTURE S9(7)V9(25)) has 32 digit positions; ISO/IEC 1989 limits fixed-point items and literals to 31 digits (ISO §8.3.3.3.2)`

### `WE_REJECT:COBOLNET0801,COBOLNET1627,COBOLNET1657` × 1 — first: fz01036 (clause 8.8.1.5.2 Arithmetic expressions > Standard-decimal arithmetic > Standard-decimal intermediate data item)
- programs: fz01036
- source: `programs/fz01036.cob`
- WiseOwl: `fz01036.cob(5,1): error COBOLNET0801: data item 'WS-RESULT' (PICTURE S9(31)V99) has 33 digit positions; ISO/IEC 1989 limits fixed-point items and literals to 31 digits (ISO §8.3.3.3.2)`

### `WE_REJECT:COBOLNET0808` × 1 — first: fz05023 (clause 14.9.43 STRING statement)
- programs: fz05023
- source: `programs/fz05023.cob`
- WiseOwl: `fz05023.cob(13,1): error COBOLNET0808: invalid PICTURE symbol 'U' in PICTURE U(10) — data item 'WS-NDEST' (ISO §13.18.40.3 SR2: not an allowable picture symbol)`

### `WE_REJECT:COBOLNET0808,COBOLNET0819` × 1 — first: fz01807 (clause 15.59 MAX function)
- programs: fz01807
- source: `programs/fz01807.cob`
- WiseOwl: `fz01807.cob(16,1): error COBOLNET0808: invalid PICTURE symbol 'U' in PICTURE U(3) — data item 'WS-NATIONAL-A' (ISO §13.18.40.3 SR2: not an allowable picture symbol)`

### `WE_REJECT:COBOLNET0808,COBOLNET2168` × 1 — first: fz00660 (clause 8.8.4.2 Conditional expressions > Simple relation conditions)
- programs: fz00660
- source: `programs/fz00660.cob`
- WiseOwl: `fz00660.cob(10,1): error COBOLNET0808: invalid PICTURE symbol 'U' in PICTURE U(3) — data item 'WS-NAT-A' (ISO §13.18.40.3 SR2: not an allowable picture symbol)`

### `WE_REJECT:COBOLNET0809,COBOLNET0844,COBOLNET0881,COBOLNET1675,COBOLNET1740,COBOLNET2168` × 1 — first: fz02581 (clause 15.59 MAX function)
- programs: fz02581
- source: `programs/fz02581.cob`
- WiseOwl: `fz02581.cob(11,4): error COBOLNET1740: data item 'WS-ALPHA-2': the VALUE alphanumeric literal (6 characters) exceeds the item's 5 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET0809,COBOLNET0844,COBOLNET0898` × 1 — first: fz03757 (clause 8.8.4.2 Conditional expressions > Simple relation conditions)
- programs: fz03757
- source: `programs/fz03757.cob`
- WiseOwl: `fz03757.cob(16,1): error COBOLNET0898: data item 'WS-BOOL-A': the VALUE of a boolean data item shall be a boolean literal (B"…") or the figurative constant ZERO (ISO §13.18.63.3 SR10)`

### `WE_REJECT:COBOLNET0809,COBOLNET0844,COBOLNET1675,COBOLNET2326` × 1 — first: fz01806 (clause 15.59 MAX function)
- programs: fz01806
- source: `programs/fz01806.cob`
- WiseOwl: `fz01806.cob(68,4): error COBOLNET2326: SET 'WS-I1' TO '10' violates ISO §14.9.39.3 SR3: "If identifier-1 references a data item of class index, arithmetic-expression-1 shall not be specified"`

### `WE_REJECT:COBOLNET0809,COBOLNET0881,COBOLNET1694,COBOLNET2326` × 1 — first: fz03356 (clause 15.59 MAX function)
- programs: fz03356
- source: `programs/fz03356.cob`
- WiseOwl: `fz03356.cob(11,1): error COBOLNET0881: data item 'WS-NAT-1': USAGE NATIONAL may not be specified with an alphabetic or alphanumeric PICTURE (X(4)) — it admits boolean, national, national-edited, numeric, and numeric-edited pictures only (ISO §13.18.60.3 SR12; §13.18.40.3 SR30)`

### `WE_REJECT:COBOLNET0809,COBOLNET0898` × 1 — first: fz02984 (clause 8.8.4.2 Conditional expressions > Simple relation conditions)
- programs: fz02984
- source: `programs/fz02984.cob`
- WiseOwl: `fz02984.cob(14,1): error COBOLNET0898: data item 'WS-NAT-A': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET0809,COBOLNET0898,COBOLNET1694` × 1 — first: fz04903 (clause 15.59 MAX function)
- programs: fz04903
- source: `programs/fz04903.cob`
- WiseOwl: `fz04903.cob(11,1): error COBOLNET0898: data item 'WS-NAT-1': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET0809,COBOLNET0898,COBOLNET1694,COBOLNET1740,COBOLNET2168,COBOLNET2533` × 1 — first: fz02580 (clause 15.59 MAX function)
- programs: fz02580
- source: `programs/fz02580.cob`
- WiseOwl: `fz02580.cob(9,1): error COBOLNET1740: data item 'WS-ALPHA2': the VALUE alphanumeric literal (6 characters) exceeds the item's 5 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET0809,COBOLNET0898,COBOLNET1694,COBOLNET2168,COBOLNET2326` × 1 — first: fz00335 (clause 15.63 MIN function)
- programs: fz00335
- source: `programs/fz00335.cob`
- WiseOwl: `fz00335.cob(12,1): error COBOLNET0898: data item 'WS-NAT-1': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET0809,COBOLNET0898,COBOLNET1740,COBOLNET2168,COBOLNET2533` × 1 — first: fz01108 (clause 15.63 MIN function)
- programs: fz01108
- source: `programs/fz01108.cob`
- WiseOwl: `fz01108.cob(11,1): error COBOLNET1740: data item 'WS-STR2': the VALUE alphanumeric literal (6 characters) exceeds the item's 5 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET0809,COBOLNET0898,COBOLNET2168` × 1 — first: fz02657 (clause 15.63 MIN function)
- programs: fz02657
- source: `programs/fz02657.cob`
- WiseOwl: `fz02657.cob(14,1): error COBOLNET0898: data item 'WS-NAT-A': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET0809,COBOLNET1675,COBOLNET2168,COBOLNET2533` × 1 — first: fz01351 (clause 8.8.4.2.13 Conditional expressions > Simple relation conditions > Comparisons involving index-names or index data items)
- programs: fz01351
- source: `programs/fz01351.cob`
- WiseOwl: `fz01351.cob(7,1): error COBOLNET2168: data item 'WS-IDX1': the VALUE clause shall not be specified with USAGE INDEX (a data item of class index — ISO §13.16.3 SR10)`

### `WE_REJECT:COBOLNET0809,COBOLNET1675,COBOLNET2326` × 1 — first: fz03355 (clause 15.59 MAX function)
- programs: fz03355
- source: `programs/fz03355.cob`
- WiseOwl: `fz03355.cob(51,4): error COBOLNET2326: SET 'WS-I1' TO '100' violates ISO §14.9.39.3 SR3: "If identifier-1 references a data item of class index, arithmetic-expression-1 shall not be specified"`

### `WE_REJECT:COBOLNET0809,COBOLNET1694` × 1 — first: fz01881 (clause 15.63 MIN function)
- programs: fz01881
- source: `programs/fz01881.cob`
- WiseOwl: `fz01881.cob(40,1): error COBOLNET0809: a MOVE operand shall not be of class index (ISO §14.9.25.3 SR1; §13.18.60.3 SR10 — only a SEARCH or SET statement, a relation condition, an intrinsic-function or inline-method argument, or a procedure-division / CALL / INVOKE USING phrase may reference an index`

### `WE_REJECT:COBOLNET0809,COBOLNET2168,COBOLNET2326,COBOLNET2533` × 1 — first: fz03674 (clause 8.8.4.2.13 Conditional expressions > Simple relation conditions > Comparisons involving index-names or index data items)
- programs: fz03674
- source: `programs/fz03674.cob`
- WiseOwl: `fz03674.cob(7,1): error COBOLNET2168: data item 'WS-IDX1': the VALUE clause shall not be specified with USAGE INDEX (a data item of class index — ISO §13.16.3 SR10)`

### `WE_REJECT:COBOLNET0809,COBOLNET2326` × 1 — first: fz00259 (clause 15.59 MAX function)
- programs: fz00259
- source: `programs/fz00259.cob`
- WiseOwl: `fz00259.cob(58,4): error COBOLNET2326: SET 'WS-I1' TO '10' violates ISO §14.9.39.3 SR3: "If identifier-1 references a data item of class index, arithmetic-expression-1 shall not be specified"`

### `WE_REJECT:COBOLNET0809,COBOLNET2576` × 1 — first: fz01368 (clause 8.8.4.2.14 Conditional expressions > Simple relation conditions > Comparisons of operands of class message-tag)
- programs: fz01368
- source: `programs/fz01368.cob`
- WiseOwl: `fz01368.cob(11,1): error COBOLNET2576: NULL is the predefined address / object reference — an identifier (ISO §8.4.3.1.2 Format 8; §8.4.3.7), not a literal or a figurative constant — and ISO §8.4.3.10.3 SR1 admits it only as a sending operand in an INITIALIZE or a SET statement, as an argument in a `

### `WE_REJECT:COBOLNET0813` × 1 — first: fz03490 (clause 13.18.58 TYPEDEF clause)
- programs: fz03490
- source: `programs/fz03490.cob`
- WiseOwl: `fz03490.cob(7,1): error COBOLNET0813: data item 'WS-INSTANCE-1': USAGE OBJECT REFERENCE 'WS-TYPE-DEF': no class or interface of that name is defined in this compilation group (ISO §13.18.60.2/.4)`

### `WE_REJECT:COBOLNET0817,COBOLNET0881` × 1 — first: fz00428 (clause 13.18.47 REQUIRED clause)
- programs: fz00428
- source: `programs/fz00428.cob`
- WiseOwl: `fz00428.cob(6,1): error COBOLNET0881: data item 'WS-NATIONAL': USAGE NATIONAL may not be specified with an alphabetic or alphanumeric PICTURE (X(5)) — it admits boolean, national, national-edited, numeric, and numeric-edited pictures only (ISO §13.18.60.3 SR12; §13.18.40.3 SR30)`

### `WE_REJECT:COBOLNET0817,COBOLNET1627` × 1 — first: fz03742 (clause 13.18.25 FROM clause)
- programs: fz03742
- source: `programs/fz03742.cob`
- WiseOwl: `fz03742.cob(14,4): error COBOLNET0817: ACCEPT FROM 'SYSIN': not a mnemonic-name declared in SPECIAL-NAMES (ISO §14.9.1.3 SR2 — mnemonic-name-1 shall be associated with an implementor device-name, §12.3.7.2 'device-name-1 IS mnemonic-name-3')`

### `WE_REJECT:COBOLNET0819,COBOLNET0844,COBOLNET0881,COBOLNET2168,COBOLNET2326` × 1 — first: fz00516 (clause 15.2 Types of functions)
- programs: fz00516
- source: `programs/fz00516.cob`
- WiseOwl: `fz00516.cob(8,1): error COBOLNET2168: data item 'WS-IDX': the VALUE clause shall not be specified with USAGE INDEX (a data item of class index — ISO §13.16.3 SR10)`

### `WE_REJECT:COBOLNET0819,COBOLNET0844,COBOLNET0898` × 1 — first: fz01765 (clause 14.9.8 COMPUTE statement)
- programs: fz01765
- source: `programs/fz01765.cob`
- WiseOwl: `fz01765.cob(14,1): error COBOLNET0898: data item 'WS-FLAG': the VALUE of a boolean data item shall be a boolean literal (B"…") or the figurative constant ZERO (ISO §13.18.63.3 SR10)`

### `WE_REJECT:COBOLNET0819,COBOLNET0844,COBOLNET0900` × 1 — first: fz01165 (clause 13.18.24 FORMAT clause)
- programs: fz01165
- source: `programs/fz01165.cob`
- WiseOwl: `fz01165.cob(34,5): error COBOLNET0819: MOVE … TO WS-FMT-BIT: MOVE is invalid — an alphabetic, alphanumeric-edited, numeric or numeric-edited sending operand does not move to a boolean receiver (ISO §14.9.25.3 SR10, Table 16)`

### `WE_REJECT:COBOLNET0819,COBOLNET0844,COBOLNET1627` × 1 — first: fz03655 (clause 13.18.1 ALIGNED clause)
- programs: fz03655
- source: `programs/fz03655.cob`
- WiseOwl: `fz03655.cob(13,1): error COBOLNET0819: MOVE … TO WS-ALIGNED-BIT: a figurative constant whose characters are not boolean characters shall not be moved to a boolean data item (ISO §14.9.25.3 SR7)`

### `WE_REJECT:COBOLNET0819,COBOLNET0898,COBOLNET1517` × 1 — first: fz00185 (clause 15.40 FORMATTED-DATETIME function)
- programs: fz00185
- source: `programs/fz00185.cob`
- WiseOwl: `fz00185.cob(11,1): error COBOLNET0898: data item 'WS-NAT-FMT': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET0819,COBOLNET0900` × 1 — first: fz03487 (clause 13.18.24 FORMAT clause)
- programs: fz03487
- source: `programs/fz03487.cob`
- WiseOwl: `fz03487.cob(28,5): error COBOLNET0819: MOVE … TO WS-FORMATTED-REC: MOVE is invalid — a noninteger numeric sending operand does not move to an alphabetic, alphanumeric or alphanumeric-edited receiver (ISO §14.9.25.3 SR10, Table 16)`

### `WE_REJECT:COBOLNET0819,COBOLNET1517,COBOLNET1627` × 1 — first: fz05367 (clause 15.3.3.7 Common time formats > Combined date and time formats)
- programs: fz05367
- source: `programs/fz05367.cob`
- WiseOwl: `fz05367.cob(19,1): error COBOLNET0819: MOVE … TO WS-COMBINED-BASIC: MOVE is invalid — a noninteger numeric sending operand does not move to an alphabetic, alphanumeric or alphanumeric-edited receiver (ISO §14.9.25.3 SR10, Table 16) (a NUMERIC-typed function is the Noninteger sender, §15.2 item 4 / §`

### `WE_REJECT:COBOLNET0819,COBOLNET1627` × 1 — first: fz05161 (clause 15.2 Types of functions)
- programs: fz05161
- source: `programs/fz05161.cob`
- WiseOwl: `fz05161.cob(28,1): error COBOLNET1627: FUNCTION CHAR argument-1 is of class alphanumeric; ISO §15.3 requires class numeric (§15.15.3 r1). --permissive accepts it as a coercion extension`

### `WE_REJECT:COBOLNET0819,COBOLNET2326` × 1 — first: fz00322 (clause 15.31 EXCEPTION-LOCATION-N function)
- programs: fz00322
- source: `programs/fz00322.cob`
- WiseOwl: `fz00322.cob(25,1): error COBOLNET0819: MOVE … TO WS-RESULT-N: MOVE is invalid — a national sending operand does not move to an alphabetic, alphanumeric or alphanumeric-edited receiver (ISO §14.9.25.3 SR10, Table 16; FUNCTION DISPLAY-OF is the sanctioned conversion)`

### `WE_REJECT:COBOLNET0844,COBOLNET0898,COBOLNET1627` × 1 — first: fz03043 (clause 15.3 Arguments)
- programs: fz03043
- source: `programs/fz03043.cob`
- WiseOwl: `fz03043.cob(12,1): error COBOLNET0898: data item 'WS-NAT': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET0844,COBOLNET0898,COBOLNET1675,COBOLNET1694,COBOLNET1740,COBOLNET2168` × 1 — first: fz01808 (clause 15.59 MAX function)
- programs: fz01808
- source: `programs/fz01808.cob`
- WiseOwl: `fz01808.cob(10,1): error COBOLNET1740: data item 'WS-ALPHA-2': the VALUE alphanumeric literal (6 characters) exceeds the item's 5 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET0863,COBOLNET0864,COBOLNET0900` × 1 — first: fz03713 (clause 14.9.30 READ statement)
- programs: fz03713
- source: `programs/fz03713.cob`
- WiseOwl: `fz03713.cob(10,23): error COBOLNET0863: RECORD KEY 'RND-KEY' is a category-numeric item; data-name-1 shall reference a data item of category alphanumeric or category national (ISO §12.4.5.12.3 SR2)`

### `WE_REJECT:COBOLNET0863,COBOLNET0864,COBOLNET0900,COBOLNET1994` × 1 — first: fz05261 (clause 14.9.30 READ statement)
- programs: fz05261
- source: `programs/fz05261.cob`
- WiseOwl: `fz05261.cob(10,23): error COBOLNET0863: RECORD KEY 'RND-KEY' is a category-numeric item; data-name-1 shall reference a data item of category alphanumeric or category national (ISO §12.4.5.12.3 SR2)`

### `WE_REJECT:COBOLNET0865,COBOLNET0900` × 1 — first: fz02725 (clause 14.9.10 DELETE statement)
- programs: fz02725
- source: `programs/fz02725.cob`
- WiseOwl: `fz02725.cob(21,1): error COBOLNET0865: DELETE RECORD shall not be specified for sequential-organization file 'INPUT-FILE' (ISO §14.9.10.3 SR1)`

### `WE_REJECT:COBOLNET0869` × 1 — first: fz02893 (clause 14.9.3 ALLOCATE statement)
- programs: fz02893
- source: `programs/fz02893.cob`
- WiseOwl: `fz02893.cob(26,1): error COBOLNET0869: 'WS-BASED-ITEM': a FREE operand (ISO §14.9.15.3 SR1 — data-pointers only) shall be a USAGE POINTER data item`

### `WE_REJECT:COBOLNET0881,COBOLNET1627` × 1 — first: fz02393 (clause 8.8.4.2.9 Conditional expressions > Simple relation conditions > Comparison of national operands)
- programs: fz02393
- source: `programs/fz02393.cob`
- WiseOwl: `fz02393.cob(5,1): error COBOLNET0881: data item 'WS-NATIONAL-A': USAGE NATIONAL may not be specified with an alphabetic or alphanumeric PICTURE (X(4)) — it admits boolean, national, national-edited, numeric, and numeric-edited pictures only (ISO §13.18.60.3 SR12; §13.18.40.3 SR30)`

### `WE_REJECT:COBOLNET0881,COBOLNET1627,COBOLNET1740` × 1 — first: fz00517 (clause 15.2 Types of functions)
- programs: fz00517
- source: `programs/fz00517.cob`
- WiseOwl: `fz00517.cob(7,1): error COBOLNET1740: data item 'WS-TEXT': the VALUE alphanumeric literal (11 characters) exceeds the item's 10 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET0881,COBOLNET1657,COBOLNET1740` × 1 — first: fz04651 (clause 8.8.4.2.5 Conditional expressions > Simple relation conditions > Comparison of a numeric integer operand with an operand of class alphanumeric or national)
- programs: fz04651
- source: `programs/fz04651.cob`
- WiseOwl: `fz04651.cob(6,1): error COBOLNET1657: data item 'WS-INT-NAT': the VALUE literal "12345" is alphanumeric but the item is numeric — all literals in the VALUE clause of a numeric item shall be numeric (ISO §13.18.63.3 SR2); under --permissive it is stored as the number 12345`

### `WE_REJECT:COBOLNET0881,COBOLNET2326` × 1 — first: fz04532 (clause 8.8.4.2 Conditional expressions > Simple relation conditions)
- programs: fz04532
- source: `programs/fz04532.cob`
- WiseOwl: `fz04532.cob(13,1): error COBOLNET0881: data item 'WS-NAT-A': USAGE NATIONAL may not be specified with an alphabetic or alphanumeric PICTURE (X(5)) — it admits boolean, national, national-edited, numeric, and numeric-edited pictures only (ISO §13.18.60.3 SR12; §13.18.40.3 SR30)`

### `WE_REJECT:COBOLNET0898,COBOLNET0900` × 1 — first: fz05034 (clause 13.18.24 FORMAT clause)
- programs: fz05034
- source: `programs/fz05034.cob`
- WiseOwl: `fz05034.cob(17,1): error COBOLNET0898: data item 'WS-BIT-DATA': the VALUE of a boolean data item shall be a boolean literal (B"…") or the figurative constant ZERO (ISO §13.18.63.3 SR10)`

### `WE_REJECT:COBOLNET0898,COBOLNET1637` × 1 — first: fz03799 (clause 13.18.63 VALUE clause)
- programs: fz03799
- source: `programs/fz03799.cob`
- WiseOwl: `fz03799.cob(10,1): error COBOLNET0898: data item 'WS-NATIONAL': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET0898,COBOLNET2168` × 1 — first: fz02209 (clause 8.8.4.2 Conditional expressions > Simple relation conditions)
- programs: fz02209
- source: `programs/fz02209.cob`
- WiseOwl: `fz02209.cob(9,1): error COBOLNET0898: data item 'WS-NAT-A': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET0898,COBOLNET2796` × 1 — first: fz01882 (clause 15.63 MIN function)
- programs: fz01882
- source: `programs/fz01882.cob`
- WiseOwl: `fz01882.cob(27,1): error COBOLNET2796: PERFORM … PERFORM: this IF statement is written without its explicit scope terminator END-IF, so it is a conditional statement, and the operand of this phrase is an imperative-statement — ISO §14.5.1: "Any statement with a conditional phrase that is not termina`

### `WE_REJECT:COBOLNET0898,COBOLNET3407` × 1 — first: fz01341 (clause 13.18.13 CODE-SET clause)
- programs: fz01341
- source: `programs/fz01341.cob`
- WiseOwl: `fz01341.cob(11,1): error COBOLNET0898: data item 'WS-NATIONAL': the VALUE of a national data item shall be a national literal (N"…") or a figurative constant (ISO §13.18.63.3 SR5)`

### `WE_REJECT:COBOLNET0899,COBOLNET0900,COBOLNET1639,COBOLNET2578` × 1 — first: fz05537 (clause 14.9.46 TERMINATE statement)
- programs: fz05537
- source: `programs/fz05537.cob`
- WiseOwl: `fz05537.cob(11,1): error COBOLNET2578: file description entry 'REPORT-FILE' specifies a REPORT clause and has 1 record description or constant entry subordinate to it — ISO §13.4.5.3 SR8: "No record description entries or constant entries shall be associated with the file description entry for a rep`

### `WE_REJECT:COBOLNET0900,COBOLNET1625` × 1 — first: fz00693 (clause 13.18.39 PAGE clause)
- programs: fz00693
- source: `programs/fz00693.cob`
- WiseOwl: `fz00693.cob(21,1): error COBOLNET1625: data item 'WS-INVALID-PAGE': the numeric literal 10000 in the VALUE clause is not a permissible value in the range the PICTURE indicates — it is not representable without truncation of leading or trailing nonzero digits (ISO §13.18.63.3 SR2)`

### `WE_REJECT:COBOLNET0900,COBOLNET1651` × 1 — first: fz01300 (clause 13.18.28 GROUP INDICATE clause)
- programs: fz01300
- source: `programs/fz01300.cob`
- WiseOwl: `fz01300.cob(42,5): error COBOLNET1651: STRING sending operand 'WS-DETAIL-AMT' is an elementary numeric item with digit positions to the right of the assumed decimal point; SR8 requires an integer (ISO §14.9.43.3 SR8)`

### `WE_REJECT:COBOLNET0900,COBOLNET1651,COBOLNET1757` × 1 — first: fz00116 (clause 14.9.34 RETURN statement)
- programs: fz00116
- source: `programs/fz00116.cob`
- WiseOwl: `fz00116.cob(25,9): error COBOLNET1651: STRING INTO 'WS-RECORD-A(8:5)': identifier-3 shall not be reference-modified (ISO §14.9.43.3 SR4)`

### `WE_REJECT:COBOLNET0900,COBOLNET1657,COBOLNET1740` × 1 — first: fz02476 (clause 13.18.14 COLUMN clause)
- programs: fz02476
- source: `programs/fz02476.cob`
- WiseOwl: `fz02476.cob(15,9): error COBOLNET1657: condition-name 'LEFT-POS': the VALUE literal 1 is numeric but the item is alphabetic / alphanumeric / alphanumeric-edited — its VALUE literals shall be alphanumeric (ISO §13.18.63.3 SR4); under --permissive the literal's characters are stored as MOVE would stor`

### `WE_REJECT:COBOLNET0900,COBOLNET1740,COBOLNET1757` × 1 — first: fz05543 (clause 14.9.32 RELEASE statement)
- programs: fz05543
- source: `programs/fz05543.cob`
- WiseOwl: `fz05543.cob(14,5): error COBOLNET1740: data item 'WS-REC-A': the VALUE alphanumeric literal (11 characters) exceeds the item's 10 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET0900,COBOLNET1740,COBOLNET2796` × 1 — first: fz03227 (clause 13.18.51 SELECT WHEN clause)
- programs: fz03227
- source: `programs/fz03227.cob`
- WiseOwl: `fz03227.cob(53,13): error COBOLNET2796: READ … NOT AT END: this IF statement is written without its explicit scope terminator END-IF, so it is a conditional statement, and the operand of this phrase is an imperative-statement — ISO §14.5.1: "Any statement with a conditional phrase that is not termin`

### `WE_REJECT:COBOLNET0900,COBOLNET2168` × 1 — first: fz00718 (clause 14.9.45 SUPPRESS statement)
- programs: fz00718
- source: `programs/fz00718.cob`
- WiseOwl: `fz00718.cob(13,1): error COBOLNET2168: data item 'WS-GROUP-A': the VALUE clause shall not be specified with USAGE INDEX (a data item of class index — ISO §13.16.3 SR10)`

### `WE_REJECT:COBOLNET0900,COBOLNET2326,COBOLNET2796` × 1 — first: fz03514 (clause 14.9.6 CLOSE statement)
- programs: fz03514
- source: `programs/fz03514.cob`
- WiseOwl: `fz03514.cob(40,5): error COBOLNET2796: READ … AT END: this READ statement is written without its explicit scope terminator END-READ, so it is a conditional statement, and the operand of this phrase is an imperative-statement — ISO §14.5.1: "Any statement with a conditional phrase that is not termina`

### `WE_REJECT:COBOLNET0901` × 1 — first: fz02715 (clause 13.18.58 TYPEDEF clause)
- programs: fz02715
- source: `programs/fz02715.cob`
- WiseOwl: `fz02715.cob(17,6): error COBOLNET0901: 'ADDRESS' is a reserved word in COBOL-2014 and cannot be used as a user-defined word (ISO §8.9)`

### `WE_REJECT:COBOLNET1517,COBOLNET1627,COBOLNET1657` × 1 — first: fz00551 (clause 15.3.3.6 Common time formats > Offset time formats)
- programs: fz00551
- source: `programs/fz00551.cob`
- WiseOwl: `fz00551.cob(6,1): error COBOLNET1657: data item 'WS-TIME-HHMMSS': the VALUE literal "143045" is alphanumeric but the item is numeric — all literals in the VALUE clause of a numeric item shall be numeric (ISO §13.18.63.3 SR2); under --permissive it is stored as the number 143045`

### `WE_REJECT:COBOLNET1517,COBOLNET1627,COBOLNET2796` × 1 — first: fz01040 (clause 15.3.3.3 Common time formats > Permissible values for data associated with common time formats)
- programs: fz01040
- source: `programs/fz01040.cob`
- WiseOwl: `fz01040.cob(15,1): error COBOLNET2796: PERFORM … PERFORM: this IF statement is written without its explicit scope terminator END-IF, so it is a conditional statement, and the operand of this phrase is an imperative-statement — ISO §14.5.1: "Any statement with a conditional phrase that is not termina`

### `WE_REJECT:COBOLNET1523,COBOLNET1961` × 1 — first: fz02927 (clause 13.18.38 OCCURS clause)
- programs: fz02927
- source: `programs/fz02927.cob`
- WiseOwl: `fz02927.cob(14,4): error COBOLNET1523: CAPACITY IN 'WS-CAP-PIC' on 'WS-DYN-ITEM': data-name-3 is implicitly defined by the OCCURS DYNAMIC entry and shall not duplicate another data-name or CAPACITY register (ISO §13.18.38.3 SR30)`

### `WE_REJECT:COBOLNET1580,COBOLNET1627` × 1 — first: fz02625 (clause 13.18.31 INVALID clause)
- programs: fz02625
- source: `programs/fz02625.cob`
- WiseOwl: `fz02625.cob(11,1): error COBOLNET1627: FUNCTION NUMVAL argument-1 is of class numeric; ISO §15.3 requires class alphanumeric, alphanumeric (numeric-edited), national (numeric-edited) or national (§15.67.3 r1). --permissive accepts it as a coercion extension`

### `WE_REJECT:COBOLNET1625,COBOLNET2406` × 1 — first: fz05113 (clause 13.18.33 Level-number)
- programs: fz05113
- source: `programs/fz05113.cob`
- WiseOwl: `fz05113.cob(9,4): error COBOLNET2406: data item 'WS-BLOB': the VALUE clause shall not be specified in an entry subordinate to 'WS-RECORD-B', which contains a REDEFINES clause (ISO §13.18.63.3 SR12) — the redefined entry 'WS-RECORD-A' is the one that may carry an initial value`

### `WE_REJECT:COBOLNET1625,COBOLNET2532` × 1 — first: fz02261 (clause 8.8.4.2.4 Conditional expressions > Simple relation conditions > Comparison of numeric operands)
- programs: fz02261
- source: `programs/fz02261.cob`
- WiseOwl: `fz02261.cob(8,1): error COBOLNET1625: data item 'WS-SMALL': the numeric literal 0.00001 in the VALUE clause is not a permissible value in the range the PICTURE indicates — it is not representable without truncation of leading or trailing nonzero digits (ISO §13.18.63.3 SR2)`

### `WE_REJECT:COBOLNET1625,COBOLNET2796` × 1 — first: fz04018 (clause 8.8.1.5.3 Arithmetic expressions > Standard-decimal arithmetic > Basic arithmetic operations in standard-decimal arithmetic)
- programs: fz04018
- source: `programs/fz04018.cob`
- WiseOwl: `fz04018.cob(24,1): error COBOLNET2796: PERFORM … PERFORM: this IF statement is written without its explicit scope terminator END-IF, so it is a conditional statement, and the operand of this phrase is an imperative-statement — ISO §14.5.1: "Any statement with a conditional phrase that is not termina`

### `WE_REJECT:COBOLNET1625,COBOLNET2961` × 1 — first: fz03806 (clause 8.8.4.5 Conditional expressions > Simple condition-name condition (conditional variable))
- programs: fz03806
- source: `programs/fz03806.cob`
- WiseOwl: `fz03806.cob(9,1): error COBOLNET1625: condition-name 'IS-POSITIVE': the numeric literal 9999 in the VALUE clause is not a permissible value in the range the PICTURE indicates — it is not representable without truncation of leading or trailing nonzero digits (ISO §13.18.63.3 SR2)`

### `WE_REJECT:COBOLNET1627,COBOLNET2168,COBOLNET2326,COBOLNET2533` × 1 — first: fz02126 (clause 8.8.4.2.13 Conditional expressions > Simple relation conditions > Comparisons involving index-names or index data items)
- programs: fz02126
- source: `programs/fz02126.cob`
- WiseOwl: `fz02126.cob(8,1): error COBOLNET2168: data item 'WS-IDX1': the VALUE clause shall not be specified with USAGE INDEX (a data item of class index — ISO §13.16.3 SR10)`

### `WE_REJECT:COBOLNET1627,COBOLNET2326` × 1 — first: fz01292 (clause 15.2 Types of functions)
- programs: fz01292
- source: `programs/fz01292.cob`
- WiseOwl: `fz01292.cob(105,1): error COBOLNET2326: SET 'WS-IDX' TO '1' violates ISO §14.9.39.3 SR3: "If identifier-1 references a data item of class index, arithmetic-expression-1 shall not be specified"`

### `WE_REJECT:COBOLNET1627,COBOLNET2796` × 1 — first: fz03258 (clause 14.9.44 SUBTRACT statement)
- programs: fz03258
- source: `programs/fz03258.cob`
- WiseOwl: `fz03258.cob(30,4): error COBOLNET2796: PERFORM … PERFORM: this IF statement is written without its explicit scope terminator END-IF, so it is a conditional statement, and the operand of this phrase is an imperative-statement — ISO §14.5.1: "Any statement with a conditional phrase that is not termina`

### `WE_REJECT:COBOLNET1627,COBOLNET2966` × 1 — first: fz00242 (clause 15.32 EXCEPTION-STATEMENT function)
- programs: fz00242
- source: `programs/fz00242.cob`
- WiseOwl: `fz00242.cob(13,9): error COBOLNET1627: FUNCTION NUMVAL-F argument-1 is of class numeric; ISO §15.3 requires class alphanumeric, alphanumeric (numeric-edited), national (numeric-edited) or national (§15.69.3 r1). --permissive accepts it as a coercion extension`

### `WE_REJECT:COBOLNET1637,COBOLNET2326` × 1 — first: fz00578 (clause 8.8.4.2.13 Conditional expressions > Simple relation conditions > Comparisons involving index-names or index data items)
- programs: fz00578
- source: `programs/fz00578.cob`
- WiseOwl: `fz00578.cob(13,1): error COBOLNET1637: 'IDX' is an index-name, which is not an identifier (ISO §8.4.3.1.2): §13.18.38.3 SR7 admits an index-name only as a subscript, in PERFORM/SEARCH VARYING, in SET, or in a relation condition, and this position needs an identifier. SET a data item to the index fir`

### `WE_REJECT:COBOLNET1651,COBOLNET2306` × 1 — first: fz01309 (clause 14.9.48 UNSTRING statement)
- programs: fz01309
- source: `programs/fz01309.cob`
- WiseOwl: `fz01309.cob(52,1): error COBOLNET2306: UNSTRING 'WS-SEND-NATIONAL' mixes class NATIONAL with an operand of another class; ISO §14.9.48.3 SR3 requires that if any of the operands it names is of class national, all of them shall be (ISO §14.9.48.3 SR3)`

### `WE_REJECT:COBOLNET1663` × 1 — first: fz00003 (clause 15.85 STANDARD-COMPARE function)
- programs: fz00003
- source: `programs/fz00003.cob`
- WiseOwl: `fz00003.cob(25,1): error COBOLNET1663: FUNCTION STANDARD-COMPARE argument-3 ''ISO 14651_2020_TABLE1'' is not an ordering-name declared in an ORDER TABLE clause of the SPECIAL-NAMES paragraph — with four arguments written, the §15.85.2 general format's third position IS ordering-name-1 (ISO §15.85.3 `

### `WE_REJECT:COBOLNET1676,COBOLNET1740` × 1 — first: fz04103 (clause 14.9.4 CALL statement)
- programs: fz04103
- source: `programs/fz04103.cob`
- WiseOwl: `fz04103.cob(10,1): error COBOLNET1740: data item 'WS-TEXT': the VALUE alphanumeric literal (11 characters) exceeds the item's 10 character positions indicated by its PICTURE clause (ISO §13.18.63.3 SR4)`

### `WE_REJECT:COBOLNET1679` × 1 — first: fz03361 (clause 15.3.3.3 Common time formats > Permissible values for data associated with common time formats)
- programs: fz03361
- source: `programs/fz03361.cob`
- WiseOwl: `fz03361.cob(19,1): error COBOLNET1679: CALL … USING BY REFERENCE 'WS-DATA-PTR' is a data item of class pointer (ISO §14.9.4.3 SR10 — Format 1 shall pass neither by reference; the program-prototype CALL admits them under §14.8.2)`

### `WE_REJECT:COBOLNET1708` × 1 — first: fz04657 (clause 13.18.41 PRESENT WHEN clause)
- programs: fz04657
- source: `programs/fz04657.cob`
- WiseOwl: `fz04657.cob(9,40): error COBOLNET1708: the PRESENT WHEN clause of the §13.16.2 validation-clauses group: A data-division clause of the VALIDATE facility (the §13.16.2 validation-clauses group — CLASS, DEFAULT, DESTINATION, INVALID, PRESENT WHEN format 2, VARYING's validation leg, VALIDATE-STATUS/VAL`

### `WE_REJECT:COBOLNET1740,COBOLNET2796` × 1 — first: fz01321 (clause 15.81 SIGN function)
- programs: fz01321
- source: `programs/fz01321.cob`
- WiseOwl: `fz01321.cob(23,1): error COBOLNET2796: PERFORM … PERFORM: this IF statement is written without its explicit scope terminator END-IF, so it is a conditional statement, and the operand of this phrase is an imperative-statement — ISO §14.5.1: "Any statement with a conditional phrase that is not termina`

### `WE_REJECT:COBOLNET1757` × 1 — first: fz04522 (clause 8.8.3 Concatenation expressions)
- programs: fz04522
- source: `programs/fz04522.cob`
- WiseOwl: `fz04522.cob(39,1): error COBOLNET1757: STRING sending operand 'ALL "B"' is a figurative constant that begins with the word ALL, which literal-1 shall not be (ISO §14.9.43.3 SR2)`

### `WE_REJECT:COBOLNET2168` × 1 — first: fz02982 (clause 8.8.4.2 Conditional expressions > Simple relation conditions)
- programs: fz02982
- source: `programs/fz02982.cob`
- WiseOwl: `fz02982.cob(16,1): error COBOLNET2168: data item 'WS-INDEX-A': the VALUE clause shall not be specified with USAGE INDEX (a data item of class index — ISO §13.16.3 SR10)`

### `WE_REJECT:COBOLNET2168,COBOLNET2326` × 1 — first: fz01033 (clause 15.59 MAX function)
- programs: fz01033
- source: `programs/fz01033.cob`
- WiseOwl: `fz01033.cob(10,1): error COBOLNET2168: data item 'WS-INDEX': the VALUE clause shall not be specified with USAGE INDEX (a data item of class index — ISO §13.16.3 SR10)`

### `WE_REJECT:COBOLNET2168,COBOLNET2326,COBOLNET2533` × 1 — first: fz05221 (clause 8.8.4.2.13 Conditional expressions > Simple relation conditions > Comparisons involving index-names or index data items)
- programs: fz05221
- source: `programs/fz05221.cob`
- WiseOwl: `fz05221.cob(8,1): error COBOLNET2168: data item 'WS-IDX1': the VALUE clause shall not be specified with USAGE INDEX (a data item of class index — ISO §13.16.3 SR10)`

### `WE_REJECT:COBOLNET2306` × 1 — first: fz03283 (clause 14.9.22 INSPECT statement)
- programs: fz03283
- source: `programs/fz03283.cob`
- WiseOwl: `fz03283.cob(56,1): error COBOLNET2306: INSPECT 'WS-NAT-TGT' mixes class NATIONAL with an operand of another class; ISO §14.9.22.3 SR4 requires that if any of the operands it names is of class national, all of them shall be (ISO §14.9.22.3 SR4)`

### `WE_REJECT:COBOLNET2326,COBOLNET2532` × 1 — first: fz05306 (clause 8.8.4.2 Conditional expressions > Simple relation conditions)
- programs: fz05306
- source: `programs/fz05306.cob`
- WiseOwl: `fz05306.cob(20,1): error COBOLNET2326: SET 'WS-IDX-A' TO '1' violates ISO §14.9.39.3 SR3: "If identifier-1 references a data item of class index, arithmetic-expression-1 shall not be specified"`

### `WE_REJECT:COBOLNET2326,COBOLNET2796` × 1 — first: fz04787 (clause 8.8.1.5.4 Arithmetic expressions > Standard-decimal arithmetic > Exponentiation in standard-decimal arithmetic)
- programs: fz04787
- source: `programs/fz04787.cob`
- WiseOwl: `fz04787.cob(73,4): error COBOLNET2796: COMPUTE … ON SIZE ERROR: this COMPUTE statement is written without its explicit scope terminator END-COMPUTE, so it is a conditional statement, and the operand of this phrase is an imperative-statement — ISO §14.5.1: "Any statement with a conditional phrase tha`

### `WE_REJECT:COBOLNET2653` × 1 — first: fz03122 (clause 15.71 ORD-MAX function)
- programs: fz03122
- source: `programs/fz03122.cob`
- WiseOwl: `fz03122.cob(122,256): error COBOLNET2653: this free-form line has 264 character positions; ISO §6.1 3) a) allows at most 255 (a tab counts the positions it advances over, DOC-A.1-157)`

### `WE_REJECT:COBOLNET2967` × 1 — first: fz01186 (clause 14.9.17 GO TO statement)
- programs: fz01186
- source: `programs/fz01186.cob`
- WiseOwl: `fz01186.cob(36,1): error COBOLNET2967: A Format 1 GO TO that appears in a consecutive sequence of imperative statements shall be the last statement in that sequence (ISO §14.9.17.3 SR2); this one is followed by the imperative statement STOP in the same sequence, which can never execute`

### `WE_REJECT:COBOLNET3407` × 1 — first: fz02116 (clause 13.18.13 CODE-SET clause)
- programs: fz02116
- source: `programs/fz02116.cob`
- WiseOwl: `fz02116.cob(6,31): error COBOLNET3407: SPECIAL-NAMES paragraph: a separator period follows 'ALPHABET ALNUM-1 IS STANDARD-1' and the paragraph goes on with 'ALPHABET NAT-1 IS STANDARD-2' — the ISO §12.3.7.2 general format prints one separator period, after the last clause (§5.2.1); delete this period`
