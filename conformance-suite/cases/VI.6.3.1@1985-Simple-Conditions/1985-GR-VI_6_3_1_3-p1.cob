      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_3-p1
      *> rules: [1985-GR-VI.6.3.1.3-p1, 1985-GR-VI.6.3.1.3-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.3 p1; §1985:VI.6.3.1.3 p2 (Simple Conditions).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-10.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S10CN.
      * 1985 VI.6.3.1.3: condition-name condition; ranges include
      * end values; comparison follows the relation-condition rules.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  V        PIC 99.
           88  LOWR VALUE 10 THRU 20.
           88  ODDS VALUE 1 3 5 7 9.
           88  MIXD VALUE 30 THRU 39 50 60 THRU 61.
       01  W        PIC X(4).
           88  ABC  VALUE "AB".
       01  S        PIC S99.
           88  SZ   VALUE ZERO.
           88  NEG  VALUE -9 THRU -1.
       PROCEDURE DIVISION.
       P0.
           MOVE 10 TO V. IF LOWR DISPLAY "R1 T" ELSE DISPLAY "R1 F".
           MOVE 20 TO V. IF LOWR DISPLAY "R2 T" ELSE DISPLAY "R2 F".
           MOVE 21 TO V. IF LOWR DISPLAY "R3 T" ELSE DISPLAY "R3 F".
           MOVE 09 TO V. IF LOWR DISPLAY "R4 T" ELSE DISPLAY "R4 F".
           MOVE 07 TO V. IF ODDS DISPLAY "V1 T" ELSE DISPLAY "V1 F".
           MOVE 08 TO V. IF ODDS DISPLAY "V2 T" ELSE DISPLAY "V2 F".
           MOVE 50 TO V. IF MIXD DISPLAY "M1 T" ELSE DISPLAY "M1 F".
           MOVE 61 TO V. IF MIXD DISPLAY "M2 T" ELSE DISPLAY "M2 F".
           MOVE 40 TO V. IF MIXD DISPLAY "M3 T" ELSE DISPLAY "M3 F".
           MOVE "AB" TO W. IF ABC DISPLAY "W1 T" ELSE DISPLAY "W1 F".
           MOVE "ABC" TO W. IF ABC DISPLAY "W2 T" ELSE DISPLAY "W2 F".
           MOVE -0 TO S. IF SZ DISPLAY "Z1 T" ELSE DISPLAY "Z1 F".
           MOVE -5 TO S. IF NEG DISPLAY "Z2 T" ELSE DISPLAY "Z2 F".
           MOVE -1 TO S. IF NEG DISPLAY "Z3 T" ELSE DISPLAY "Z3 F".
           MOVE 1 TO S. IF NEG DISPLAY "Z4 T" ELSE DISPLAY "Z4 F".
           IF NOT NEG DISPLAY "Z5 T" ELSE DISPLAY "Z5 F".
           STOP RUN.
