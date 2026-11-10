      *>--- conformance-case
      *> id: 1985-SR-VI_6_18_3-3
      *> rules: [1985-SR-VI.6.18.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.3 SR3 (THE INSPECT STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSP85.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-X    PIC X(6) VALUE "A B C ".
       01  WS-N    PIC 99   VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT WS-X TALLYING WS-N FOR ALL SPACE.
           DISPLAY WS-N.
           STOP RUN.
