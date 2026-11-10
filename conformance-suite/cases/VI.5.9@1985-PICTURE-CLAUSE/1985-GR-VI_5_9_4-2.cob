      *>--- conformance-case
      *> id: 1985-GR-VI_5_9_4-2
      *> rules: [1985-GR-VI.5.9.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.4 GR2 (THE PICTURE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALPHA1.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-NAME PICTURE AAAAA VALUE "HELLO".
       PROCEDURE DIVISION.
       P1.
           DISPLAY W-NAME.
           MOVE "WORLD" TO W-NAME.
           DISPLAY W-NAME.
           STOP RUN.
