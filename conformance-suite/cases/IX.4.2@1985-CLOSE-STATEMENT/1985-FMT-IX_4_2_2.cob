      *>--- conformance-case
      *> id: 1985-FMT-IX_4_2_2
      *> rules: [1985-FMT-IX.4.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.2.2 (THE CLOSE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CLSTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO DISK1
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS K1.
           SELECT F2 ASSIGN TO DISK2
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS K2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1.
           05 K1 PIC X(4).
           05 D1 PIC X(3).
       FD  F2.
       01  R2.
           05 K2 PIC X(4).
           05 D2 PIC X(3).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1 F2.
           MOVE "AAAA" TO K1.
           MOVE "ONE" TO D1.
           WRITE R1 INVALID KEY DISPLAY "W1 ERR".
           MOVE "BBBB" TO K2.
           MOVE "TWO" TO D2.
           WRITE R2 INVALID KEY DISPLAY "W2 ERR".
           CLOSE F1 F2.
           OPEN INPUT F1 F2.
           READ F1 AT END DISPLAY "E1".
           READ F2 AT END DISPLAY "E2".
           DISPLAY D1.
           DISPLAY D2.
           CLOSE F1 F2.
           DISPLAY "DONE".
           STOP RUN.
