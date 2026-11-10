      *>--- conformance-case
      *> id: 1985-SR-VI_5_9_3-3
      *> rules: [1985-SR-VI.5.9.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.3 SR3 (THE PICTURE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PICLOW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC x(5) VALUE "HELLO".
       01  WS-N PIC s9v9 VALUE -1.5.
       01  WS-E PIC z9.9cr.
       01  WS-B PIC xbx.
       01  WS-Z PIC zz9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE WS-N TO WS-E.
           MOVE "AB" TO WS-B.
           MOVE 7 TO WS-Z.
           DISPLAY WS-A.
           DISPLAY WS-E.
           DISPLAY WS-B.
           DISPLAY WS-Z.
           STOP RUN.
