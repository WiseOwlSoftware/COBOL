      *>--- conformance-case
      *> id: 1985-GR-VI_6_26_4-5
      *> rules: [1985-GR-VI.6.26.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.26.4 GR5 (THE SUBTRACT STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. C1SUB.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A1 PIC S9V99 VALUE 2.
       01  A2 PIC S9V99 VALUE 0.
       01  B1 PIC S99 VALUE -50.
       01  C1 PIC S99 VALUE 15.
       01  IX PIC 9 VALUE 3.
       01  TBL.
           05  T PIC 99 OCCURS 3 TIMES.
       01  G1.
           05  P PIC 99 VALUE 7.
           05  Q PIC 99 VALUE 60.
           05  S PIC 99 VALUE 5.
           05  R PIC X VALUE "X".
       01  G2.
           05  S PIC S99 VALUE 20.
           05  Q PIC S99 VALUE -50.
           05  P PIC S99 VALUE 30.
           05  R PIC X VALUE "Y".
       01  ED PIC -9.99.
       01  E2 PIC -99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SUBTRACT 1.255 FROM A1 ROUNDED A2 ROUNDED.
           MOVE A1 TO ED.
           DISPLAY "A1=" ED.
           MOVE A2 TO ED.
           DISPLAY "A2=" ED.
           SUBTRACT 2.5 1 FROM 10 GIVING E2 ROUNDED ED.
           DISPLAY "E2=" E2 " ED=" ED.
           SUBTRACT 60 FROM B1 C1
               ON SIZE ERROR DISPLAY "SIZE ERROR 1"
               NOT ON SIZE ERROR DISPLAY "NO SIZE ERROR 1"
           END-SUBTRACT.
           MOVE B1 TO E2.
           DISPLAY "B1=" E2.
           MOVE C1 TO E2.
           DISPLAY "C1=" E2.
           MOVE 10 TO T (1).
           MOVE 20 TO T (2).
           MOVE 30 TO T (3).
           SUBTRACT 1 FROM IX T (IX).
           DISPLAY "IX=" IX " T=" T (1) " " T (2) " " T (3).
           SUBTRACT CORRESPONDING G1 FROM G2
               ON SIZE ERROR DISPLAY "SIZE ERROR 2"
           END-SUBTRACT.
           MOVE P OF G2 TO E2.
           DISPLAY "P=" E2.
           MOVE Q OF G2 TO E2.
           DISPLAY "Q=" E2.
           MOVE S OF G2 TO E2.
           DISPLAY "S=" E2.
           DISPLAY "R=" R OF G2.
           IF IX = 2
               SUBTRACT 1 FROM C1
                   ON SIZE ERROR DISPLAY "SIZE ERROR 3"
               END-SUBTRACT
               DISPLAY "AFTER END-SUBTRACT"
           END-IF.
           MOVE C1 TO E2.
           DISPLAY "C1=" E2.
           STOP RUN.
