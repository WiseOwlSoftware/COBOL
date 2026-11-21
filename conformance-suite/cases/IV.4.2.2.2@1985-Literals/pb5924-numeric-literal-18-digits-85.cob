      *>--- conformance-case
      *> id: pb5924-numeric-literal-18-digits-85
      *> rules: [1985-SR-IV.4.2.2.2.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.2.2 (cite.py: OK): "numeric literals of 1 through 18 digits in length"
      *> Numeric literals of 18 digits (integer, fraction, signed) keep
      *> every digit in MOVE, COMPUTE and a comparison.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NUML18.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N18               PIC 9(18).
       01  D18               PIC V9(18).
       01  D18-X REDEFINES D18 PIC X(18).
       01  S18               PIC S9(18) SIGN LEADING SEPARATE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 123456789012345678 TO N18.
           DISPLAY N18.
           MOVE .123456789012345678 TO D18.
           DISPLAY D18-X.
           MOVE -987654321098765432 TO S18.
           DISPLAY S18.
           COMPUTE N18 = 999999999999999999 - 1.
           DISPLAY N18.
           IF N18 < 999999999999999999
               DISPLAY "LT".
           MOVE 7 TO N18.
           DISPLAY N18.
           STOP RUN.
