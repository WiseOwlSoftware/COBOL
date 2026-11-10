      *>--- conformance-case
      *> id: 1985-GR-IX_1_3_5-3
      *> rules: [1985-GR-IX.1.3.5-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.1.3.5 GR3 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IKTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO "IFILE"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IKEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE.
       01  IREC.
           05  IKEY  PIC X(4).
           05  IDAT  PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-STAT   PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON IFILE.
       ERR-PARA.
           DISPLAY "USE " WS-STAT.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT IFILE.
           MOVE "AAAA" TO IKEY.
           MOVE "FIRST" TO IDAT.
           WRITE IREC
               NOT INVALID KEY DISPLAY "WRITE 1 OK"
           END-WRITE.
           MOVE "AAAA" TO IKEY.
           MOVE "SECOND" TO IDAT.
           WRITE IREC
               NOT INVALID KEY DISPLAY "WRITE 2 OK"
           END-WRITE.
           DISPLAY "AFTER 2".
           CLOSE IFILE.
           STOP RUN.
