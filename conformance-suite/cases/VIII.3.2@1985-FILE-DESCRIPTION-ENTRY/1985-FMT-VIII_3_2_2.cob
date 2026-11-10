      *>--- conformance-case
      *> id: 1985-FMT-VIII_3_2_2
      *> rules: [1985-FMT-VIII.3.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.3.2.2 (THE FILE DESCRIPTION ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELFD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO "RELFD"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE
           BLOCK CONTAINS 2 RECORDS
           RECORD CONTAINS 10 CHARACTERS
           LABEL RECORDS ARE STANDARD
           DATA RECORD IS RREC.
       01  RREC PIC X(10).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RFILE.
           MOVE "HELLO" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE RFILE.
           OPEN INPUT RFILE.
           MOVE SPACES TO RREC.
           READ RFILE AT END DISPLAY "EOF".
           DISPLAY RREC.
           CLOSE RFILE.
           STOP RUN.
