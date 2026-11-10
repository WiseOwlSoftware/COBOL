      *>--- conformance-case
      *> id: 1985-GR-X_5_2_4-7
      *> rules: [1985-GR-X.5.2.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR7 (THE CALL STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OP PIC 9.
       PROCEDURE DIVISION.
       P1.
           MOVE 1 TO OP CALL "SUBP" USING OP
           MOVE 2 TO OP CALL "SUBP" USING OP
           MOVE 3 TO OP CALL "SUBP" USING OP
           STOP RUN.
       END PROGRAM MAINP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "SUBPF".
       DATA DIVISION.
       FILE SECTION.
       FD F LABEL RECORDS ARE STANDARD.
       01 R PIC X.
       LINKAGE SECTION.
       01 OP PIC 9.
       PROCEDURE DIVISION USING OP.
       S1.
           IF OP = 1
               OPEN OUTPUT F
               MOVE "A" TO R WRITE R
               MOVE "B" TO R WRITE R
               CLOSE F
               OPEN INPUT F
           END-IF
           IF OP < 3
               READ F AT END DISPLAY "EOF" END-READ
               DISPLAY R
           END-IF
           IF OP = 3 CLOSE F END-IF.
       S2.
           EXIT PROGRAM.
       END PROGRAM SUBP.
