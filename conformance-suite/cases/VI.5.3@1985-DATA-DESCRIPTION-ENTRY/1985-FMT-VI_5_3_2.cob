      *>--- conformance-case
      *> id: 1985-FMT-VI_5_3_2
      *> rules: [1985-FMT-VI.5.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.3.2 (THE DATA DESCRIPTION ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DDE1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A.
           05  FILLER PIC X(3) VALUE "ABC".
           05  B PIC 9(3) USAGE IS DISPLAY VALUE 123.
       01  C REDEFINES A PIC X(6).
       01  T.
           05  E PIC X OCCURS 3 TIMES.
       01  R.
           05  R1 PIC X(2) VALUE "PQ".
           05  R2 PIC 9 VALUE 5.
               88  R2-MID VALUES ARE 3 THRU 6 9.
           05  R3 PIC X(2) VALUE "RS".
           05  R4 PIC S9(3) SIGN IS LEADING SEPARATE CHARACTER.
           05  R5 PIC X(5) JUSTIFIED RIGHT.
           05  R6 PIC 9(3) BLANK WHEN ZERO.
       66  R23 RENAMES R2 THRU R3.
       PROCEDURE DIVISION.
       P1.
           MOVE "XYZ" TO T.
           DISPLAY C.
           DISPLAY E (2).
           DISPLAY R23.
           IF R2-MID DISPLAY "MID".
           MOVE -12 TO R4.
           DISPLAY R4.
           MOVE "AB" TO R5.
           DISPLAY "[" R5 "]".
           MOVE ZERO TO R6.
           DISPLAY "[" R6 "]".
           STOP RUN.
