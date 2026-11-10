      *>--- conformance-case
      *> id: 1985-GR-VI_6_17_4-3
      *> rules: [1985-GR-VI.6.17.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.17.4 GR3 (THE INITIALIZE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INITFILL.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-GRP.
           05  WS-A        PIC X(3) VALUE "ABC".
           05  FILLER      PIC X(3) VALUE "XYZ".
           05  WS-N        PIC 9(3) VALUE 123.
       01  WS-IGRP.
           05  WS-X        USAGE INDEX.
           05  WS-C        PIC X    VALUE "Q".
       01  WS-TAB.
           05  WS-E        PIC X OCCURS 3 TIMES INDEXED BY WS-IX.
       01  WS-K            PIC 9    VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET WS-IX TO 2.
           SET WS-X TO WS-IX.
           INITIALIZE WS-GRP.
           INITIALIZE WS-IGRP.
           SET WS-IX TO 1.
           SET WS-IX TO WS-X.
           SET WS-K TO WS-IX.
           DISPLAY "[" WS-GRP "]".
           DISPLAY "[" WS-C "]".
           DISPLAY WS-K.
           STOP RUN.
