      *>--- conformance-case
      *> id: 1985-GR-VIII_3_2_4-3
      *> rules: [1985-GR-VIII.3.2.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.3.2.4 GR3 (THE FILE DESCRIPTION ENTRY). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WRONG
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELDR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO RELDISK
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE
           DATA RECORDS ARE RREC-1 RREC-2.
       01  RREC-1 PIC X(10).
       01  RREC-2.
           05  RREC-2-HEAD PIC X(4).
           05  RREC-2-TAIL PIC X(6).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RFILE.
           MOVE "ABCDEFGHIJ" TO RREC-1.
           WRITE RREC-1
               INVALID KEY DISPLAY "WRITE FAILED".
           MOVE "KLMN" TO RREC-2-HEAD.
           MOVE "OPQRST" TO RREC-2-TAIL.
           WRITE RREC-2
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE RFILE.
           OPEN INPUT RFILE.
           READ RFILE AT END DISPLAY "READ FAILED".
           DISPLAY RREC-1.
           READ RFILE AT END DISPLAY "READ FAILED".
           DISPLAY RREC-2-HEAD.
           DISPLAY RREC-2-TAIL.
           CLOSE RFILE.
           DISPLAY "DATA RECORDS OK".
           STOP RUN.
