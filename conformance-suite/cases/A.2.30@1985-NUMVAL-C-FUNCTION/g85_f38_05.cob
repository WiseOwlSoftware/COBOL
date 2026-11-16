      *>--- conformance-case
      *> id: g85_f38_05
      *> rules: [1989-RV-A.2.30.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.30.4 RV1 - NUMVAL-C returns the numeric value its
      *> argument represents, over §1989:A.2.30.3 AR1's two formats:
      *> the program's currency symbol $ when argument-2 is absent
      *> (§1989:A.2.30.3 AR5), a multi-character cs given by
      *> argument-2, commas before the decimal point (ignored per
      *> §1989:A.2.30.1), a leading sign, and the trailing CR and DB
      *> forms. Each value is shown through -9(5).9(4): a comma read
      *> as a decimal point, a lost sign or a currency string not
      *> skipped changes a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3805.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-E         PIC -9(5).9(4).
       01  W-CS        PIC X(3) VALUE "USD".
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-E = FUNCTION NUMVAL-C ("$1,234.56").
           DISPLAY "C1 " W-E.
           COMPUTE W-E = FUNCTION NUMVAL-C (" - $ 12.5").
           DISPLAY "C2 " W-E.
           COMPUTE W-E = FUNCTION NUMVAL-C ("USD 1,000" W-CS).
           DISPLAY "C3 " W-E.
           COMPUTE W-E = FUNCTION NUMVAL-C ("1,234.5CR").
           DISPLAY "C4 " W-E.
           COMPUTE W-E = FUNCTION NUMVAL-C ("$.75 DB").
           DISPLAY "C5 " W-E.
           COMPUTE W-E = FUNCTION NUMVAL-C ("  42  ").
           DISPLAY "C6 " W-E.
           STOP RUN.
