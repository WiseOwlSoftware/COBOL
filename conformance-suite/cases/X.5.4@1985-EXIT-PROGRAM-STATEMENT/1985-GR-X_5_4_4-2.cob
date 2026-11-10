      *>--- conformance-case
      *> id: 1985-GR-X_5_4_4-2
      *> rules: [1985-GR-X.5.4.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.4.4 GR2 (THE EXIT PROGRAM STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-X PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "SUBP" USING WS-X.
           DISPLAY "BACK " WS-X.
           CALL "SUBP" USING WS-X.
           DISPLAY "BACK " WS-X.
           STOP RUN.
       END PROGRAM MAINP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-CNT PIC 9 VALUE 0.
       LINKAGE SECTION.
       01  L-X PIC 9.
       PROCEDURE DIVISION USING L-X.
       S-MAIN.
           ADD 1 TO W-CNT.
           MOVE W-CNT TO L-X.
           IF W-CNT = 1
               PERFORM S-P1
               DISPLAY "NOT REACHED".
       S-P1.
           DISPLAY "IN SUB " W-CNT.
           IF W-CNT = 1
               EXIT PROGRAM.
       S-P2.
           DISPLAY "PAST S-P1".
       S-P3.
           EXIT PROGRAM.
       END PROGRAM SUBP.
