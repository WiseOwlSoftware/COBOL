      *>--- conformance-case
      *> id: 1985-GR-IX_4_5_4-7
      *> rules: [1985-GR-IX.4.5.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.4 GR7 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDINTO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO IDXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS KEY-A.
       DATA DIVISION.
       FILE SECTION.
       FD  IDX-FILE
           LABEL RECORDS ARE STANDARD.
       01  REC-A.
           05 KEY-A  PIC X(4).
           05 DATA-A PIC X(6).
       01  REC-B.
           05 KEY-B  PIC X(4).
           05 DATA-B PIC 9(6).
       WORKING-STORAGE SECTION.
       01  WS-REC.
           05 WS-KEY  PIC X(4).
           05 WS-DATA PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IDX-FILE.
           MOVE "0001" TO KEY-A.
           MOVE "ALPHA1" TO DATA-A.
           WRITE REC-A INVALID KEY DISPLAY "BAD WRITE".
           MOVE "0002" TO KEY-A.
           MOVE "BRAVO2" TO DATA-A.
           WRITE REC-A INVALID KEY DISPLAY "BAD WRITE".
           CLOSE IDX-FILE.
           OPEN INPUT IDX-FILE.
           READ IDX-FILE RECORD INTO WS-REC
               AT END DISPLAY "EOF".
           DISPLAY WS-REC.
           READ IDX-FILE RECORD INTO WS-REC
               AT END DISPLAY "EOF".
           DISPLAY WS-REC.
           CLOSE IDX-FILE.
           STOP RUN.
