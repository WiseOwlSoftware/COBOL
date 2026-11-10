      *>--- conformance-case
      *> id: 1985-GR-IX_4_5_4-11
      *> rules: [1985-GR-IX.4.5.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.4 GR11 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDGR11.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO IDXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IDX-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IDX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IDX-REC.
           05  IDX-KEY     PIC X.
           05  IDX-DATA    PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-STAT         PIC XX.
       01  WS-REC.
           05  WS-KEY      PIC X.
           05  WS-DATA     PIC X(5).
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON IDX-FILE.
       ERR-PARA.
           DISPLAY "USE " WS-STAT.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT IDX-FILE.
           MOVE "A" TO IDX-KEY.
           MOVE "ALPHA" TO IDX-DATA.
           WRITE IDX-REC INVALID KEY DISPLAY "WRITE A FAILED".
           MOVE "B" TO IDX-KEY.
           MOVE "BRAVO" TO IDX-DATA.
           WRITE IDX-REC INVALID KEY DISPLAY "WRITE B FAILED".
           MOVE "C" TO IDX-KEY.
           MOVE "CHARL" TO IDX-DATA.
           WRITE IDX-REC INVALID KEY DISPLAY "WRITE C FAILED".
           CLOSE IDX-FILE.
           OPEN INPUT IDX-FILE.
           MOVE SPACES TO WS-REC.
           MOVE "B" TO IDX-KEY.
           READ IDX-FILE INTO WS-REC
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "FOUND " WS-DATA " " WS-STAT
           END-READ.
           READ IDX-FILE NEXT RECORD
               AT END DISPLAY "AT END"
               NOT AT END DISPLAY "NEXT " IDX-DATA " " WS-STAT
           END-READ.
           CLOSE IDX-FILE.
           READ IDX-FILE NEXT RECORD
               AT END DISPLAY "AT END"
               NOT AT END DISPLAY "NOT AT END"
           END-READ.
           STOP RUN.
