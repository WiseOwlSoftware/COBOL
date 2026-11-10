      *>--- conformance-case
      *> id: 1985-GR-IX_2_8_3-1
      *> rules: [1985-GR-IX.2.8.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.8.3 GR1 (THE I-O-CONTROL PARAGRAPH). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RERUNIX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO IDXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IDX-KEY.
       I-O-CONTROL.
           RERUN ON RERUN-UNIT EVERY 100 RECORDS OF IDX-FILE.
       DATA DIVISION.
       FILE SECTION.
       FD  IDX-FILE.
       01  IDX-REC.
           05  IDX-KEY   PIC X(4).
           05  IDX-DATA  PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IDX-FILE.
           MOVE "K001" TO IDX-KEY.
           MOVE "RERUN OK" TO IDX-DATA.
           WRITE IDX-REC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IDX-FILE.
           OPEN INPUT IDX-FILE.
           READ IDX-FILE
               AT END DISPLAY "READ FAILED".
           DISPLAY IDX-DATA.
           CLOSE IDX-FILE.
           STOP RUN.
