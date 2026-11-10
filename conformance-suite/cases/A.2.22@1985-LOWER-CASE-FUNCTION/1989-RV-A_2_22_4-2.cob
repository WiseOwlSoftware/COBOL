      *>--- conformance-case
      *> id: 1989-RV-A_2_22_4-2
      *> rules: [1989-RV-A.2.22.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.22.4 RV2 (THE LOWER-CASE FUNCTION). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LCTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC PIC X(5) VALUE "HELLO".
       01 TGT PIC X(5) VALUE SPACES.
       01 N   PIC 99 VALUE ZERO.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE N = FUNCTION LENGTH (FUNCTION LOWER-CASE (SRC)).
           DISPLAY N.
           MOVE FUNCTION LOWER-CASE (SRC) TO TGT.
           DISPLAY "[" TGT "]".
           STOP RUN.
