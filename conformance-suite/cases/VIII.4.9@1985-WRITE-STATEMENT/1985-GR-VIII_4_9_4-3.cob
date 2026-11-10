      *>--- conformance-case
      *> id: 1985-GR-VIII_4_9_4-3
      *> rules: [1985-GR-VIII.4.9.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.9.4 GR3 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WRFROM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO RELFILE
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE
           LABEL RECORDS ARE STANDARD.
       01  RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WSRC PIC X(5) VALUE "ABCDE".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RFILE.
           MOVE "ZZZZZZZZZZ" TO RREC.
           WRITE RREC FROM WSRC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE RFILE.
           OPEN INPUT RFILE.
           READ RFILE AT END DISPLAY "EOF".
           DISPLAY "[" RREC "]".
           CLOSE RFILE.
           STOP RUN.
