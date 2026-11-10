      *>--- conformance-case
      *> id: 1985-GR-VI_6_21_4-1
      *> rules: [1985-GR-VI.6.21.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.21.4 GR1 (THE PERFORM STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PERFBY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  I       PIC 9 VALUE 0.
       77  J       PIC 9 VALUE 0.
       77  STEP-I  PIC 9 VALUE 1.
       77  STEP-J  PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM SHOW-PARA
               VARYING I FROM 1 BY STEP-I UNTIL I > 2
               AFTER J FROM 1 BY STEP-J UNTIL J > 2.
           STOP RUN.
       SHOW-PARA.
           DISPLAY "I=" I " J=" J.
