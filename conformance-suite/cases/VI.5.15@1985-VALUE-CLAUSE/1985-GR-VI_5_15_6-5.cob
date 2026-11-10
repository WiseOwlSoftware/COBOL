      *>--- conformance-case
      *> id: 1985-GR-VI_5_15_6-5
      *> rules: [1985-GR-VI.5.15.6-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.15.6 GR5 (THE VALUE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VALODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  N PIC 9 VALUE 1.
       01  G VALUE "ABCDE".
           05  E PIC X OCCURS 1 TO 5 TIMES DEPENDING ON N.
       PROCEDURE DIVISION.
       P1.
           DISPLAY G.
           MOVE 5 TO N.
           DISPLAY G.
           STOP RUN.
