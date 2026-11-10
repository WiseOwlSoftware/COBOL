      *>--- conformance-case
      *> id: 1985-FMT-X_4_5_2
      *> rules: [1985-FMT-X.4.5.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.5.2 (THE EXTERNAL CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SHARED-ITEM IS EXTERNAL PIC X(5).
       PROCEDURE DIVISION.
       P1.
           MOVE "HELLO" TO SHARED-ITEM.
           CALL "SUBPROG".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SHARED-ITEM IS EXTERNAL PIC X(5).
       PROCEDURE DIVISION.
       P2.
           DISPLAY SHARED-ITEM.
           EXIT PROGRAM.
       END PROGRAM SUBPROG.
       END PROGRAM MAINPROG.
