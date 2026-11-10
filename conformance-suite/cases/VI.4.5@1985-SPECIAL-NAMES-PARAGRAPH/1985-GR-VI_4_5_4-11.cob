      *>--- conformance-case
      *> id: 1985-GR-VI_4_5_4-11
      *> rules: [1985-GR-VI.4.5.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.5.4 GR11 (THE SPECIAL-NAMES PARAGRAPH). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CURTST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "L".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-AMT PIC L99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 5 TO WS-AMT.
           DISPLAY WS-AMT.
           STOP RUN.
