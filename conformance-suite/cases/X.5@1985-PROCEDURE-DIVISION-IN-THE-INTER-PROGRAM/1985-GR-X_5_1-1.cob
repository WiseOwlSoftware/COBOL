      *>--- conformance-case
      *> id: 1985-GR-X_5_1-1
      *> rules: [1985-GR-X.5.1-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.1 GR1 (PROCEDURE DIVISION IN THE INTER-PROGRAM
      *> COMMUNICATION MODULE). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from the audit's
      *> draft, confirmed by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-ITEM PIC X(5) VALUE "AAAAA".
       PROCEDURE DIVISION.
       P1.
           CALL "SUBPROG" USING BY CONTENT WS-ITEM.
           DISPLAY WS-ITEM.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPROG.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-ITEM PIC X(5).
       PROCEDURE DIVISION USING LK-ITEM.
       P2.
           DISPLAY LK-ITEM.
           MOVE "BBBBB" TO LK-ITEM.
           DISPLAY LK-ITEM.
           EXIT PROGRAM.
       END PROGRAM SUBPROG.
       END PROGRAM MAINPROG.
