# CobolSharp — Flagging Manifests (…M conformance modules)

Generated from the Wave-1 audit. For each `…M` flagging module, the obsolete/removed/non-conforming
constructs it presents and the diagnostic a conforming flagger must emit. Drives the WS-FLAG harness
(compile under the strict/flagging dialect, assert these flags). See docs/COBOL85_COMPLIANCE_PLAN.md §3.

## group: live-nc-if

### NC303M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L6: "000600 DATE-COMPILED.  22ND AUG 1988." (line 6) | obsolete | OBSOLETE — the DATE-COMPILED paragraph in the IDENTIFICATION DIVISION is an obsolete element (removed after COBOL-85). Source comment line 7 says "Message expected for above statement: OBSOLETE". |
| L19-20: "000900     ALTER NC303M-GOTO TO PROCEED TO NC303M-GOTO-2," / "NC303M-GOTO-2 TO PROCEED TO NC303M-CONTROL." (lines 19-20) | obsolete | OBSOLETE — the ALTER statement is an obsolete element (removed after COBOL-85). Source comment line 21 says "Message expected for above statement: OBSOLETE". |
| L25: "002500     GO TO." (line 25) | obsolete | OBSOLETE — the GO TO statement without a procedure-name (the altered/alterable GO TO, paragraph NC303M-GOTO) is an obsolete element (removed after COBOL-85). Source comment line 26 says "Message expected for above statement: OBSOLETE". |
| L29: "002900     GO TO." (line 29) | obsolete | OBSOLETE — the GO TO statement without a procedure-name (the altered/alterable GO TO, paragraph NC303M-GOTO-2) is an obsolete element (removed after COBOL-85). Source comment line 30 says "Message expected for above statement: OBSOLETE". Source comment line 32: "TOTAL NUMBER OF FLAGS EXPECTED = 4". |

### IF401M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L31: "003100     IF FUNCTION ACOS (1.0) = FUNCTION ACOS (1.0)" (line 31) | non-conforming-extension | NON-CONFORMING STANDARD — use of high-subset intrinsic function ACOS is above the minimum implementor subset and must be flagged. Source comment line 33: "MESSAGE EXPECTED FOR ABOVE STATEMENT: NON-CONFORMING STANDARD". |
| L36: "003600     IF FUNCTION ANNUITY (0, 4) = FUNCTION ANNUITY (0, 4)" (line 36) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function ANNUITY flagged. Source comment line 38. |
| L41: "004100     IF FUNCTION ASIN (1.0) = FUNCTION ASIN (1.0)" (line 41) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function ASIN flagged. Source comment line 43. |
| L46: "004600     IF FUNCTION ATAN (1.0) = FUNCTION ATAN (1.0)" (line 46) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function ATAN flagged. Source comment line 48. |
| L51: "005100     MOVE FUNCTION CHAR (37) TO WS-ANUM." (line 51) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function CHAR flagged. Source comment line 52. |
| L55: "005500     IF FUNCTION COS (1.0) = FUNCTION COS (1.0)" (line 55) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function COS flagged. Source comment line 57. |
| L60: "006000     MOVE FUNCTION CURRENT-DATE TO TEMP1." (line 60) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function CURRENT-DATE flagged. Source comment line 61. |
| L64-65: "006400     IF FUNCTION DATE-OF-INTEGER (1) =" / "FUNCTION DATE-OF-INTEGER (1)" (lines 64-65) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function DATE-OF-INTEGER flagged. Source comment line 67. |
| L70: "007000     IF FUNCTION DAY-OF-INTEGER (1) = FUNCTION DAY-OF-INTEGER (1)" (line 70) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function DAY-OF-INTEGER flagged. Source comment line 72. |
| L75: "007500     IF FUNCTION FACTORIAL (1) = FUNCTION FACTORIAL (1)" (line 75) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function FACTORIAL flagged. Source comment line 77. |
| L80: "008000     IF FUNCTION INTEGER (1.0) = FUNCTION INTEGER (1.0)" (line 80) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function INTEGER flagged. Source comment line 82. |
| L85-86: "008500     IF FUNCTION INTEGER-OF-DATE (16010101) =" / "FUNCTION INTEGER-OF-DATE (16010101)" (lines 85-86) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function INTEGER-OF-DATE flagged. Source comment line 88. |
| L91-92: "009100     IF FUNCTION INTEGER-OF-DAY (1601001) =" / "FUNCTION INTEGER-OF-DAY (1601001)" (lines 91-92) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function INTEGER-OF-DAY flagged. Source comment line 94. |
| L97-98: "009700     IF FUNCTION INTEGER-PART (4.578) =" / "FUNCTION INTEGER-PART (4.578)" (lines 97-98) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function INTEGER-PART flagged. Source comment line 100. Source comment line 104: "TOTAL NUMBER OF FLAGS EXPECTED = 14". |

### IF402M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L35: "003500     IF FUNCTION LENGTH (\"ABC\") = FUNCTION LENGTH (\"ABC\")" (line 35) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function LENGTH flagged. Source comment line 37: "MESSAGE EXPECTED FOR ABOVE STATEMENT: NON-CONFORMING STANDARD". |
| L40: "004000     IF FUNCTION LOG (1.0) = FUNCTION LOG (1.0)" (line 40) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function LOG flagged. Source comment line 42. |
| L45: "004500     IF FUNCTION LOG10 (1.0) = FUNCTION LOG10 (1.0)" (line 45) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function LOG10 flagged. Source comment line 47. |
| L50: "005000     MOVE FUNCTION LOWER-CASE (\"ABC\") TO WS-AN-TEMP." (line 50) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function LOWER-CASE flagged. Source comment line 51. |
| L54-55: "005400     IF FUNCTION MAX (5, 6, 10, 3, 7) =" / "FUNCTION MAX (5, 6, 10, 3, 7)" (lines 54-55) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function MAX flagged. Source comment line 57. |
| L59: "005900     MOVE FUNCTION MAX (WS-TABLE (ALL, ALL, ALL)) TO WS-AN-TEMP." (line 59) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function MAX (with ALL subscript form) flagged a second time. Source comment line 60. |
| L63-64: "006300     IF FUNCTION MEAN (5, -2, -14, 0) =" / "FUNCTION MEAN (5, -2, -14, 0)" (lines 63-64) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function MEAN flagged. Source comment line 66. |
| L69-70: "006900     IF FUNCTION MEDIAN (5, -2, -14, 0) =" / "FUNCTION MEDIAN (5, -2, -14, 0)" (lines 69-70) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function MEDIAN flagged. Source comment line 72. |
| L75-76: "007500     IF FUNCTION MIDRANGE (5, -2, -14, 0) =" / "FUNCTION MIDRANGE (5, -2, -14, 0)" (lines 75-76) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function MIDRANGE flagged. Source comment line 78. |
| L81-82: "008100     IF FUNCTION MIN (5, 6, 10, 3, 7) =" / "FUNCTION MIN (5, 6, 10, 3, 7)" (lines 81-82) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function MIN flagged. Source comment line 84. |
| L86: "008600     MOVE FUNCTION MIN (WS-TABLE (ALL, ALL, ALL)) TO WS-AN-TEMP." (line 86) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function MIN (with ALL subscript form) flagged a second time. Source comment line 87. |
| L90: "009000     IF FUNCTION MOD (6, 6) = FUNCTION MOD (6, 6)" (line 90) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function MOD flagged. Source comment line 92. |
| L95: "009500     IF FUNCTION NUMVAL (\"4738\") = FUNCTION NUMVAL (\"4738\")" (line 95) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function NUMVAL flagged. Source comment line 97. |
| L100-101: "010000     IF FUNCTION NUMVAL-C (\"-$1,234.56\") =" / "FUNCTION NUMVAL-C (\"-$1,234.56\")" (lines 100-101) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function NUMVAL-C flagged. Source comment line 103. |
| L106: "010600     IF FUNCTION ORD (\"A\") = FUNCTION ORD (\"A\")" (line 106) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function ORD flagged. Source comment line 108. |
| L111-112: "011100     IF FUNCTION ORD-MAX (5, 3, 2, 8, 3, 1) =" / "FUNCTION ORD-MAX (5, 3, 2, 8, 3, 1)" (lines 111-112) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function ORD-MAX flagged. Source comment line 114. |
| L117-118: "011700     IF FUNCTION ORD-MIN (5, 3, 2, 8, 3, 1) =" / "FUNCTION ORD-MIN (5, 3, 2, 8, 3, 1)" (lines 117-118) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function ORD-MIN flagged. Source comment line 120. Source comment line 124: "TOTAL NUMBER OF FLAGS EXPECTED = 17". |

### IF403M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L29-30: "002900     IF FUNCTION PRESENT-VALUE (0, 23, 12, 9) =" / "FUNCTION PRESENT-VALUE (0, 23, 12, 9)" (lines 29-30) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function PRESENT-VALUE flagged. Source comment line 32: "MESSAGE EXPECTED FOR ABOVE STATEMENT: NON-CONFORMING STANDARD". |
| L35: "003500     IF FUNCTION RANDOM (1) = FUNCTION RANDOM (1)" (line 35) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function RANDOM flagged. Source comment line 37. |
| L40-41: "004000     IF FUNCTION RANGE (5, -2, -14, 0) =" / "FUNCTION RANGE (5, -2, -14, 0)" (lines 40-41) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function RANGE flagged. Source comment line 43. |
| L46: "004600     IF FUNCTION REM (0, 20) = FUNCTION REM (0, 20)" (line 46) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function REM flagged. Source comment line 48. |
| L51: "005100     MOVE FUNCTION REVERSE (\"ABC\") TO WS-AN-TEMP." (line 51) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function REVERSE flagged. Source comment line 52. |
| L55: "005500     IF FUNCTION SIN (1.0) = FUNCTION SIN (1.0)" (line 55) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function SIN flagged. Source comment line 57. |
| L60: "006000     IF FUNCTION SQRT (0) = FUNCTION SQRT (0)" (line 60) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function SQRT flagged. Source comment line 62. |
| L65-66: "006500     IF FUNCTION STANDARD-DEVIATION (5, -2, -14, 0) =" / "FUNCTION STANDARD-DEVIATION (5, -2, -14, 0)" (lines 65-66) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function STANDARD-DEVIATION flagged. Source comment line 68. |
| L71-72: "007100     IF FUNCTION SUM (5, -2, -14, 0) =" / "FUNCTION SUM (5, -2, -14, 0)" (lines 71-72) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function SUM flagged. Source comment line 74. |
| L77: "007700     IF FUNCTION TAN (1.0) = FUNCTION TAN (1.0)" (line 77) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function TAN flagged. Source comment line 79. |
| L82: "008200     MOVE FUNCTION UPPER-CASE (\"abc\") TO WS-AN-TEMP." (line 82) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function UPPER-CASE flagged. Source comment line 83. |
| L86-87: "008600     IF FUNCTION VARIANCE (5, -2, -14, 0) =" / "FUNCTION VARIANCE (5, -2, -14, 0)" (lines 86-87) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function VARIANCE flagged. Source comment line 89. |
| L92: "009200     MOVE FUNCTION WHEN-COMPILED TO WS-AN-TEMP." (line 92) | non-conforming-extension | NON-CONFORMING STANDARD — high-subset intrinsic function WHEN-COMPILED flagged. Source comment line 93. Source comment line 97: "TOTAL NUMBER OF FLAGS EXPECTED = 13". |

## group: live-io

### IX301M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L17: '           ORGANIZATION IS INDEXED' (SELECT TFIL); paired with the inline annotation L18 '*Message expected for above statement: NON-CONFORMING STANDARD' — INDEXED organization is an intermediate-subset (above Level-1) feature relative to this module's tested subset | non-conforming-extension | NON-CONFORMING STANDARD — flag the INDEXED organization clause as a feature outside the Level-1 indexed I-O subset being tested |
| L20: '           ACCESS MODE IS RANDOM'; annotation L21 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag ACCESS MODE IS RANDOM as an intermediate-subset feature beyond Level-1 indexed I-O |
| L23: '           RECORD KEY IS RKEY.'; annotation L24 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag RECORD KEY IS as an intermediate-subset feature beyond Level-1 indexed I-O |
| L45-46: 'READ TFIL INVALID KEY PERFORM INV-PARA / NOT INVALID KEY PERFORM DONE-PARA.'; annotation L47 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the READ ... NOT INVALID KEY phrase (intermediate-subset extension to the Level-1 READ statement) |
| L50-51: 'REWRITE FREC INVALID KEY PERFORM INV-PARA / NOT INVALID KEY PERFORM DONE-PARA.'; annotation L52 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the REWRITE ... NOT INVALID KEY phrase (intermediate-subset extension to the Level-1 REWRITE statement) |
| L55-56: 'WRITE FREC INVALID KEY PERFORM INV-PARA / NOT INVALID KEY PERFORM DONE-PARA.'; annotation L57 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the WRITE ... NOT INVALID KEY phrase (intermediate-subset extension to the Level-1 WRITE statement) |
| L60-61: 'DELETE TFIL INVALID KEY PERFORM INV-PARA / NOT INVALID KEY PERFORM DONE-PARA.'; annotation L62 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the DELETE ... NOT INVALID KEY phrase (intermediate-subset extension to the Level-1 DELETE statement) |

### IX401M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L15: '    SELECT OPTIONAL TFIL ASSIGN'; annotation L16 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the SELECT OPTIONAL phrase as a high-subset feature beyond the tested indexed-I-O subset |
| L19: '        RESERVE 2 AREAS'; annotation L20 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the RESERVE n AREAS clause as a high-subset feature |
| L23: '        ACCESS MODE IS DYNAMIC'; annotation L24 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag ACCESS MODE IS DYNAMIC as a high-subset feature |
| L27: '        ALTERNATE RECORD KEY IS BEANO.'; annotation L28 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the ALTERNATE RECORD KEY clause as a high-subset feature |
| L38-39: 'FD TFIL / RECORD IS VARYING IN SIZE FROM 18 TO 36 CHARACTERS.'; annotation L40 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the RECORD IS VARYING IN SIZE clause as a high-subset feature |
| L60: '    CLOSE TFIL WITH LOCK.'; annotation L61 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the CLOSE ... WITH LOCK phrase as a high-subset feature |
| L64: '    OPEN EXTEND TFIL2.'; annotation L65 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag OPEN EXTEND (extend mode on an indexed file) as a high-subset feature |
| L69-70: 'READ TFIL NEXT RECORD / AT END DISPLAY "AT END".'; annotation L71 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the READ ... NEXT RECORD statement (sequential-access read of an indexed file) as a high-subset feature |
| L74-76: 'READ TFIL RECORD / KEY IS RKEY / INVALID KEY DISPLAY "INVALID".'; annotation L77 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the READ ... KEY IS phrase (random read by record key) as a high-subset feature |
| L80-81: 'START TFIL KEY IS EQUAL TO RKEY / INVALID KEY DISPLAY "INVALID".'; annotation L82 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the START statement as a high-subset feature |

### RL301M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L17: '           ORGANIZATION IS RELATIVE'; annotation L18 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the RELATIVE organization clause as an intermediate-subset feature beyond Level-1 relative I-O |
| L19: '           ACCESS MODE IS RANDOM'; annotation L20 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag ACCESS MODE IS RANDOM as an intermediate-subset feature beyond Level-1 relative I-O |
| L42-43: 'READ TFIL INVALID KEY PERFORM INV-PARA / NOT INVALID KEY PERFORM DONE-PARA.'; annotation L44 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the READ ... NOT INVALID KEY phrase (intermediate-subset extension) |
| L47-48: 'REWRITE FREC INVALID KEY PERFORM INV-PARA / NOT INVALID KEY PERFORM DONE-PARA.'; annotation L49 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the REWRITE ... NOT INVALID KEY phrase (intermediate-subset extension) |
| L52-53: 'WRITE FREC INVALID KEY PERFORM INV-PARA / NOT INVALID KEY PERFORM DONE-PARA.'; annotation L54 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the WRITE ... NOT INVALID KEY phrase (intermediate-subset extension) |
| L57-58: 'DELETE TFIL INVALID KEY PERFORM INV-PARA / NOT INVALID KEY PERFORM DONE-PARA.'; annotation L59 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the DELETE ... NOT INVALID KEY phrase (intermediate-subset extension) |

### RL401M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L15: '    SELECT OPTIONAL TFIL ASSIGN'; annotation L16 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the SELECT OPTIONAL phrase as a high-subset feature beyond the tested relative-I-O subset |
| L18: '        RESERVE 2 AREAS'; annotation L19 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the RESERVE n AREAS clause as a high-subset feature |
| L21: '        ACCESS MODE IS DYNAMIC'; annotation L22 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag ACCESS MODE IS DYNAMIC as a high-subset feature |
| L30: '    SAME RECORD AREA FOR TFIL2, TFIL.' (I-O-CONTROL); annotation L31 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the SAME RECORD AREA clause as a high-subset feature |
| L35-36: 'FD TFIL / RECORD IS VARYING IN SIZE FROM 1 TO 8 CHARACTERS.'; annotation L37 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the RECORD IS VARYING IN SIZE clause as a high-subset feature |
| L60: '    CLOSE TFIL WITH LOCK.'; annotation L61 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the CLOSE ... WITH LOCK phrase as a high-subset feature |
| L65: '    OPEN EXTEND TFIL2.'; annotation L66 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag OPEN EXTEND on a relative file as a high-subset feature |
| L69-70: 'READ TFIL NEXT RECORD / AT END DISPLAY "AT END".'; annotation L71 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the READ ... NEXT RECORD statement (sequential-access read of a relative file) as a high-subset feature |
| L74-75: 'START TFIL KEY IS EQUAL TO RKEY / INVALID KEY STOP RUN.'; annotation L76 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the START statement as a high-subset feature |

### SQ303M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L26: '    MULTIPLE FILE TAPE CONTAINS TFIL2.' (I-O-CONTROL); annotation L27 '*Message expected for above statement: OBSOLETE' | obsolete | OBSOLETE — flag the MULTIPLE FILE TAPE clause as an obsolete element (removed after COBOL-85) |
| L42: '    OPEN INPUT TFIL REVERSED.'; annotation L43 '*Message expected for above statement: OBSOLETE' | obsolete | OBSOLETE — flag the OPEN ... REVERSED phrase as an obsolete element (removed after COBOL-85) |

### SQ401M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L15: '    SELECT OPTIONAL TFIL ASSIGN'; annotation L16 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the SELECT OPTIONAL phrase as a high-subset feature beyond the tested sequential-I-O subset |
| L19: '        RESERVE 2 AREAS'; annotation L20 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the RESERVE n AREAS clause as a high-subset feature |
| L23: '        PADDING CHARACTER IS "P"'; annotation L24 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the PADDING CHARACTER clause as a high-subset feature (also an obsolete element) |
| L26: '        RECORD DELIMITER IS STANDARD-1'; annotation L27 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the RECORD DELIMITER clause as a high-subset feature |
| L41: '    SAME RECORD AREA FOR TFIL2, TFIL' (I-O-CONTROL); annotation L42 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the SAME RECORD AREA clause as a high-subset feature |
| L44: '    MULTIPLE FILE TAPE CONTAINS TFIL2.' (I-O-CONTROL); annotation L45 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the MULTIPLE FILE TAPE clause as a high-subset feature (also an obsolete element) |
| L50: '    BLOCK CONTAINS 1 TO 8 RECORDS' (FD TFIL); annotation L51 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the BLOCK CONTAINS clause as a high-subset feature |
| L53: '    RECORD VARYING IN SIZE FROM 1 TO 8 CHARACTERS' (FD TFIL); annotation L54 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the RECORD VARYING IN SIZE clause as a high-subset feature |
| L56: '    LINAGE IS 20 LINES' (FD TFIL); annotation L57 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the LINAGE clause as a high-subset feature |
| L59-62: 'LABEL RECORDS ARE STANDARD / VALUE OF / XXXXX074 / IS VKEY.' (FD TFIL); annotation L63 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the VALUE OF implementor-name IS clause as a high-subset feature (also an obsolete element). The LABEL RECORDS clause is likewise obsolete but the annotation is attached to the VALUE OF statement |
| L95: '    CLOSE TFIL REEL FOR REMOVAL.'; annotation L96 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the CLOSE ... REEL FOR REMOVAL phrase as a high-subset feature |
| L100: '    CLOSE TFIL WITH NO REWIND.'; annotation L101 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the CLOSE ... WITH NO REWIND phrase as a high-subset feature |
| L105: '    CLOSE TFIL WITH LOCK.'; annotation L106 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the CLOSE ... WITH LOCK phrase as a high-subset feature |
| L109: '    OPEN INPUT TFIL REVERSED.'; annotation L110 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the OPEN ... REVERSED phrase as a high-subset feature (also an obsolete element) |
| L114: '    OPEN INPUT TFIL WITH NO REWIND.'; annotation L115 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the OPEN ... WITH NO REWIND phrase as a high-subset feature |
| L119: '    OPEN EXTEND TFIL3.'; annotation L120 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the OPEN EXTEND phrase as a high-subset feature |
| L124-125: 'READ TFIL NEXT RECORD / AT END DISPLAY "AT END".'; annotation L126 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the READ ... NEXT RECORD statement as a high-subset feature |
| L132: '    WRITE FREC AT END-OF-PAGE DISPLAY "HELLO".'; annotation L133 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | NON-CONFORMING STANDARD — flag the WRITE ... AT END-OF-PAGE phrase as a high-subset feature |

## group: live-misc

### SM301M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L24: '     COPY KSM31.' (annotated L23 '*Message expected for following statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the COPY statement as NON-CONFORMING STANDARD (intermediate-subset COPY feature). This is the only flag; module header L4-5 states it 'tests the flagging of the intermediate subset COPY feature'. TOTAL NUMBER OF FLAGS EXPECTED = 1 (L27). |

### SM401M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L24: '     COPY KSM41 REPLACING "PIG" BY "HORSE".' (annotated L23 '*Message expected for following statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag COPY ... REPLACING as NON-CONFORMING STANDARD (high-subset source-text-manipulation feature). Header L4-6 states the program tests flagging of high-subset source text manipulation features. |
| L27: '     REPLACE OFF.' (annotated L28 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the REPLACE statement (REPLACE OFF) as NON-CONFORMING STANDARD (high-subset source-text-manipulation feature). TOTAL NUMBER OF FLAGS EXPECTED = 2 (L30). |

### ST301M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L30: '     SAME SORT-MERGE AREA FOR TFIL-5, TFIL.' (annotated L31 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the I-O-CONTROL SAME SORT-MERGE AREA clause as NON-CONFORMING STANDARD (intermediate-subset sort-merge feature). |
| L34: 'SD  TFIL.' (annotated L35 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the sort-merge file description (SD entry) as NON-CONFORMING STANDARD (intermediate-subset sort-merge feature). |
| L62-64: 'MERGE TFIL ON ASCENDING KEY DATA-1 / USING TFIL-2 TFIL-3 / OUTPUT PROCEDURE IS ST301M-RETURN.' (annotated L66 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the MERGE statement as NON-CONFORMING STANDARD (intermediate-subset sort-merge feature). |
| L69: '     RELEASE FREC.' (annotated L70 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the RELEASE statement as NON-CONFORMING STANDARD (intermediate-subset sort-merge feature). |
| L73-74: 'RETURN TFIL RECORD / AT END DISPLAY "AT END".' (annotated L75 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the RETURN statement as NON-CONFORMING STANDARD (intermediate-subset sort-merge feature). |
| L78-80: 'SORT TFIL ON ASCENDING KEY DATA-1 / INPUT PROCEDURE IS ST301M-RELEASE / GIVING TFIL-4.' (annotated L81 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the SORT statement as NON-CONFORMING STANDARD (intermediate-subset sort-merge feature). TOTAL NUMBER OF FLAGS EXPECTED = 6 (L84). |

### RW301M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L41-42: 'FD TFIL2 / REPORT IS RFIL2.' (annotated L43 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the FD REPORT IS clause as NON-CONFORMING STANDARD (Report Writer feature not in the conforming subset). |
| L50: 'REPORT SECTION.' (annotated L51 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the REPORT SECTION header as NON-CONFORMING STANDARD (Report Writer feature). |
| L52: 'RD  RFIL2.' (annotated L53 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the report description (RD entry) as NON-CONFORMING STANDARD (Report Writer feature). |
| L54-55: '01  RREC / TYPE IS DETAIL.' (annotated L56 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the report-group TYPE IS DETAIL clause as NON-CONFORMING STANDARD (Report Writer feature). |
| L57-58: '02  PIC 9(8) / SOURCE IS RKEY' (annotated L59 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the report-entry SOURCE IS clause as NON-CONFORMING STANDARD (Report Writer feature). |
| L60: 'COLUMN NUMBER IS 1' (annotated L61 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the report-entry COLUMN NUMBER clause as NON-CONFORMING STANDARD (Report Writer feature). |
| L62: 'LINE NUMBER IS PLUS 1.' (annotated L63 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the report-entry LINE NUMBER clause as NON-CONFORMING STANDARD (Report Writer feature). |
| L73: '     INITIATE RFIL2.' (annotated L74 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the INITIATE statement as NON-CONFORMING STANDARD (Report Writer feature). |
| L75: '     GENERATE RREC.' (annotated L76 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the GENERATE statement as NON-CONFORMING STANDARD (Report Writer feature). |
| L77: '     TERMINATE RFIL2.' (annotated L78 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the TERMINATE statement as NON-CONFORMING STANDARD (Report Writer feature). TOTAL NUMBER OF FLAGS EXPECTED = 10 (L84). Header L4-5 states the program tests flagging of features used in report writing. |

### RW302M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L27: '     MULTIPLE FILE TAPE CONTAINS TFIL.' (annotated L28 '*Message expected for above statement: OBSOLETE') | obsolete | Flag the I-O-CONTROL MULTIPLE FILE TAPE clause as OBSOLETE (obsolete element in COBOL-85, removed in COBOL-2002). |
| L33-34: 'FD TFIL / LABEL RECORDS STANDARD' (annotated L35 '*Message expected for above statement: OBSOLETE') | obsolete | Flag the FD LABEL RECORDS clause as OBSOLETE (obsolete element in COBOL-85). |
| L36-39: 'VALUE OF / XXXXX074 / IS / XXXXX075.' (annotated L40 '*Message expected for above statement: OBSOLETE') | obsolete | Flag the FD VALUE OF clause as OBSOLETE (obsolete element in COBOL-85). TOTAL NUMBER OF FLAGS EXPECTED = 3 (L79). Header L4-6 states the program tests flagging of OBSOLETE features used in report writing. Note: the Report Writer constructs present (REPORT IS / REPORT SECTION / RD / TYPE / SOURCE / INITIATE / GENERATE / TERMINATE, L46-73) are deliberately NOT annotated here — RW302M targets only the 3 obsolete file-I/O clauses. |

### IC116M  _(emits CCVS report)_

| Construct | Category | Expected flag |
|---|---|---|

### IC117M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|

### IC118M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|

### IC401M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L3: '      IC401M IS INITIAL.' (PROGRAM-ID ... IS INITIAL; annotated L4 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the PROGRAM-ID IS INITIAL clause as NON-CONFORMING STANDARD (high-subset inter-program-communication feature). |
| L18: '01 GLOB IS GLOBAL   PIC IS X(2) VALUE IS "HI".' (annotated L19 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the GLOBAL clause (data item IS GLOBAL) as NON-CONFORMING STANDARD (high-subset IPC feature). |
| L21: '01 EXTE IS EXTERNAL PIC IS X(5).' (annotated L22 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the EXTERNAL clause (data item IS EXTERNAL) as NON-CONFORMING STANDARD (high-subset IPC feature). |
| L29: '     USE GLOBAL AFTER STANDARD ERROR PROCEDURE ON I-O.' (annotated L30 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the USE GLOBAL declarative as NON-CONFORMING STANDARD (high-subset IPC feature). |
| L39: '     CANCEL "NESTEDPROG".' (annotated L40 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the CANCEL statement as NON-CONFORMING STANDARD (high-subset IPC feature). |
| L44: '     CALL "NESTEDPROG" USING BY REFERENCE GLOB.' (annotated L45 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the CALL ... USING BY REFERENCE phrase as NON-CONFORMING STANDARD (high-subset IPC feature). |
| L49: '     CALL "FIC401M" USING BY CONTENT GLOB.' (annotated L50 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the CALL ... USING BY CONTENT phrase as NON-CONFORMING STANDARD (high-subset IPC feature). |
| L53: 'IDENTIFICATION DIVISION.' (the nested-program IDENTIFICATION DIVISION header; annotated L54 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the nested-program IDENTIFICATION DIVISION (contained/nested source program) as NON-CONFORMING STANDARD (high-subset IPC feature). |
| L56-57: 'PROGRAM-ID. / NESTEDPROG IS COMMON.' (annotated L58 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the PROGRAM-ID IS COMMON clause as NON-CONFORMING STANDARD (high-subset IPC feature). |
| L71: ' END PROGRAM NESTEDPROG.' (annotated L72 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the END PROGRAM NESTEDPROG marker as NON-CONFORMING STANDARD (high-subset IPC feature, contained-program end marker). |
| L74: ' END PROGRAM IC401M.' (annotated L73 '*Message expected for following statement: NON-CONFORMING STANDARD') | non-conforming-extension | Flag the END PROGRAM IC401M marker as NON-CONFORMING STANDARD (high-subset IPC feature, program end marker). TOTAL NUMBER OF FLAGS EXPECTED = 11 (L70). Header L6-8 states the program tests flagging of high-subset features used in inter-program communication. |

## group: removed-cm

### CM101M  _(emits CCVS report)_

| Construct | Category | Expected flag |
|---|---|---|
| L229 '023000 COMMUNICATION SECTION.' / L230 '023100 CD  CM-INQUE-1 FOR INPUT' (CD continues through L241 STATUS KEY / MESSAGE COUNT) | removed-after-85 | Flag the COMMUNICATION SECTION header and the CD (communication description) entry for an input queue — the entire MCS/communication facility (incl. the CD entry, SYMBOLIC QUEUE/SUB-QUEUE, MESSAGE DATE/TIME, SYMBOLIC SOURCE, TEXT LENGTH, END/STATUS KEY, MESSAGE COUNT clauses) is obsolete in COBOL-85 and was removed after COBOL-85 (absent from ISO 2002/2023). Diagnostic: obsolete/removed language element. |
| L254 '025400     ENABLE INPUT  CM-INQUE-1 WITH KEY' (also L437, L458, L470/478, L494) | removed-after-85 | Flag the ENABLE statement (communication facility) — obsolete in COBOL-85, removed after COBOL-85. |
| L268 '026800     RECEIVE CM-INQUE-1 MESSAGE INTO INCOMING-MSG' with NO DATA phrase (also L363, L384) | removed-after-85 | Flag the RECEIVE statement (communication facility) — obsolete in COBOL-85, removed after COBOL-85. |
| L271 '027100     ACCEPT CM-INQUE-1 MESSAGE COUNT.' (also L330, L401 ACCEPT...MESSAGE COUNT / L421 ACCEPT...COUNT) | removed-after-85 | Flag the ACCEPT ... MESSAGE COUNT form (communication facility ACCEPT) — obsolete in COBOL-85, removed after COBOL-85. |
| L278 '027800         DISABLE INPUT CM-INQUE-1 WITH KEY' (also L509, L530, L550) | removed-after-85 | Flag the DISABLE statement (communication facility) — obsolete in COBOL-85, removed after COBOL-85. |

### CM102M  _(emits CCVS report)_

| Construct | Category | Expected flag |
|---|---|---|
| L277 '027700 COMMUNICATION SECTION.' / L278 '027800 CD  CM-OUTQUE-1 FOR OUTPUT' (CD continues through L283: DESTINATION COUNT / TEXT LENGTH / STATUS KEY / ERROR KEY / SYMBOLIC DESTINATION) | removed-after-85 | Flag the COMMUNICATION SECTION and the output CD entry (with DESTINATION COUNT, TEXT LENGTH, STATUS KEY, ERROR KEY, SYMBOLIC DESTINATION clauses) — obsolete in COBOL-85, removed after COBOL-85. |
| L301 '030100     DISABLE OUTPUT CM-OUTQUE-1 WITH KEY' (also L319, L342, L364, L384) | removed-after-85 | Flag the DISABLE statement (communication facility) — obsolete in COBOL-85, removed after COBOL-85. |
| L405 '040500     SEND CM-OUTQUE-1 FROM MSG-70 WITH EMI' with AFTER ADVANCING PAGE (also L408, L411 WITH EGI, L431/L536/L560/L583 FROM ERR-MSG WITH EMI) | removed-after-85 | Flag the SEND statement (communication facility), including the WITH EMI/EGI/ESI indicator phrase and the AFTER/BEFORE ADVANCING phrase — obsolete in COBOL-85, removed after COBOL-85. |
| L447 '044700     ENABLE OUTPUT CM-OUTQUE-1 WITH KEY' (also L470, L492, L514) | removed-after-85 | Flag the ENABLE statement (communication facility) — obsolete in COBOL-85, removed after COBOL-85. |

### CM103M  _(emits CCVS report)_

| Construct | Category | Expected flag |
|---|---|---|
| L180 '018000 COMMUNICATION SECTION.' / L181-183 'CD  CM-INQUE-1 FOR INPUT' with implicit (positional) data-name list (MAIN-QUEUE NO-SPEC-1 ... END-KEY IN-STATUS FILLER) and L184 'CD  CM-OUTQUE-1 FOR OUTPUT.' with following record description | removed-after-85 | Flag the COMMUNICATION SECTION and both CD entries (Format-using-implicit-record-area input CD and output CD) — obsolete in COBOL-85, removed after COBOL-85. |
| L202 '020200     ENABLE INPUT CM-INQUE-1 WITH KEY' and L204 '020400     ENABLE OUTPUT CM-OUTQUE-1 WITH KEY' | removed-after-85 | Flag the ENABLE statement (communication facility) — obsolete in COBOL-85, removed after COBOL-85. |
| L210 '021000     RECEIVE CM-INQUE-1 MESSAGE INTO MSG.' | removed-after-85 | Flag the RECEIVE statement (communication facility) — obsolete in COBOL-85, removed after COBOL-85. |
| L215 '021500     SEND CM-OUTQUE-1 FROM MSG WITH EMI.' | removed-after-85 | Flag the SEND statement (communication facility) with WITH EMI indicator — obsolete in COBOL-85, removed after COBOL-85. |

### CM104M  _(emits CCVS report)_

| Construct | Category | Expected flag |
|---|---|---|
| L176 '017600 COMMUNICATION SECTION.' with four CD entries: L177 'CD  CM-INQUE-1 FOR INPUT.', L194 'CD  CM-OUTQUE-1 FOR OUTPUT.', L205-207 'CD  CM-INQUE-2 FOR INPUT' (positional FILLER list), L216-219 'CD  CM-OUTQUE-2 FOR OUTPUT' (TEXT LENGTH / STATUS KEY / ERROR KEY) | removed-after-85 | Flag the COMMUNICATION SECTION and all four CD entries (two input, two output) — obsolete in COBOL-85, removed after COBOL-85. |
| L233 '023300     ENABLE INPUT CM-INQUE-1 WITH KEY' (also L235 INPUT CM-INQUE-2, L237 OUTPUT CM-OUTQUE-1, L239 OUTPUT CM-OUTQUE-2) | removed-after-85 | Flag the ENABLE statement (communication facility) — obsolete in COBOL-85, removed after COBOL-85. |
| L243 '024300     RECEIVE CM-INQUE-1 MESSAGE INTO MSG' with NO DATA (also L267 RECEIVE CM-INQUE-2) | removed-after-85 | Flag the RECEIVE statement (communication facility) — obsolete in COBOL-85, removed after COBOL-85. |
| L245 '024500     ACCEPT CM-INQUE-1 COUNT.' (also L269 ACCEPT CM-INQUE-2 COUNT) | removed-after-85 | Flag the ACCEPT ... COUNT (message count) form (communication facility ACCEPT) — obsolete in COBOL-85, removed after COBOL-85. |
| L249 '024900     SEND CM-OUTQUE-2 FROM MSG WITH EMI.' (also L273 SEND CM-OUTQUE-1 FROM MSG WITH EMI) | removed-after-85 | Flag the SEND statement (communication facility) with WITH EMI indicator — obsolete in COBOL-85, removed after COBOL-85. |

### CM105M  _(emits CCVS report)_

| Construct | Category | Expected flag |
|---|---|---|
| L175 '017500 COMMUNICATION SECTION.' / L176-177 'CD  CM-INQUE-1 INPUT STATUS KEY IS IN-STAT SUB-QUEUE-3 IS-OF-NO-INTEREST COUNT NAMED-BELOW SOURCE NOT-USED.' | removed-after-85 | Flag the COMMUNICATION SECTION and the input CD entry (with STATUS KEY, SUB-QUEUE-3, COUNT, SOURCE clauses) — obsolete in COBOL-85, removed after COBOL-85. |
| L198 '019800     ENABLE INPUT CM-INQUE-1 KEY' (also L208 DISABLE INPUT CM-INQUE-1 KEY at BEGIN-TESTS) | removed-after-85 | Flag the ENABLE statement (communication facility) — obsolete in COBOL-85, removed after COBOL-85. |
| L208 '020800     DISABLE INPUT CM-INQUE-1 KEY' | removed-after-85 | Flag the DISABLE statement (communication facility) — obsolete in COBOL-85, removed after COBOL-85. |
| L205 '020500     ACCEPT CM-INQUE-1 COUNT.' (recurs throughout ACCEPT-TEST-01, e.g. L326, L335, L344, ...) | removed-after-85 | Flag the ACCEPT ... COUNT (message count) form (communication facility ACCEPT) — obsolete in COBOL-85, removed after COBOL-85. |
| L426 '042600     RECEIVE CM-INQUE-1 MESSAGE INTO RE-MARK' with NO DATA | removed-after-85 | Flag the RECEIVE statement (communication facility) — obsolete in COBOL-85, removed after COBOL-85. |

### CM201M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L44 '004400 COMMUNICATION SECTION.' / L45 'CD  CM-INQUE-1 FOR INITIAL INPUT.' and L53 'CD  CM-OUTQUE-1 FOR OUTPUT.' | removed-after-85 | Flag the COMMUNICATION SECTION and the CD entries, including the FOR INITIAL INPUT (initial-input CD) form — obsolete in COBOL-85, removed after COBOL-85. |
| L63 '006300     ENABLE OUTPUT CM-OUTQUE-1 WITH KEY' | removed-after-85 | Flag the ENABLE statement (communication facility) — obsolete in COBOL-85, removed after COBOL-85. |
| L66 '006600     RECEIVE CM-INQUE-1 MESSAGE INTO MSG-72' with NO DATA | removed-after-85 | Flag the RECEIVE statement (communication facility) — obsolete in COBOL-85, removed after COBOL-85. |
| L71 '007100     SEND CM-OUTQUE-1 FROM RECOGNITION-MSG-1 WITH EMI.' (also L73, L75 WITH EGI, L81, L84 WITH EGI) | removed-after-85 | Flag the SEND statement (communication facility) with WITH EMI/EGI indicator — obsolete in COBOL-85, removed after COBOL-85. |
| L76 '007600     ACCEPT CM-INQUE-1 MESSAGE COUNT.' | removed-after-85 | Flag the ACCEPT ... MESSAGE COUNT form (communication facility ACCEPT) — obsolete in COBOL-85, removed after COBOL-85. |

### CM202M  _(emits CCVS report)_

| Construct | Category | Expected flag |
|---|---|---|
| L207 '020700 COMMUNICATION SECTION.' / L208 'CD  CM-INQUE-1 INPUT.' and L219-225 'CD  CM-OUTQUE-1 OUTPUT' with DESTINATION COUNT / TEXT LENGTH / STATUS KEY / DESTINATION TABLE OCCURS 2 TIMES INDEXED BY I1 / ERROR KEY / DESTINATION | removed-after-85 | Flag the COMMUNICATION SECTION and the two CD entries, including the DESTINATION TABLE OCCURS (multiple-destination output CD) clause — obsolete in COBOL-85, removed after COBOL-85. |
| L241 '024100     ENABLE OUTPUT CM-OUTQUE-1 WITH KEY' and L248 '024800     ENABLE INPUT TERMINAL CM-INQUE-1 WITH KEY' (also L257, L265, L354, L369) | removed-after-85 | Flag the ENABLE statement (communication facility), including the ENABLE INPUT TERMINAL form — obsolete in COBOL-85, removed after COBOL-85. |
| L251 '025100     DISABLE INPUT TERMINAL CM-INQUE-1 WITH KEY' (also L268, L273, L388, L403) | removed-after-85 | Flag the DISABLE statement (communication facility), including the DISABLE INPUT TERMINAL form — obsolete in COBOL-85, removed after COBOL-85. |
| L247 '024700     SEND CM-OUTQUE-1 FROM ENABLE-MSG WITH EMI.' (and SEND ... WITH ESI/EGI/END-FLAG, BEFORE ADVANCING, and partial-segment SEND forms throughout, e.g. L437 SEND...FROM SEND-MSG, L455 SEND...WITH END-FLAG, L498 WITH ESI, L506 WITH EGI, L532 BEFORE ADVANCING 4 LINES) | removed-after-85 | Flag the SEND statement (communication facility) in all its forms (WITH identifier/EMI/ESI/EGI/END-FLAG, BEFORE/AFTER ADVANCING, segmented and incomplete messages) — obsolete in COBOL-85, removed after COBOL-85. |
| L285 '028500     RECEIVE CM-INQUE-1 MESSAGE INTO MSG-1  NO DATA' and L317 'RECEIVE CM-INQUE-1 SEGMENT INTO MSG-1' (also L342 SEGMENT INTO RE-MARK) | removed-after-85 | Flag the RECEIVE statement (communication facility), including the RECEIVE ... SEGMENT form — obsolete in COBOL-85, removed after COBOL-85. |

### CM303M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L16 '001600 COMMUNICATION SECTION.' / L17 'CD COMMNAME FOR INITIAL INPUT.' / L18-19 record CREC / CNAME1 PIC X(87) | removed-after-85 | Flag the COMMUNICATION SECTION and the CD entry (FOR INITIAL INPUT) — obsolete communication feature. (This CD header itself is not separately enumerated in the module's flag count, but a conforming flagger flags the obsolete section/CD; the module's 2 explicitly-annotated flags are the two statements below.) |
| L28 '002800     DISABLE INPUT COMMNAME WITH KEY CNAME1.' — annotated L29 '*Message expected for above statement: OBSOLETE' | obsolete | Flag exactly one OBSOLETE message for the DISABLE statement (communication facility, obsolete in COBOL-85). Expected diagnostic text/category: OBSOLETE. |
| L32 '003200     ENABLE INPUT COMMNAME WITH KEY CNAME1.' — annotated L33 '*Message expected for above statement: OBSOLETE' | obsolete | Flag exactly one OBSOLETE message for the ENABLE statement (communication facility, obsolete in COBOL-85). Expected diagnostic text/category: OBSOLETE. Module self-asserts (L35) TOTAL NUMBER OF FLAGS EXPECTED = 2. |

### CM401M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L15 '001500 CD COMMNAME FOR INITIAL INPUT' — annotated L16 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | Flag NON-CONFORMING STANDARD for the input CD entry (Level-2 communication feature). Expected diagnostic category: NON-CONFORMING STANDARD (non-conforming extension / removed Level-2 communication feature). |
| L17 '001700     SYMBOLIC SUB-QUEUE-1 IS CQ.' — annotated L18 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | Flag NON-CONFORMING STANDARD for the SYMBOLIC SUB-QUEUE-1 clause of the CD (Level-2 communication feature). Expected category: NON-CONFORMING STANDARD. |
| L26-27 '002600 CD COMM2 FOR OUTPUT / 002700     DESTINATION TABLE OCCURS 7 TIMES.' — annotated L28 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | Flag NON-CONFORMING STANDARD for the output CD with DESTINATION TABLE OCCURS (multiple-destination, Level-2 communication feature). Expected category: NON-CONFORMING STANDARD. |
| L37 '003700     DISABLE INPUT COMMNAME WITH KEY CNAME1.' — annotated L38 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | Flag NON-CONFORMING STANDARD for the DISABLE statement (Level-2 communication feature). Expected category: NON-CONFORMING STANDARD. |
| L41 '004100     ENABLE INPUT COMMNAME WITH KEY CNAME1.' — annotated L42 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | Flag NON-CONFORMING STANDARD for the ENABLE statement (Level-2 communication feature). Expected category: NON-CONFORMING STANDARD. |
| L46 '004600     PURGE COMM2.' — annotated L47 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | Flag NON-CONFORMING STANDARD for the PURGE statement (Level-2 communication feature). Expected category: NON-CONFORMING STANDARD. |
| L50 '005000     SEND COMM2 FROM CNAME1.' — annotated L51 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | Flag NON-CONFORMING STANDARD for the SEND ... FROM (basic send) statement (Level-2 communication feature). Expected category: NON-CONFORMING STANDARD. |
| L54 '005400     SEND COMM2 FROM CNAME1 WITH CINT.' — annotated L55 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | Flag NON-CONFORMING STANDARD for the SEND ... WITH identifier (programmable end-indicator) form (Level-2 communication feature). Expected category: NON-CONFORMING STANDARD. |
| L58 '005800     SEND COMM2 FROM CNAME1 WITH ESI.' — annotated L59 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | Flag NON-CONFORMING STANDARD for the SEND ... WITH ESI (segment indicator) form (Level-2 communication feature). Expected category: NON-CONFORMING STANDARD. |
| L62 '006200     SEND COMM2 WITH EMI REPLACING LINE.' — annotated L63 '*Message expected for above statement: NON-CONFORMING STANDARD' | non-conforming-extension | Flag NON-CONFORMING STANDARD for the SEND ... WITH EMI REPLACING LINE (replacing-line) form (Level-2 communication feature). Expected category: NON-CONFORMING STANDARD. Module self-asserts (L65) TOTAL NUMBER OF FLAGS EXPECTED = 10. |

## group: removed-dbsg

### DB103M  _(emits CCVS report)_

| Construct | Category | Expected flag |
|---|---|---|
| L213-214: 'DECLARATIVES.' / 'START-UP SECTION.' followed by L214 'USE FOR DEBUGGING ON OPEN-FILES.' (also L222-223 'USE FOR DEBUGGING ON FALL-THROUGH-TEST PROC-SERIES-TEST.', L228 'USE FOR DEBUGGING ON GO-TO-TEST.', L233 'USE FOR DEBUGGING ON ALTERABLE-PARAGRAPH.', L238 'USE FOR DEBUGGING ON LOOP-ROUTINE.', L243 'USE FOR DEBUGGING ON DO-NOTHING-1.') | removed-after-85 | Each USE FOR DEBUGGING declarative is an obsolete/removed DEBUG-module element (the whole Debug module was deleted after COBOL-85). A conforming flagger should report each as OBSOLETE (and as removed in post-85 standards). |
| L218-220: 'MOVE DEBUG-LINE TO DBLINE-HOLD.' / 'MOVE DEBUG-NAME TO DBNAME-HOLD.' / 'MOVE DEBUG-CONTENTS TO DBCONT-HOLD.' | removed-after-85 | References to the special registers DEBUG-LINE / DEBUG-NAME / DEBUG-CONTENTS (DEBUG-ITEM subfields) are obsolete/removed DEBUG-module elements. Flag each register reference as OBSOLETE (removed after 85). |
| L466: 'ALTER ALTERABLE-PARAGRAPH TO PROCEED TO ALTERED-GO-TO-TEST.' | obsolete | The ALTER statement is an obsolete COBOL-85 element. Flag as OBSOLETE. |
| L321-329 (debug lines, col-7 'Y'): 'Y    IF RECORD-COUNT GREATER 50' ... and the D-indicator debug lines L617 'D    PERFORM FAIL.', L627-628 'D    PERFORM FAIL.' / 'D    SUBTRACT 1 FROM D.', L638-640, L649, L653, L666-668 (col-7 'D' debugging lines) | removed-after-85 | Column-7 'D' (and the test's 'Y') debugging-indicator lines are an obsolete DEBUG-module feature (debugging lines, compile-time switch). Flag the presence of debugging lines as OBSOLETE/removed-after-85. |

### DB301M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L33: 'USE FOR DEBUGGING ON ALL PROCEDURES.' (annotated L34 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | USE FOR DEBUGGING ... ON ALL PROCEDURES is a level-2 DEBUG-module feature; the module's own annotation requires the diagnostic NON-CONFORMING STANDARD. TOTAL NUMBER OF FLAGS EXPECTED = 1 (L45). |

### DB302M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L33: 'USE FOR DEBUGGING ON DB302M-CONTROL.' (annotated L34 '*Message expected for above statement: OBSOLETE') | obsolete | USE FOR DEBUGGING on a procedure-name is a level-1 obsolete DEBUG-module feature; module annotation requires diagnostic OBSOLETE. TOTAL NUMBER OF FLAGS EXPECTED = 1 (L44). |

### DB303M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L33: 'USE FOR DEBUGGING ON ALL REFERENCES OF FREC.' (annotated L34 '*Message expected for above statement: OBSOLETE') | obsolete | USE FOR DEBUGGING ON ALL REFERENCES OF data-name is a level-2 obsolete DEBUG-module feature; annotation requires OBSOLETE. |
| L41: 'USE FOR DEBUGGING ON TFIL.' (annotated L42 '*Message expected for above statement: OBSOLETE') | obsolete | USE FOR DEBUGGING on a file-name is an obsolete DEBUG-module feature; annotation requires OBSOLETE. TOTAL NUMBER OF FLAGS EXPECTED = 2 (L52). |

### DB304M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L28: 'USE FOR DEBUGGING ON COMMNAME.' (cd-name from L17 'CD COMMNAME FOR INPUT.'; annotated L29 '*Message expected for above statement: OBSOLETE') | obsolete | USE FOR DEBUGGING on a communication cd-name is an obsolete level-2 Communication/DEBUG feature (both Communication and Debug modules removed after 85); annotation requires OBSOLETE. TOTAL NUMBER OF FLAGS EXPECTED = 1 (L41). (Note: whole COMMUNICATION SECTION / CD is itself removed-after-85.) |

### DB305M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L33: 'USE FOR DEBUGGING ON ALL PROCEDURES.' (annotated L34 '*Message expected for above statement: OBSOLETE') | obsolete | USE FOR DEBUGGING ON ALL PROCEDURES treated here as a level-1 obsolete DEBUG-module feature; module annotation requires OBSOLETE (contrast DB301M which expects NON-CONFORMING STANDARD for the same syntax under level-1 flagging). TOTAL NUMBER OF FLAGS EXPECTED = 1 (L44). |

### SG302M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L16: 'BEANO SECTION 1.' (annotated L17 '*Message expected for above statement: OBSOLETE') | obsolete | A section with a segment-number (priority-number) — segmentation level 1 — is an obsolete COBOL-85 feature (Segmentation module removed after 85). Annotation requires OBSOLETE. TOTAL NUMBER OF FLAGS EXPECTED = 1 (L21). |

### SG303M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L11-12: 'XXXXX083' / 'SEGMENT-LIMIT IS 20.' in OBJECT-COMPUTER (annotated L13 '*Message expected for above statement: OBSOLETE') | obsolete | The SEGMENT-LIMIT clause is an obsolete COBOL-85 segmentation feature. Annotation requires OBSOLETE. |
| L17: 'NUMBER1 SECTION 18.' (annotated L18 '*Message expected for above statement: OBSOLETE') | obsolete | Section with segment-number 18 (independent/overlayable segment) — obsolete segmentation. Flag OBSOLETE. |
| L23: 'NUMBER2 SECTION 19.' (annotated L24 '*Message expected for above statement: OBSOLETE') | obsolete | Section with segment-number 19 — obsolete segmentation. Flag OBSOLETE. |
| L29: 'NUMBER3 SECTION 18.' (annotated L30 '*Message expected for above statement: OBSOLETE') | obsolete | Section with segment-number 18 — obsolete segmentation. Flag OBSOLETE. TOTAL NUMBER OF FLAGS EXPECTED = 4 (L37): SEGMENT-LIMIT + three numbered SECTIONs. |

### SG401M  _(flag-only (no report))_

| Construct | Category | Expected flag |
|---|---|---|
| L11-12: 'XXXXX083' / 'SEGMENT-LIMIT IS 18.' in OBJECT-COMPUTER (annotated L13 '*Message expected for above statement: NON-CONFORMING STANDARD') | non-conforming-extension | SEGMENT-LIMIT clause flagged at level-2 strictness as NON-CONFORMING STANDARD (per module annotation). (It is also obsolete; this module asserts the non-conforming-extension diagnostic.) |
| L29: 'NUMBER3 SECTION 18.' — second section bearing segment-number 18, not physically contiguous with the earlier 'NUMBER1 SECTION 18.' at L19 (annotated L30 '*Message expected for above statement: NON-CONFORMING STANDARD'; explanatory comment L35-37: 'A MESSAGE IS EXPECTED FOR THE EXISTENCE OF TWO SECTIONS WITH THE SAME SECTION NUMBER THAT ARE NOT PHYSICALLY CONTIGUOUS IN THE SOURCE PROGRAM') | non-conforming-extension | Two sections with the same segment-number 18 that are not physically contiguous in the source — flag NON-CONFORMING STANDARD. TOTAL NUMBER OF FLAGS EXPECTED = 2 (L39). (Note: the bare numbered SECTIONs at L19 NUMBER1 SECTION 18 and L24 NUMBER2 SECTION 19 are not separately annotated here; the 2 expected flags are the SEGMENT-LIMIT and the non-contiguous duplicate segment-number.) |

### OBNC1M  _(emits CCVS report)_

| Construct | Category | Expected flag |
|---|---|---|
| L48-83: comment-entry paragraph body of the REMARKS paragraph (program-abstract text containing pseudo source 'ENVIRONMENT DIVISION.' ... 'STOP RUN.' all in Area B, to be treated as documentation) — exercised by REMARKS-TEST at L507-513 ('COBOL REMARKS PARA'); also L638-640 lists comment-entry paragraphs AUTHOR, INSTALLATION, DATE-WRITTEN, SECURITY | obsolete | The REMARKS paragraph and the comment-entry (commentary) paragraphs AUTHOR / INSTALLATION / DATE-WRITTEN / SECURITY are obsolete level-1 elements (REMARKS removed after 85; the comment-entry paragraphs of the IDENTIFICATION DIVISION are obsolete). Flag REMARKS and the commentary paragraphs as OBSOLETE; the embedded Area-B 'code' must be ignored as documentation. |
| L93-95: OBJECT-COMPUTER 'MEMORY SIZE' / 'XXXXX067' / 'WORDS.' | obsolete | The MEMORY SIZE clause of OBJECT-COMPUTER is an obsolete COBOL-85 element. Flag as OBSOLETE. |
| L66: 'LABEL RECORDS OMITTED' (first FD) and L55-56 of the embedded-comment listing; the live FD at L102 'FD PRINT-FILE.' has none, but the phony-FD comment shows LABEL RECORDS | obsolete | The LABEL RECORDS clause is an obsolete COBOL-85 FD element. (Appears only inside the REMARKS comment-entry here, so primarily documents the obsolete clause; flag as OBSOLETE if processed.) |
| L516-531: NOTE-TEST-6 / NOTE-WRITE-6 — FEATURE 'NOTE RESERVED WORDS'; the NOTE statement examples are in comments (L518-523) but the test asserts NOTE-as-statement reserved-word handling | obsolete | The NOTE statement is an obsolete/removed COBOL-85 element (removed after 85). Flag any NOTE statement as OBSOLETE. |
| L532-550: NUM-INIT-1 / NUM-TEST-1 with numeric paragraph-names — L537 'ALTER 02 TO PROCEED TO 77.', L538 'GO TO 02.', L542 '02.', L543 'GO TO 50.', L544 '50. PERFORM FAIL.', L546 '77.' (FEATURE 'NUMERIC PARA-NAMES') | obsolete | All-numeric paragraph-names (e.g. 02, 50, 77) are obsolete in COBOL-85. Flag each numeric procedure-name as OBSOLETE. |
| L551-610: ALTER-TEST-1/2/3 — L555 'ALTER ALTER-A TO PROCEED TO ALTER-C.', L571 'ALTER ALTER-D TO ALTER-F.', L588 'ALTER ALTER-G TO PROCEED TO ALTER-I.', L604 'ALTER ALTER-G TO PROCEED TO ALTER-J.' (FEATURE 'ALTER') | obsolete | The ALTER statement (and the GO TO altered by it) is an obsolete COBOL-85 element. Flag each ALTER as OBSOLETE. |
| L612-631: GO--TEST-1 — L613 'ALTER GO--A TO PROCEED TO GO--C.' and L621 'GO--A.' / L621-622 'GO TO.' (unfinished/bare GO TO with no procedure-name, only valid after ALTER) (FEATURE 'UNFINISHED GO TO') | obsolete | A GO TO statement with no procedure-name (the 'unfinished' GO TO that must be ALTERed before reached) is part of the obsolete ALTER/GO-TO mechanism. Flag the alterable bare 'GO TO.' as OBSOLETE. |
| L641-778: SECT-NC180M-001 STOP-TEST-GF-1..9 — L646 'STOP "OPERATOR PLEASE EXECUTE RUN CONTINUATION".', L666 'STOP "A".', L679 'STOP "*".', L691 'STOP QUOTE.', L704 'STOP " * 5 *...".', L722 'STOP 7.', L734 'STOP 123456789987654321.', L746 'STOP ZERO.', L766 'STOP "OPERATOR KILL OBNC1".' (FEATURE 'STOP LITERAL') | obsolete | The STOP literal form (STOP statement with a literal operand) is an obsolete COBOL-85 element. Flag each 'STOP literal' as OBSOLETE. |

### OBNC2M  _(emits CCVS report)_

| Construct | Category | Expected flag |
|---|---|---|
| L25-28: 'DATE-COMPILED.' paragraph with comment-entry body (L26 '*THIS COMMENT LINE SHOULD NOT BE REPLACED' ...); verified live by DATE-TEST-1 at L870-874 (FEATURE/PAR-NAME 'DATE-COMPILED', RE-MARK 'COMMENT SHOULD BE DELETED') | obsolete | The DATE-COMPILED paragraph (a comment-entry paragraph of the IDENTIFICATION DIVISION) is an obsolete COBOL-85 element. Flag as OBSOLETE; its comment entry must be treated as documentation. |
| L593-612: GO--TEST-1 — L594 'ALTER GO--A TO PROCEED TO GO--C.' and L601-602 'GO--A.' / 'GO TO.' (bare GO TO, no procedure-name) (FEATURE 'UNFINISHED GO TO') | obsolete | A GO TO with no procedure-name (alterable/unfinished GO TO) is part of the obsolete ALTER mechanism. Flag the bare 'GO TO.' as OBSOLETE. |
| L613-617: ALTER-TEST-1 series ALTER — L614-616 'ALTER ALTER-A TO PROCEED TO ALTER-C / ALTER-D TO PROCEED TO ALTER-F / ALTER-F TO PROCEED TO ALTER-H.' (FEATURE 'SERIES ALTER') | obsolete | The ALTER statement with multiple {procedure TO PROCEED TO procedure} operands (series ALTER) is obsolete in COBOL-85. Flag as OBSOLETE. |
| L679-685: ALTER-TESTTT-2 — L681-683 'ALTER ALTER-A-2 TO PROCEED TO ALTER-B-2 / ALTER-TESTT-2 TO PROCEED TO ALTER-TESTT-2 / ALTER-D-2 TO PROCEED TO ALTER-E-2.' (FEATURE 'SERIES ALTER') | obsolete | Series ALTER (multiple operands, including self-referential ALTER) — obsolete. Flag as OBSOLETE. |
| L704-710: ALTER-TEST-3 — 11-operand series ALTER 'ALTER TEST-3A TO PROCEED TO TEST-3C TEST-4A TO TEST-4C ... TEST-13A TO TEST-13B.' (FEATURE label 'ALTER-TEST-3 THRU 13') | obsolete | Series ALTER with 11 operands and the optional 'PROCEED TO' omitted on some operands — obsolete ALTER. Flag as OBSOLETE. |
| L875-924: QUAL-SECTION-1 / QUAL-SECTION-2 — qualified ALTER/GO TO: L877-878 'ALTER PARA-5A IN QUAL-SECTION-1 TO PROCEED TO PARA-5C OF QUAL-SECTION-2.', L880 'GO TO PARA-5C OF QUAL-SECTION-1.', L895-896 'ALTER PARA-6B IN QUAL-SECTION-2 TO PROCEED TO PARA-6C OF QUAL-SECTION-1.' | obsolete | Qualified ALTER (paragraph qualified by section via IN/OF) and the qualified altered GO TO are part of the obsolete ALTER mechanism. Flag each ALTER as OBSOLETE. |


