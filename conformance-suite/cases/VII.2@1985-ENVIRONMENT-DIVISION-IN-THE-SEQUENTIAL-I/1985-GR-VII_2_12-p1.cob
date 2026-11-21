      *>--- conformance-case
      *> id: 1985-GR-VII_2_12-p1
      *> rules: [1985-GR-VII.2.12-p1, 1985-GR-VII.3.5.1-p1, 1985-GR-VII.3.9.1-p1, 1985-GR-VII.4.4.1-p1, 1985-GR-VII.4.5.1-p1, 1985-GR-VII.4.6.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.12 p1; §1985:VII.3.5.1 p1; §1985:VII.3.9.1 p1;
      *> §1985:VII.4.4.1 p1; §1985:VII.4.5.1 p1; §1985:VII.4.6.1 p1
      *> (ENVIRONMENT DIVISION IN THE SEQUENTIAL I-O MODULE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-13.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-13 probe: 1985 Sequential I-O obsolete elements accepted at
      * --std 85 (RERUN, LABEL RECORDS, VALUE OF, DATA RECORDS) and the
      * FD, OPEN, WRITE, CLOSE, READ, REWRITE, USE functions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEQOBS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO "S13SEQOBS.DAT"
               FILE STATUS IS WS-STAT.
       I-O-CONTROL.
           RERUN ON SYSRR EVERY 100 RECORDS OF SFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  SFILE
           LABEL RECORDS ARE STANDARD
           VALUE OF FILE-ID IS "S13SEQOBS.DAT"
           DATA RECORD IS SREC.
       01  SREC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       01  WS-USE  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON SFILE.
       ERR-PARA.
           ADD 1 TO WS-USE.
           DISPLAY "USE RAN STATUS " WS-STAT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT SFILE.
           MOVE "AAAAAAAAAA" TO SREC.
           WRITE SREC.
           MOVE "BBBBBBBBBB" TO SREC.
           WRITE SREC.
           CLOSE SFILE.
           DISPLAY "WRITE+CLOSE " WS-STAT.
           OPEN I-O SFILE.
           READ SFILE AT END DISPLAY "UNEXPECTED END".
           DISPLAY "READ1 " SREC " " WS-STAT.
           MOVE "CCCCCCCCCC" TO SREC.
           REWRITE SREC.
           DISPLAY "REWRITE " WS-STAT.
           CLOSE SFILE.
           OPEN INPUT SFILE.
           READ SFILE AT END DISPLAY "UNEXPECTED END".
           DISPLAY "READ1 " SREC.
           READ SFILE AT END DISPLAY "UNEXPECTED END".
           DISPLAY "READ2 " SREC.
           READ SFILE AT END DISPLAY "AT END " WS-STAT.
           CLOSE SFILE.
           REWRITE SREC.
           DISPLAY "USE COUNT " WS-USE.
           STOP RUN.
