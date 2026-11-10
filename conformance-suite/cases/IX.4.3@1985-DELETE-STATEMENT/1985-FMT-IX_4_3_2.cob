      *>--- conformance-case
      *> id: 1985-FMT-IX_4_3_2
      *> rules: [1985-FMT-IX.4.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.3.2 (THE DELETE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DELTST.
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
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IDX-FILE.
           MOVE "0001" TO IDX-KEY.
           MOVE "ONE" TO IDX-DATA.
           WRITE IDX-REC INVALID KEY DISPLAY "W1 FAIL".
           MOVE "0002" TO IDX-KEY.
           MOVE "TWO" TO IDX-DATA.
           WRITE IDX-REC INVALID KEY DISPLAY "W2 FAIL".
           CLOSE IDX-FILE.
           OPEN I-O IDX-FILE.
           MOVE "0001" TO IDX-KEY.
           DELETE IDX-FILE RECORD
               INVALID KEY DISPLAY "DEL1 INVALID"
               NOT INVALID KEY DISPLAY "DEL1 OK"
           END-DELETE.
           MOVE "0003" TO IDX-KEY.
           DELETE IDX-FILE RECORD
               INVALID KEY DISPLAY "DEL3 INVALID"
               NOT INVALID KEY DISPLAY "DEL3 OK"
           END-DELETE.
           MOVE "0001" TO IDX-KEY.
           READ IDX-FILE
               INVALID KEY DISPLAY "READ1 NOT FOUND"
               NOT INVALID KEY DISPLAY "READ1 FOUND"
           END-READ.
           CLOSE IDX-FILE.
           STOP RUN.
