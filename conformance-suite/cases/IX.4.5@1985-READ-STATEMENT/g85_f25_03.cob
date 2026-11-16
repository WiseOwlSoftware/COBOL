      *>--- conformance-case
      *> id: g85_f25_03
      *> rules: [1985-GR-IX.4.5.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.4 GR5 - the record read is available before
      *> imperative-statement-2 (NOT AT END, leg A) and imperative-
      *> statement-4 (NOT INVALID KEY, leg C) execute, and, where
      *> neither is written, before the statement after the READ (legs
      *> B and D). The data is overwritten with XXXXXX before each
      *> random READ, so a leg fails if the imperative or the next
      *> statement sees the old content instead of the record read.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2503.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO F2503IX
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05 IX-KEY   PIC X(4).
           05 IX-DATA  PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       01  WS-EOF  PIC X VALUE "N".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT IX-FILE.
           MOVE "K001" TO IX-KEY. MOVE "ALPHA1" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "W-FAIL".
           MOVE "K002" TO IX-KEY. MOVE "BRAVO2" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "W-FAIL".
           MOVE "K003" TO IX-KEY. MOVE "CHARL3" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "W-FAIL".
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           READ IX-FILE NEXT RECORD
               AT END DISPLAY "A EOF"
               NOT AT END DISPLAY "A " IX-REC
           END-READ.
           READ IX-FILE NEXT RECORD AT END DISPLAY "B EOF" END-READ.
           DISPLAY "B " IX-REC.
           MOVE "K003" TO IX-KEY.
           MOVE "XXXXXX" TO IX-DATA.
           READ IX-FILE RECORD
               INVALID KEY DISPLAY "C IK"
               NOT INVALID KEY DISPLAY "C " IX-REC
           END-READ.
           MOVE "K001" TO IX-KEY.
           MOVE "XXXXXX" TO IX-DATA.
           READ IX-FILE RECORD INVALID KEY DISPLAY "D IK" END-READ.
           DISPLAY "D " IX-REC.
           CLOSE IX-FILE.
           STOP RUN.
