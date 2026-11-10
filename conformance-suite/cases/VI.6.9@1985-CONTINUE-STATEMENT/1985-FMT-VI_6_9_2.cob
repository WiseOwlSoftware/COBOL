      *>--- conformance-case
      *> id: 1985-FMT-VI_6_9_2
      *> rules: [1985-FMT-VI.6.9.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.9.2 (THE CONTINUE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CONTTEST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF WS-A = 1
               CONTINUE
           ELSE
               DISPLAY "ELSE TAKEN".
           CONTINUE.
           DISPLAY "DONE".
           STOP RUN.
