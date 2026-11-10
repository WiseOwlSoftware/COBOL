      *>--- conformance-case
      *> id: 1985-GR-VI_6_15_4-3
      *> rules: [1985-GR-VI.6.15.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.15.4 GR3 (THE GO TO STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GOTODEP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       P-MAIN.
           GO TO P-ONE P-TWO DEPENDING ON WS-N.
           DISPLAY "NO TRANSFER 3".
           MOVE 0 TO WS-N.
           GO TO P-ONE P-TWO DEPENDING ON WS-N.
           DISPLAY "NO TRANSFER 0".
           MOVE 2 TO WS-N.
           GO TO P-ONE P-TWO DEPENDING ON WS-N.
           DISPLAY "WRONG".
           STOP RUN.
       P-ONE.
           DISPLAY "ONE".
           STOP RUN.
       P-TWO.
           DISPLAY "TWO".
           STOP RUN.
