      *>--- conformance-case
      *> id: 1985-GR-VIII_4_2_4-2
      *> rules: [1985-GR-VIII.4.2.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.2.4 GR2 (THE CLOSE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELCLS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE ASSIGN TO RELFILE
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE.
       01  RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RFILE.
       ERR-PARA.
           DISPLAY "OPEN AFTER LOCK " WS-STAT.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT RFILE.
           MOVE "FIRST" TO RREC.
           WRITE RREC
               INVALID KEY DISPLAY "WRITE FAILED".
           MOVE "SECOND" TO RREC.
           WRITE RREC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE RFILE.
           DISPLAY "CLOSE OUTPUT " WS-STAT.
           OPEN INPUT RFILE.
           READ RFILE
               AT END DISPLAY "READ FAILED".
           DISPLAY RREC.
           CLOSE RFILE.
           DISPLAY "CLOSE INPUT " WS-STAT.
           OPEN I-O RFILE.
           DISPLAY "OPEN AFTER CLOSE " WS-STAT.
           CLOSE RFILE WITH LOCK.
           DISPLAY "CLOSE WITH LOCK " WS-STAT.
           OPEN INPUT RFILE.
           DISPLAY "USE PROCEDURE NOT EXECUTED".
           STOP RUN.
