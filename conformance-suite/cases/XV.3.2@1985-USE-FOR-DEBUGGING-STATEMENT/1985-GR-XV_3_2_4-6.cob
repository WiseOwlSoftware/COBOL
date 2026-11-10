      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-6
      *> rules: [1985-GR-XV.3.2.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR6 (THE USE FOR DEBUGGING STATEMENT). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGTST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANY-COMPUTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-X PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE FOR DEBUGGING ON W-X.
       D-PAR.
           DISPLAY "DEBUG " W-X.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       M-PAR.
           MOVE 5 TO W-X.
           DISPLAY "VALUE " W-X.
           STOP RUN.
