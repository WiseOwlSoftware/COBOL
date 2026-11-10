      *>--- conformance-case
      *> id: 1985-SR-VI_6_26_3-1
      *> rules: [1985-SR-VI.6.26.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.26.3 SR1 (THE SUBTRACT STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 99 VALUE 10.
       01 B PIC 99 VALUE 3.
       01 C PIC Z9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SUBTRACT B FROM A GIVING C.
           DISPLAY C.
           STOP RUN.
