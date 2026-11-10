      *>--- conformance-case
      *> id: 1985-GR-VI_5_7_4-2
      *> rules: [1985-GR-VI.5.7.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.7.4 GR2 (LEVEL-NUMBER). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from the
      *> audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LVLTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  WS-COUNT PIC 9 VALUE 5.
       01  WS-REC.
           05  WS-CODE PIC 9 VALUE 1.
               88  CODE-ONE VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF CODE-ONE DISPLAY "CODE-ONE TRUE".
           DISPLAY WS-COUNT.
           STOP RUN.
