      *>--- conformance-case
      *> id: 1985-GR-VI_5_9_5-5
      *> rules: [1985-GR-VI.5.9.5-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.5 GR5 (THE PICTURE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EDTEST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC PICTURE 9(3)V99 VALUE 12.34.
       01  ED  PICTURE 999.99.
       01  ED2 PICTURE 99..
       PROCEDURE DIVISION.
       P1.
           MOVE SRC TO ED.
           MOVE SRC TO ED2.
           DISPLAY ED.
           DISPLAY ED2.
           STOP RUN.
