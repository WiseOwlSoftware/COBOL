      *>--- conformance-case
      *> id: g2002-amc-status-and-count
      *> rules: [2002-GR-A.2.1.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.2.1.3 GR2 - the CD area names at least the queue to be
      *> tested, and the statement updates STATUS KEY and MESSAGE COUNT.
      *> Table A.1 (§2002:A.1.2.4) gives ACCEPT MESSAGE COUNT two codes:
      *> 00, and 20 "One or more queues or subqueues unknown. No action
      *> taken." The count is updated only by the statement's action
      *> (§2002:A.1.2.3 GR14), so on 20 the count keeps 777777. Q1 holds
      *> M1 and M2; Q2 has the empty sub-queue S1. A status left XX, a
      *> stale count, a count changed on 20, or 00 for an unknown queue
      *> or sub-queue fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2AMC02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  BUF PIC X(10).
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT
           SYMBOLIC QUEUE IS IN-Q
           SYMBOLIC SUB-QUEUE-1 IS IN-S1
           SYMBOLIC SUB-QUEUE-2 IS IN-S2
           SYMBOLIC SUB-QUEUE-3 IS IN-S3
           MESSAGE DATE IS IN-DATE
           MESSAGE TIME IS IN-TIME
           SYMBOLIC SOURCE IS IN-SRC
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STAT
           MESSAGE COUNT IS IN-CNT.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""Q1"",""messages"":["
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """M1"",""M2""]},{""name"":""Q2"",""subQueues"":[{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""S1""}]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "Q1" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE "XX" TO IN-STAT.
           MOVE 999999 TO IN-CNT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1    " IN-STAT " " IN-CNT.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           MOVE "XX" TO IN-STAT.
           MOVE 999999 TO IN-CNT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1    " IN-STAT " " IN-CNT.
           MOVE "NOQ" TO IN-Q.
           MOVE "XX" TO IN-STAT.
           MOVE 777777 TO IN-CNT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "NOQ   " IN-STAT " " IN-CNT.
           MOVE "Q2" TO IN-Q.
           MOVE "SX" TO IN-S1.
           MOVE "XX" TO IN-STAT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q2 SX " IN-STAT " " IN-CNT.
           MOVE "S1" TO IN-S1.
           MOVE "XX" TO IN-STAT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q2 S1 " IN-STAT " " IN-CNT.
           STOP RUN.
