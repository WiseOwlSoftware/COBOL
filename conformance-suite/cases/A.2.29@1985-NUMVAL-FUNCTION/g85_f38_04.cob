      *>--- conformance-case
      *> id: g85_f38_04
      *> rules: [1989-RV-A.2.29.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.29.4 RV1 - NUMVAL returns the numeric value its
      *> argument represents, over each branch of §1989:A.2.29.3 AR1's
      *> two formats: digits alone, a leading sign with spaces around
      *> it, a fraction with no integer digits, a decimal point with
      *> no fraction digits, and the trailing +, -, CR and DB forms
      *> (CR, DB and - negative). Each value is shown through the
      *> edited picture -9(5).9(4): a lost sign, a misplaced decimal
      *> point or a dropped fraction changes a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3804.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-E         PIC -9(5).9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-E = FUNCTION NUMVAL ("123").
           DISPLAY "N1 " W-E.
           COMPUTE W-E = FUNCTION NUMVAL ("  - 12.5  ").
           DISPLAY "N2 " W-E.
           COMPUTE W-E = FUNCTION NUMVAL ("+3").
           DISPLAY "N3 " W-E.
           COMPUTE W-E = FUNCTION NUMVAL (".25").
           DISPLAY "N4 " W-E.
           COMPUTE W-E = FUNCTION NUMVAL ("7.").
           DISPLAY "N5 " W-E.
           COMPUTE W-E = FUNCTION NUMVAL ("4.75 CR").
           DISPLAY "N6 " W-E.
           COMPUTE W-E = FUNCTION NUMVAL (" 9DB ").
           DISPLAY "N7 " W-E.
           COMPUTE W-E = FUNCTION NUMVAL ("8 -").
           DISPLAY "N8 " W-E.
           COMPUTE W-E = FUNCTION NUMVAL ("6+").
           DISPLAY "N9 " W-E.
           STOP RUN.
