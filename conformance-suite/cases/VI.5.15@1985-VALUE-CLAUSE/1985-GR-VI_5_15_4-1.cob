      *>--- conformance-case
      *> id: 1985-GR-VI_5_15_4-1
      *> rules: [1985-GR-VI.5.15.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.15.4 GR1 (THE VALUE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VALTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-JUST PIC X(5) JUSTIFIED RIGHT VALUE "AB".
       01  W-EDIT PIC ZZ9.99 VALUE " 12.50".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "[" W-JUST "]".
           DISPLAY "[" W-EDIT "]".
           STOP RUN.
