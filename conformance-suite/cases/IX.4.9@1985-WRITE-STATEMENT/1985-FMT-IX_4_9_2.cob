      *>--- conformance-case
      *> id: 1985-FMT-IX_4_9_2
      *> rules: [1985-FMT-IX.4.9.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.9.2 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WRTIDX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO IDXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IDX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IDX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IDX-REC.
           05 IDX-KEY  PIC X(4).
           05 IDX-DATA PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-REC.
           05 WS-KEY  PIC X(4) VALUE "0001".
           05 WS-DATA PIC X(10) VALUE "FIRST".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IDX-FILE.
           WRITE IDX-REC FROM WS-REC
               INVALID KEY DISPLAY "INVALID 1"
               NOT INVALID KEY DISPLAY "WRITTEN 1"
           END-WRITE.
           WRITE IDX-REC FROM WS-REC
               INVALID KEY DISPLAY "INVALID 2"
               NOT INVALID KEY DISPLAY "WRITTEN 2"
           END-WRITE.
           CLOSE IDX-FILE.
           STOP RUN.
