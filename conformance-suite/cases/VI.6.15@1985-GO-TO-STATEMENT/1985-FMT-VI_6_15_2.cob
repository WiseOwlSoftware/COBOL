      *>--- conformance-case
      *> id: 1985-FMT-VI_6_15_2
      *> rules: [1985-FMT-VI.6.15.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.15.2 (THE GO TO STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GOTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  IDX PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P0.
           GO TO P1 P2 P3 DEPENDING ON IDX.
           DISPLAY "FELL THROUGH".
           GO TO P9.
       P1.
           DISPLAY "P1".
           GO TO P9.
       P2.
           DISPLAY "P2".
           MOVE 4 TO IDX.
           GO TO P1 P2 P3 DEPENDING ON IDX.
           DISPLAY "NO TRANSFER".
           ALTER P4 TO PROCEED TO P5.
           GO TO P4.
       P3.
           DISPLAY "P3".
       P4.
           GO TO.
       P5.
           DISPLAY "P5".
       P9.
           STOP RUN.
