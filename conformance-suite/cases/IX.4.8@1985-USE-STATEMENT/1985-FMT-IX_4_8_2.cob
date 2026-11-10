      *>--- conformance-case
      *> id: 1985-FMT-IX_4_8_2
      *> rules: [1985-FMT-IX.4.8.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.8.2 (THE USE STATEMENT). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from the
      *> audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USETEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO IDXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IKEY
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE
           LABEL RECORDS ARE STANDARD.
       01  IREC.
           05 IKEY  PIC X.
           05 IDATA PIC X(9).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON IFILE.
       ERR-PARA.
           DISPLAY "USE " FS.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT IFILE.
           MOVE "A" TO IKEY.
           MOVE "DATA" TO IDATA.
           WRITE IREC.
           CLOSE IFILE.
           OPEN INPUT IFILE.
           MOVE "B" TO IKEY.
           READ IFILE.
           DISPLAY "AFTER " FS.
           CLOSE IFILE.
           STOP RUN.
