      *>--- conformance-case
      *> id: g85_bf21_03
      *> rules: [1985-GR-VII.4.3.4-15]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR15 - OPEN EXTEND positions a sequential file
      *> immediately after its last logical record, the last record
      *> written in the file. ALPHA and BRAVO are written in output
      *> mode; the first OPEN EXTEND must place CHARLIE after BRAVO, and
      *> the second must place DELTA after CHARLIE, the record the
      *> previous extend wrote (not after BRAVO, the last record of the
      *> output run). An OPEN that positions at the beginning overwrites
      *> ALPHA; one that remembers the creating run loses CHARLIE.
      *> §1985:VII.1.3.5 - each successful OPEN EXTEND sets status 00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2103.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-F ASSIGN TO "BF2103.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-F.
       01  F-REC                PIC X(8).
       WORKING-STORAGE SECTION.
       01  WS-FS                PIC XX.
       01  WS-CNT               PIC 9 VALUE 0.
       01  WS-EOF               PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-F.
           MOVE "ALPHA" TO F-REC.
           WRITE F-REC.
           MOVE "BRAVO" TO F-REC.
           WRITE F-REC.
           CLOSE SEQ-F.
           OPEN EXTEND SEQ-F.
           DISPLAY "EXTEND 1 STATUS " WS-FS.
           MOVE "CHARLIE" TO F-REC.
           WRITE F-REC.
           CLOSE SEQ-F.
           OPEN EXTEND SEQ-F.
           DISPLAY "EXTEND 2 STATUS " WS-FS.
           MOVE "DELTA" TO F-REC.
           WRITE F-REC.
           CLOSE SEQ-F.
           OPEN INPUT SEQ-F.
           PERFORM READ-F UNTIL WS-EOF = "Y".
           CLOSE SEQ-F.
           STOP RUN.
       READ-F.
           READ SEQ-F
               AT END
                   MOVE "Y" TO WS-EOF
                   DISPLAY "END AFTER " WS-CNT
               NOT AT END
                   ADD 1 TO WS-CNT
                   DISPLAY "REC " WS-CNT " [" F-REC "]"
           END-READ.
