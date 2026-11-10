      *>--- conformance-case
      *> id: 1985-GR-VI_5_9_5-6
      *> rules: [1985-GR-VI.5.9.5-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.5 GR6 (THE PICTURE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FIXINS.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  E1 PIC +999.
       01  E2 PIC -999.
       01  E3 PIC $999CR.
       01  E4 PIC $999DB.
       PROCEDURE DIVISION.
       P1.
           MOVE 12 TO E1.
           DISPLAY E1.
           MOVE -12 TO E1.
           DISPLAY E1.
           MOVE 5 TO E2.
           DISPLAY "[" E2 "]".
           MOVE -5 TO E2.
           DISPLAY "[" E2 "]".
           MOVE -7 TO E3.
           DISPLAY E3.
           MOVE 7 TO E3.
           DISPLAY "[" E3 "]".
           MOVE -7 TO E4.
           DISPLAY E4.
           STOP RUN.
