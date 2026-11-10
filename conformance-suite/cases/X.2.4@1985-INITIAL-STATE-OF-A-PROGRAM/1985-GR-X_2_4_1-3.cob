      *>--- conformance-case
      *> id: 1985-GR-X_2_4_1-3
      *> rules: [1985-GR-X.2.4.1-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.4.1 GR3 (INITIAL STATE OF A PROGRAM). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPGM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-MODE PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       M-PARA.
           MOVE 1 TO W-MODE.
           CALL "SUBPGM" USING W-MODE.
           CANCEL "SUBPGM".
           MOVE 2 TO W-MODE.
           CALL "SUBPGM" USING W-MODE.
           DISPLAY "DONE".
           STOP RUN.
       END PROGRAM MAINPGM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPGM.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-MODE PIC 9.
       PROCEDURE DIVISION USING LK-MODE.
       S-MAIN.
           IF LK-MODE = 1
               PERFORM P1 THRU P2
               DISPLAY "RETURNED"
               GO TO P-END.
           GO TO P1.
       P1.
           DISPLAY "IN P1".
           IF LK-MODE = 1
               GO TO P-END.
       P2.
           DISPLAY "IN P2".
       P3.
           DISPLAY "IN P3".
       P-END.
           EXIT PROGRAM.
       END PROGRAM SUBPGM.
