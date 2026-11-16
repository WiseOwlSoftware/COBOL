      *>--- conformance-case
      *> id: g85_f07_05
      *> rules: [1985-SR-VI.5.8.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.8.3 SR5 - "Where both integer-1 and integer-2 are
      *> used, integer-1 must be greater than or equal to zero and
      *> integer-2 must be greater than integer-1." Line 18 writes
      *> OCCURS 4 TO 4: integer-2 equals integer-1, so it is not
      *> greater. A refusal: a compiler that accepts line 18 fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0705.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-CNT  PIC 9 VALUE 0.
       01  WS-TBL.
           05  WS-EL  PIC X OCCURS 4 TO 4 TIMES DEPENDING ON WS-CNT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 3 TO WS-CNT.
           MOVE "P" TO WS-EL (1).
           MOVE "Q" TO WS-EL (2).
           MOVE "R" TO WS-EL (3).
           DISPLAY "[" WS-TBL "]".
           MOVE 1 TO WS-CNT.
           DISPLAY "[" WS-TBL "]".
           STOP RUN.
