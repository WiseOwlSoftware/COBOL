      *>--- conformance-case
      *> id: g2_class_truth_values
      *> rules: [2002-GR-8.8.4.1.3.3-2, 2002-GR-8.8.4.1.3.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.4.1.3.3 GR2 - the truth value of each class phrase
      *> over a non-zero-length item, and GR3 - "If the word NOT is
      *> specified, the truth value is reversed."  (cite.py OK: GR2a,
      *> GR2b, GR2c, GR2d, GR2e, GR2f, GR2g, GR3.)
      *> No CHARACTER CLASSIFICATION clause: the classification is
      *> the coded character set's (§2002:12.2.5.3 GR6, cite.py OK),
      *> so no locale is in effect and the "2." arms of b) c) d)
      *> apply. Each line is one leg; its expected value:
      *> a) NUMERIC
      *>  1.a display numeric: sign in agreement with the description
      *>      and the rest digits 0-9.
      *>   A1 U3 PIC 9(3) "123"                                    T
      *>   A2 S3 PIC S9(3) VALUE -45 (signed, sign present)        T
      *>   A3 RU PIC 9(3) REDEFINES "1 3": a space is no digit      F
      *>   A4 LS PIC S9(3) SIGN LEADING SEPARATE over "+123"       T
      *>   A5 UL PIC 9(4) over the same "+123": unsigned, so the
      *>      "+" is neither an agreeing sign nor a digit           F
      *>   A6 LB, the LS layout over "1234": no valid separate
      *>      sign (13.16.50)                                       F
      *>  1.b usage not display: a valid representation for the
      *>      usage (and within the PICTURE's range)
      *>   A7 BINARY PIC S9(4) VALUE -12                            T
      *>   A8 PACKED-DECIMAL PIC 9(3) VALUE 123                     T
      *>   A9 FLOAT-LONG VALUE 12.5                                 T
      *>   A14 BINARY PIC 9(4) over X"FFFF" (65535 in either byte
      *>       order): outside the PICTURE's range 0-9999            F
      *>   A15 PACKED-DECIMAL PIC 9(3) over X"ABCD": nibbles A-C
      *>       are no decimal digits, so no valid representation     F
      *>  2. category not numeric: content entirely digits 0-9
      *>   A10 X(3) "123" T   A11 X(3) "12 " F
      *>   A12 numeric-edited ZZ9 holding "  7" (spaces)            F
      *>   A13 N(3) N"123"                                          T
      *> b) 2. ALPHABETIC: upper and/or lower letters and space
      *>   B1 "ABC " T  B2 "abc " T  B3 "Ab c" T  B4 "    " T
      *>   B5 "AB1 " F
      *> c) 2. ALPHABETIC-LOWER: lower letters and space
      *>   C1 "ab c" T  C2 "aBc " F
      *> d) 2. ALPHABETIC-UPPER: upper letters and space
      *>   D1 "AB C" T  D2 "AbC " F
      *> e) BOOLEAN: the boolean values 0 and 1 only
      *>   E1 PIC 1(4) B"1010" T  E2 X(4) "0120" F
      *> f) alphabet-name: characters of the coded character set
      *>   F1 "AB12" IS G2STD (STANDARD-1)                         T
      *>   F2 X"41804243" IS G2STD: X"80" has no counterpart in the
      *>      7-bit ISO/IEC 646 IRV (§2002:12.2.6.3 GR7c, cite.py OK) F
      *> g) class-name: only the characters of the CLASS definition
      *>   G1 "1F0A" IS G2HEX T  G2 "1G0A" IS G2HEX F
      *> GR3 NOT reverses: N1 "12 " NOT NUMERIC T, N2 "ABC " NOT
      *>   ALPHABETIC F, N3 "AB1 " NOT ALPHABETIC T, N4 "0120" NOT
      *>   BOOLEAN T, N5 "1G0A" NOT G2HEX T, N6 "AB C" NOT
      *>   ALPHABETIC-UPPER F, N7 U3 NOT NUMERIC F.
      *> A leg fails if a phrase tests the wrong character set (A5:
      *> a sign accepted on an unsigned item; B4/C1: space refused;
      *> A12: a numeric-edited image read as its value; A14/A15/F2:
      *> a class test folded to constant TRUE, kb/Work PB1466) or
      *> NOT is not the exact reversal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CLT01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET G2STD IS STANDARD-1
           CLASS G2HEX IS "0" THRU "9" "A" THRU "F".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  U3 PIC 9(3) VALUE 123.
       01  S3 PIC S9(3) VALUE -45.
       01  RA PIC X(3) VALUE "1 3".
       01  RU REDEFINES RA PIC 9(3).
       01  SA PIC X(4) VALUE "+123".
       01  LS REDEFINES SA PIC S9(3) SIGN LEADING SEPARATE.
       01  UL REDEFINES SA PIC 9(4).
       01  SB PIC X(4) VALUE "1234".
       01  LB REDEFINES SB PIC S9(3) SIGN LEADING SEPARATE.
       01  BN PIC S9(4) USAGE BINARY VALUE -12.
       01  PK PIC 9(3) USAGE PACKED-DECIMAL VALUE 123.
       01  FL USAGE FLOAT-LONG VALUE 12.5.
       01  X3 PIC X(3).
       01  NE PIC ZZ9.
       01  N3 PIC N(3) VALUE N"123".
       01  X4 PIC X(4).
       01  BB PIC 1(4) VALUE B"1010".
       01  GB.
           05  GBN PIC 9(4) USAGE BINARY.
       01  GP.
           05  GPK PIC 9(3) USAGE PACKED-DECIMAL.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF U3 IS NUMERIC DISPLAY "A1=T" ELSE DISPLAY "A1=F" END-IF
           IF S3 IS NUMERIC DISPLAY "A2=T" ELSE DISPLAY "A2=F" END-IF
           IF RU IS NUMERIC DISPLAY "A3=T" ELSE DISPLAY "A3=F" END-IF
           IF LS IS NUMERIC DISPLAY "A4=T" ELSE DISPLAY "A4=F" END-IF
           IF UL IS NUMERIC DISPLAY "A5=T" ELSE DISPLAY "A5=F" END-IF
           IF LB IS NUMERIC DISPLAY "A6=T" ELSE DISPLAY "A6=F" END-IF
           IF BN IS NUMERIC DISPLAY "A7=T" ELSE DISPLAY "A7=F" END-IF
           IF PK IS NUMERIC DISPLAY "A8=T" ELSE DISPLAY "A8=F" END-IF
           IF FL IS NUMERIC DISPLAY "A9=T" ELSE DISPLAY "A9=F" END-IF
           MOVE "123" TO X3
           IF X3 IS NUMERIC DISPLAY "A10=T" ELSE DISPLAY "A10=F"
           END-IF
           MOVE "12 " TO X3
           IF X3 IS NUMERIC DISPLAY "A11=T" ELSE DISPLAY "A11=F"
           END-IF
           MOVE 7 TO NE
           IF NE IS NUMERIC DISPLAY "A12=T" ELSE DISPLAY "A12=F"
           END-IF
           IF N3 IS NUMERIC DISPLAY "A13=T" ELSE DISPLAY "A13=F"
           END-IF
           MOVE X"FFFF" TO GB
           IF GBN IS NUMERIC DISPLAY "A14=T" ELSE DISPLAY "A14=F"
           END-IF
           MOVE X"ABCD" TO GP
           IF GPK IS NUMERIC DISPLAY "A15=T" ELSE DISPLAY "A15=F"
           END-IF
           MOVE "ABC " TO X4
           IF X4 IS ALPHABETIC DISPLAY "B1=T" ELSE DISPLAY "B1=F"
           END-IF
           IF X4 IS NOT ALPHABETIC DISPLAY "N2=T" ELSE DISPLAY "N2=F"
           END-IF
           MOVE "abc " TO X4
           IF X4 IS ALPHABETIC DISPLAY "B2=T" ELSE DISPLAY "B2=F"
           END-IF
           MOVE "Ab c" TO X4
           IF X4 IS ALPHABETIC DISPLAY "B3=T" ELSE DISPLAY "B3=F"
           END-IF
           MOVE SPACES TO X4
           IF X4 IS ALPHABETIC DISPLAY "B4=T" ELSE DISPLAY "B4=F"
           END-IF
           MOVE "AB1 " TO X4
           IF X4 IS ALPHABETIC DISPLAY "B5=T" ELSE DISPLAY "B5=F"
           END-IF
           IF X4 IS NOT ALPHABETIC DISPLAY "N3=T" ELSE DISPLAY "N3=F"
           END-IF
           MOVE "ab c" TO X4
           IF X4 IS ALPHABETIC-LOWER
               DISPLAY "C1=T" ELSE DISPLAY "C1=F" END-IF
           MOVE "aBc " TO X4
           IF X4 IS ALPHABETIC-LOWER
               DISPLAY "C2=T" ELSE DISPLAY "C2=F" END-IF
           MOVE "AB C" TO X4
           IF X4 IS ALPHABETIC-UPPER
               DISPLAY "D1=T" ELSE DISPLAY "D1=F" END-IF
           IF X4 IS NOT ALPHABETIC-UPPER
               DISPLAY "N6=T" ELSE DISPLAY "N6=F" END-IF
           MOVE "AbC " TO X4
           IF X4 IS ALPHABETIC-UPPER
               DISPLAY "D2=T" ELSE DISPLAY "D2=F" END-IF
           IF BB IS BOOLEAN DISPLAY "E1=T" ELSE DISPLAY "E1=F" END-IF
           MOVE "0120" TO X4
           IF X4 IS BOOLEAN DISPLAY "E2=T" ELSE DISPLAY "E2=F" END-IF
           IF X4 IS NOT BOOLEAN DISPLAY "N4=T" ELSE DISPLAY "N4=F"
           END-IF
           MOVE "AB12" TO X4
           IF X4 IS G2STD DISPLAY "F1=T" ELSE DISPLAY "F1=F" END-IF
           MOVE X"41804243" TO X4
           IF X4 IS G2STD DISPLAY "F2=T" ELSE DISPLAY "F2=F" END-IF
           MOVE "1F0A" TO X4
           IF X4 IS G2HEX DISPLAY "G1=T" ELSE DISPLAY "G1=F" END-IF
           MOVE "1G0A" TO X4
           IF X4 IS G2HEX DISPLAY "G2=T" ELSE DISPLAY "G2=F" END-IF
           IF X4 IS NOT G2HEX DISPLAY "N5=T" ELSE DISPLAY "N5=F"
           END-IF
           MOVE "12 " TO X3
           IF X3 IS NOT NUMERIC DISPLAY "N1=T" ELSE DISPLAY "N1=F"
           END-IF
           IF U3 IS NOT NUMERIC DISPLAY "N7=T" ELSE DISPLAY "N7=F"
           END-IF
           STOP RUN.
