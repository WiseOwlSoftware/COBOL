      *>--- conformance-case
      *> id: 1989-AR-A_2_29_3-2
      *> rules: [1989-AR-A.2.29.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.29.3 AR2 (THE NUMVAL FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NUMV18.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-NUM  PIC 9(18).
       01  W-ARG  PIC X(20) VALUE "  123456789012345678".
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-NUM = FUNCTION NUMVAL (W-ARG).
           DISPLAY W-NUM.
           STOP RUN.
