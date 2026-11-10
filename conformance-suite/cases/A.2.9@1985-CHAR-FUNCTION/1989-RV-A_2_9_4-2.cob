      *>--- conformance-case
      *> id: 1989-RV-A_2_9_4-2
      *> rules: [1989-RV-A.2.9.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.9.4 RV2 (THE CHAR FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CHARTST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER.
       OBJECT-COMPUTER. ANY-COMPUTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-CH PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION CHAR (66) TO WS-CH.
           DISPLAY "DONE".
           STOP RUN.
