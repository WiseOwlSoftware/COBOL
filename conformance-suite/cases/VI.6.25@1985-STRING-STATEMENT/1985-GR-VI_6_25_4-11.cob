      *>--- conformance-case
      *> id: 1985-GR-VI_6_25_4-11
      *> rules: [1985-GR-VI.6.25.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.25.4 GR11 (THE STRING STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STRTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T PIC X(2) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STRING "AB" DELIMITED BY SIZE INTO WS-T
               ON OVERFLOW DISPLAY "OVF"
           END-STRING
           DISPLAY WS-T
           DISPLAY "DONE"
           STOP RUN.
