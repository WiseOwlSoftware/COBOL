      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-5
      *> rules: [1985-GR-XV.3.2.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR5 (THE USE FOR DEBUGGING STATEMENT). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGTST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-A PIC 9 VALUE 0.
       01  W-B PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF W-A.
       DBG-1.
           DISPLAY "DEBUG " W-A.
       DBG-P SECTION.
           USE FOR DEBUGGING ON P3.
       DBG-2.
           DISPLAY "DEBUG P3".
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           MOVE 5 TO W-A.
           DISPLAY "AFTER SET".
           MOVE W-A TO W-B.
           DISPLAY "AFTER USE " W-B.
           MOVE 2 TO W-A.
           DISPLAY "BEFORE GO TO".
           GO TO P2 P3 DEPENDING ON W-A.
       P2.
           DISPLAY "IN P2".
           STOP RUN.
       P3.
           DISPLAY "IN P3".
           STOP RUN.
