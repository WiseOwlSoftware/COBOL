      *>--- conformance-case
      *> id: 1985-GR-VI_6_23_4-7
      *> rules: [1985-GR-VI.6.23.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.23.4 GR7 (THE SET STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETTRUE.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-CODE PIC 9 VALUE 0.
           88  CODE-OK VALUE 5 3 7.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET CODE-OK TO TRUE.
           DISPLAY "CODE=" WS-CODE.
           STOP RUN.
