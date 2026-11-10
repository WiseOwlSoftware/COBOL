      *>--- conformance-case
      *> id: 1985-FMT-VIII_4_4_2
      *> rules: [1985-FMT-VIII.4.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.4.2 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELOPEN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO "RELOPN1"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
           SELECT QFILE ASSIGN TO "RELOPN2"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE.
       01  RREC PIC X(5).
       FD  QFILE.
       01  QREC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RFILE QFILE.
           MOVE "AAAAA" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "WRITE 1 FAILED".
           MOVE "BBBBB" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "WRITE 2 FAILED".
           MOVE "QQQQQ" TO QREC.
           WRITE QREC INVALID KEY DISPLAY "WRITE 3 FAILED".
           CLOSE RFILE QFILE.
           OPEN I-O RFILE INPUT QFILE.
           DISPLAY "OPEN I-O " WS-STAT.
           READ RFILE AT END DISPLAY "EOF 1".
           MOVE "CCCCC" TO RREC.
           REWRITE RREC.
           READ QFILE AT END DISPLAY "EOF 2".
           DISPLAY QREC.
           CLOSE RFILE QFILE.
           OPEN EXTEND RFILE.
           MOVE "DDDDD" TO RREC.
           WRITE RREC INVALID KEY DISPLAY "WRITE 4 FAILED".
           CLOSE RFILE.
           OPEN INPUT RFILE.
           PERFORM READ-PARA 4 TIMES.
           CLOSE RFILE.
           STOP RUN.
       READ-PARA.
           READ RFILE
               AT END DISPLAY "END"
               NOT AT END DISPLAY RREC
           END-READ.
