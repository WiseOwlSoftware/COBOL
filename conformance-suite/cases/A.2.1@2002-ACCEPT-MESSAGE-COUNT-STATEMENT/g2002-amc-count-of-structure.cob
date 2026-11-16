      *>--- conformance-case
      *> id: g2002-amc-count-of-structure
      *> rules: [2002-GR-A.2.1.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.2.1.3 GR1 - ACCEPT MESSAGE COUNT sets the message count
      *> "to indicate the number of complete messages that exist in the
      *> queue structure" the CD's four names designate. A holds sub-
      *> queues B (M1, M2) and C (M3): the count of A is 3, A/B 2, A/C 1
      *> A RECEIVE of A/B takes M1 whole (§2002:A.2.5.3 GR8b), so A/B is
      *> 1 and A is 2. Q1 holds ABCDEFGHIJ and SECND; a 5-character item
      *> takes the first in two portions (§2002:A.2.5.3 GR8c, GR10): the
      *> message still exists, complete, in the queue between them, so
      *> the count stays 2, then drops to 1 and to 0. END KEY 0 / 2 and
      *> TEXT LENGTH 5 are §2002:A.1.2.3 GR12a and GR11. A count of the
      *> named level only, of every message, of portions, or a count
      *> left stale fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2AMC01.
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
       01  BUF5 PIC X(5).
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
           MOVE "{""queues"":[{""name"":""A"",""subQueues"":[{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""B"",""messages"":[""M1"",""M2""]},{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""C"",""messages"":[""M3""]}]},{"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """name"":""Q1"",""messages"":[""ABCDEFGHIJ"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """SECND""]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "A" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE 999999 TO IN-CNT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "A   " IN-CNT.
           MOVE "B" TO IN-S1.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "A B " IN-CNT.
           MOVE "C" TO IN-S1.
           ACCEPT CIN COUNT.
           DISPLAY "A C " IN-CNT.
           MOVE "B" TO IN-S1.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA".
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "A B " IN-CNT.
           MOVE SPACES TO IN-S1.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "A   " IN-CNT.
           MOVE "Q1" TO IN-Q.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1  " IN-CNT.
           PERFORM RCV-ONE 3 TIMES.
           STOP RUN.
       RCV-ONE.
           MOVE ALL "*" TO BUF5.
           RECEIVE CIN MESSAGE INTO BUF5
               NO DATA DISPLAY "NO DATA"
               WITH DATA DISPLAY "RCV " BUF5 " " IN-END " " IN-LEN.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "Q1  " IN-CNT.
