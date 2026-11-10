      *>--- conformance-case
      *> id: 1985-GR-IX_1_3_4-L2_2
      *> rules: [1985-GR-IX.1.3.4-L2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.1.3.4 GRL2.2 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ATENDT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO "IFILE"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IKEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE
           LABEL RECORDS ARE STANDARD.
       01  IREC.
           05  IKEY   PIC X(4).
           05  IDATA  PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-STAT    PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IFILE.
           MOVE "0001" TO IKEY.
           MOVE "DATA" TO IDATA.
           WRITE IREC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IFILE.
           OPEN INPUT IFILE.
           READ IFILE
               AT END DISPLAY "EARLY END".
           DISPLAY "FIRST " WS-STAT.
           READ IFILE
               AT END DISPLAY "AT END " WS-STAT.
           CLOSE IFILE.
           STOP RUN.
