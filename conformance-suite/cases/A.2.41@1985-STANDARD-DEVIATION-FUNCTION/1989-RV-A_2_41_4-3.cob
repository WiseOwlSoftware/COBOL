      *>--- conformance-case
      *> id: 1989-RV-A_2_41_4-3
      *> rules: [1989-RV-A.2.41.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.41.4 RV3 (THE STANDARD-DEVIATION FUNCTION). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SDTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC 9(3) VALUE 42.
       01 WS-R PIC 9.99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE WS-R = FUNCTION STANDARD-DEVIATION (WS-X).
           DISPLAY WS-R.
           STOP RUN.
