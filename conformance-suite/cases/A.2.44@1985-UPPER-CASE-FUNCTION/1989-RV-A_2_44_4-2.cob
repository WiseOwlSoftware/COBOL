      *>--- conformance-case
      *> id: 1989-RV-A_2_44_4-2
      *> rules: [1989-RV-A.2.44.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.44.4 RV2 (THE UPPER-CASE FUNCTION). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UCLEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-IN   PIC X(3) VALUE "abc".
       01  WS-LEN  PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE WS-LEN =
               FUNCTION LENGTH (FUNCTION UPPER-CASE (WS-IN)).
           DISPLAY WS-LEN.
           DISPLAY "[" FUNCTION UPPER-CASE (WS-IN) "]".
           STOP RUN.
