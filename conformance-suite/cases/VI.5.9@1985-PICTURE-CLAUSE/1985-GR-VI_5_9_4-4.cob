      *>--- conformance-case
      *> id: 1985-GR-VI_5_9_4-4
      *> rules: [1985-GR-VI.5.9.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.4 GR4 (THE PICTURE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALNUMPIC.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-ITEM    PIC A9XX.
       01  W-OTHER   PIC 9X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "1A2B" TO W-ITEM.
           DISPLAY W-ITEM.
           MOVE "Z*" TO W-OTHER.
           DISPLAY W-OTHER.
           STOP RUN.
