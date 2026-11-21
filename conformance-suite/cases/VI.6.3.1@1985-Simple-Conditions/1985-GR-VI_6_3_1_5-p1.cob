      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_5-p1
      *> rules: [1985-GR-VI.6.3.1.5-p1, 1985-GR-VI.6.3.1.5-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.5 p1; §1985:VI.6.3.1.5 p2 (Simple Conditions).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-10.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S10SG.
      * 1985 VI.6.3.1.5: sign condition on identifiers and arithmetic
      * expressions, with and without NOT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  P        PIC S9(3)V9 VALUE 0.1.
       01  M        PIC S9(3) COMP VALUE -4.
       01  Z        PIC S9(3) VALUE -0.
       01  U        PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       P0.
           IF P IS POSITIVE DISPLAY "S1 T" ELSE DISPLAY "S1 F".
           IF P IS NEGATIVE DISPLAY "S2 T" ELSE DISPLAY "S2 F".
           IF M IS NEGATIVE DISPLAY "S3 T" ELSE DISPLAY "S3 F".
           IF Z IS ZERO DISPLAY "S4 T" ELSE DISPLAY "S4 F".
           IF Z IS POSITIVE DISPLAY "S5 T" ELSE DISPLAY "S5 F".
           IF Z IS NEGATIVE DISPLAY "S6 T" ELSE DISPLAY "S6 F".
           IF M IS NOT ZERO DISPLAY "S7 T" ELSE DISPLAY "S7 F".
           IF P IS NOT ZERO DISPLAY "S8 T" ELSE DISPLAY "S8 F".
           IF Z IS NOT POSITIVE DISPLAY "S9 T" ELSE DISPLAY "S9 F".
           IF M IS NOT NEGATIVE DISPLAY "SA T" ELSE DISPLAY "SA F".
           IF U IS ZERO DISPLAY "SB T" ELSE DISPLAY "SB F".
           IF M + 4 IS ZERO DISPLAY "E1 T" ELSE DISPLAY "E1 F".
           IF M * 2 + 9 IS POSITIVE DISPLAY "E2 T" ELSE DISPLAY "E2 F".
           IF P - 1 IS NEGATIVE DISPLAY "E3 T" ELSE DISPLAY "E3 F".
           IF (M - P) IS NOT NEGATIVE DISPLAY "E4 T"
               ELSE DISPLAY "E4 F".
           STOP RUN.
