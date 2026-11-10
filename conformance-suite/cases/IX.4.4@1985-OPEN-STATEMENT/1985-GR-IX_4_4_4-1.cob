      *>--- conformance-case
      *> id: 1985-GR-IX_4_4_4-1
      *> rules: [1985-GR-IX.4.4.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.4.4 GR1 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPENIX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO IXDATA
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY.
           SELECT OPTIONAL OPT-FILE ASSIGN TO IXOPT
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS OPT-KEY.
           SELECT NO-FILE ASSIGN TO IXNONE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS NO-KEY
               FILE STATUS IS NO-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05  IX-KEY   PIC X(4).
           05  IX-DATA  PIC X(6).
       FD  OPT-FILE
           LABEL RECORDS ARE STANDARD.
       01  OPT-REC.
           05  OPT-KEY  PIC X(4).
           05  OPT-DATA PIC X(6).
       FD  NO-FILE
           LABEL RECORDS ARE STANDARD.
       01  NO-REC.
           05  NO-KEY   PIC X(4).
           05  NO-DATA  PIC X(6).
       WORKING-STORAGE SECTION.
       01  NO-STAT      PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       NO-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NO-FILE.
       NO-PARA.
           DISPLAY "UNSUCCESSFUL " NO-STAT.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT IX-FILE.
           MOVE "0001" TO IX-KEY.
           MOVE "RECORD" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           READ IX-FILE
               AT END DISPLAY "NO RECORDS"
               NOT AT END DISPLAY "RECORD FOUND".
           CLOSE IX-FILE.
           OPEN OUTPUT IX-FILE.
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           READ IX-FILE
               AT END DISPLAY "NO RECORDS"
               NOT AT END DISPLAY "RECORD FOUND".
           CLOSE IX-FILE.
           OPEN INPUT OPT-FILE.
           READ OPT-FILE
               AT END DISPLAY "OPTIONAL AT END"
               NOT AT END DISPLAY "OPTIONAL RECORD".
           CLOSE OPT-FILE.
           OPEN INPUT NO-FILE.
           STOP RUN.
