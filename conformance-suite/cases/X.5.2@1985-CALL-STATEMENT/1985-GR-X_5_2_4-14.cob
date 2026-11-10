      *>--- conformance-case
      *> id: 1985-GR-X_5_2_4-14
      *> rules: [1985-GR-X.5.2.4-14]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR14 (THE CALL STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-VAL PIC X(5) VALUE "AAAAA".
       01  WS-TWO PIC X(5) VALUE "CCCCC".
       01  WS-REF PIC X(5) VALUE "RRRRR".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "SUBPROG" USING BY CONTENT WS-VAL WS-TWO
                                BY REFERENCE WS-REF.
           DISPLAY WS-VAL.
           DISPLAY WS-TWO.
           DISPLAY WS-REF.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPROG.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-VAL PIC X(5).
       01  LK-TWO PIC X(5).
       01  LK-REF PIC X(5).
       PROCEDURE DIVISION USING LK-VAL LK-TWO LK-REF.
       SUB-PARA.
           DISPLAY LK-VAL.
           DISPLAY LK-TWO.
           MOVE "BBBBB" TO LK-VAL.
           MOVE "DDDDD" TO LK-TWO.
           MOVE "SSSSS" TO LK-REF.
           DISPLAY LK-VAL.
           DISPLAY LK-TWO.
           EXIT PROGRAM.
       END PROGRAM SUBPROG.
       END PROGRAM MAINPROG.
