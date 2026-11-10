      *>--- conformance-case
      *> id: 1985-GR-VI_5_9_4-8
      *> rules: [1985-GR-VI.5.9.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.4 GR8 (THE PICTURE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GRP-A.
           05  WS-A PIC 9PP.
       01  GRP-B.
           05  WS-B PIC 999.
       01  GRP-C.
           05  WS-C PIC PP9.
       01  GRP-D.
           05  WS-D PIC V999.
       PROCEDURE DIVISION.
       P1.
           MOVE 500 TO WS-A.
           DISPLAY GRP-A.
           MOVE WS-A TO WS-B.
           DISPLAY GRP-B.
           MOVE .005 TO WS-C.
           DISPLAY GRP-C.
           MOVE WS-C TO WS-D.
           DISPLAY GRP-D.
           IF WS-A = 500 DISPLAY "EQUAL".
           STOP RUN.
