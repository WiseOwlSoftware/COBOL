      *>--- conformance-case
      *> id: 1985-GR-X_5_2_4-13
      *> rules: [1985-GR-X.5.2.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR13 (THE CALL STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X(5) VALUE "AAAAA".
       01  WS-B PIC X(5) VALUE "CCCCC".
       PROCEDURE DIVISION.
       P1.
           CALL "SUBPG" USING WS-A BY REFERENCE WS-B.
           DISPLAY WS-A.
           DISPLAY WS-B.
           STOP RUN.
       END PROGRAM MAINPG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPG.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-A PIC X(5).
       01  LK-B.
           05  LK-B1 PIC X(2).
           05  LK-B2 PIC X(3).
       PROCEDURE DIVISION USING LK-A LK-B.
       P1.
           DISPLAY LK-A.
           DISPLAY LK-B.
           MOVE "BBBBB" TO LK-A.
           MOVE "ZZ" TO LK-B1.
           EXIT PROGRAM.
       END PROGRAM SUBPG.
