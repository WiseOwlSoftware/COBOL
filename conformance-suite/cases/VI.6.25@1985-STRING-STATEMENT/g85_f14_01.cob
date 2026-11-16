      *>--- conformance-case
      *> id: g85_f14_01
      *> rules: [1985-GR-VI.6.25.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.25.4 GR5 - the POINTER item 'must be set to an
      *> initial value greater than zero prior to the execution of the
      *> STRING statement'; set so, it names the first receiving
      *> position (§1985:VI.6.25.4 GR7: each character goes to the
      *> position the pointer designates, then the pointer is increased
      *> by one). L1: P = 3 puts AB at positions 3-4 of *****, P ends 5;
      *> it fails if the pointer is ignored (AB***) or reset. L2: P = 1
      *> fills all five, P ends 6, no overflow (§1985:VI.6.25.4 GR9 is
      *> tested only before each move). L3: P = 5 moves X to position 5,
      *> then P = 6 exceeds the size before Y: overflow, ****X, P 6.
      *> L4 contrasts GR5 with the value it forbids: P = 0 is less
      *> than one before the first move, so §1985:VI.6.25.4 GR9 gives
      *> overflow with no transfer and P stays 0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1401.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X  PIC X(5).
       01  P  PIC 99.
       PROCEDURE DIVISION.
       P1.
           MOVE ALL "*" TO X.
           MOVE 3 TO P.
           STRING "AB" DELIMITED BY SIZE INTO X WITH POINTER P
               ON OVERFLOW DISPLAY "L1 OVERFLOW"
               NOT ON OVERFLOW DISPLAY "L1 NO OVERFLOW"
           END-STRING.
           DISPLAY "L1 [" X "] " P.
           MOVE ALL "*" TO X.
           MOVE 1 TO P.
           STRING "ABCDE" DELIMITED BY SIZE INTO X WITH POINTER P
               ON OVERFLOW DISPLAY "L2 OVERFLOW"
               NOT ON OVERFLOW DISPLAY "L2 NO OVERFLOW"
           END-STRING.
           DISPLAY "L2 [" X "] " P.
           MOVE ALL "*" TO X.
           MOVE 5 TO P.
           STRING "XY" DELIMITED BY SIZE INTO X WITH POINTER P
               ON OVERFLOW DISPLAY "L3 OVERFLOW"
               NOT ON OVERFLOW DISPLAY "L3 NO OVERFLOW"
           END-STRING.
           DISPLAY "L3 [" X "] " P.
           MOVE ALL "*" TO X.
           MOVE 0 TO P.
           STRING "AB" DELIMITED BY SIZE INTO X WITH POINTER P
               ON OVERFLOW DISPLAY "L4 OVERFLOW"
               NOT ON OVERFLOW DISPLAY "L4 NO OVERFLOW"
           END-STRING.
           DISPLAY "L4 [" X "] " P.
           STOP RUN.
