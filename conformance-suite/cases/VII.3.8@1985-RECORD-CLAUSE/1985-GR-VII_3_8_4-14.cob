      *>--- conformance-case
      *> id: 1985-GR-VII_3_8_4-14
      *> rules: [1985-GR-VII.3.8.4-14]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.8.4 GR14 (THE RECORD CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RECF3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISK1.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD
           RECORD CONTAINS 5 TO 12 CHARACTERS.
       01  R-SHORT PIC X(5).
       01  R-LONG.
           05 R-A PIC X(5).
           05 R-B PIC 9(7).
       WORKING-STORAGE SECTION.
       01  W-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "ABCDE" TO R-SHORT.
           WRITE R-SHORT.
           MOVE "FGHIJ" TO R-A.
           MOVE 1234567 TO R-B.
           WRITE R-LONG.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END MOVE "Y" TO W-EOF.
           IF W-EOF = "N" DISPLAY R-SHORT ELSE DISPLAY "EOF".
           READ F1 AT END MOVE "Y" TO W-EOF.
           IF W-EOF = "N" DISPLAY R-LONG ELSE DISPLAY "EOF".
           READ F1 AT END MOVE "Y" TO W-EOF.
           IF W-EOF = "Y" DISPLAY "END" ELSE DISPLAY "MORE".
           CLOSE F1.
           STOP RUN.
