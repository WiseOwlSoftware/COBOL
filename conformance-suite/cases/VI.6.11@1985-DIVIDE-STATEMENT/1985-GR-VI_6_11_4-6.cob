      *>--- conformance-case
      *> id: 1985-GR-VI_6_11_4-6
      *> rules: [1985-GR-VI.6.11.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.11.4 GR6 (THE DIVIDE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DIVREM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-DIVIDEND PIC 99 VALUE 11.
       01  WS-DIVISOR  PIC 9  VALUE 3.
       01  WS-QUOT     PIC 9.
       01  WS-REM      PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DIVIDE WS-DIVISOR INTO WS-DIVIDEND
               GIVING WS-QUOT ROUNDED REMAINDER WS-REM.
           DISPLAY WS-QUOT.
           DISPLAY WS-REM.
           STOP RUN.
