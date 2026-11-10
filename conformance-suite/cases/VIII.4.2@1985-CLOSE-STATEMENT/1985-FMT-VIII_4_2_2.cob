      *>--- conformance-case
      *> id: 1985-FMT-VIII_4_2_2
      *> rules: [1985-FMT-VIII.4.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.2.2 (THE CLOSE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CLOSETST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO "RELCL1"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
           SELECT QFILE ASSIGN TO "RELCL2"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-STAT2.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE.
       01  RREC PIC X(5).
       FD  QFILE.
       01  QREC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       01  WS-STAT2 PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RFILE.
           OPEN OUTPUT QFILE.
           MOVE "HELLO" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "WRITE 1 FAILED".
           MOVE "WORLD" TO QREC.
           WRITE QREC INVALID KEY DISPLAY "WRITE 2 FAILED".
           CLOSE RFILE QFILE.
           DISPLAY "CLOSE1 " WS-STAT " " WS-STAT2.
           OPEN INPUT RFILE.
           OPEN INPUT QFILE.
           READ RFILE AT END DISPLAY "EOF 1".
           DISPLAY RREC.
           READ QFILE AT END DISPLAY "EOF 2".
           DISPLAY QREC.
           CLOSE RFILE WITH LOCK QFILE.
           DISPLAY "CLOSE2 " WS-STAT " " WS-STAT2.
           STOP RUN.
