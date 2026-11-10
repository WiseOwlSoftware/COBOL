      *>--- conformance-case
      *> id: 1985-GR-X_2_4_2-1
      *> rules: [1985-GR-X.2.4.2-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.4.2 GR1 (INITIAL STATE OF A PROGRAM). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPGM.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "SUBPGM".
           STOP RUN.
       END PROGRAM MAINPGM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPGM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-FLAG PIC X(7) VALUE "INITIAL".
       PROCEDURE DIVISION.
       SUB-PARA.
           DISPLAY W-FLAG.
           MOVE "CHANGED" TO W-FLAG.
           EXIT PROGRAM.
       END PROGRAM SUBPGM.
