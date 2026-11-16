      *>--- conformance-case
      *> id: g85_f02_08
      *> rules: [1985-GR-X.5.3.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.3.4 GR10 - G85F02FC is cancelled with its file open:
      *> the implicit CLOSE leaves both records readable by the caller
      *> (MAIN AAAA, MAIN BBBB), the next OPEN OUTPUT in G85F02FC finds
      *> the file closed (O 00, not 41), and its USE procedure never
      *> runs (no USE line). A lost record, 41 or USE fails a leg.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0208.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT INFL ASSIGN TO "G85F0208.DAT"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  INFL.
       01  RR PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS  PIC XX.
       01  OP  PIC X.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE "O" TO OP. CALL "G85F02FC" USING OP.
           MOVE "W" TO OP. CALL "G85F02FC" USING OP.
           CANCEL "G85F02FC".
           DISPLAY "CANCELLED".
           OPEN INPUT INFL.
           DISPLAY "MAIN OPEN " FS.
           READ INFL AT END MOVE "EOF " TO RR.
           DISPLAY "MAIN " RR.
           READ INFL AT END MOVE "EOF " TO RR.
           DISPLAY "MAIN " RR.
           READ INFL AT END MOVE "EOF " TO RR.
           DISPLAY "MAIN " RR.
           CLOSE INFL.
           MOVE "O" TO OP. CALL "G85F02FC" USING OP.
           MOVE "C" TO OP. CALL "G85F02FC" USING OP.
           STOP RUN.
       END PROGRAM G85F0208.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02FC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WF ASSIGN TO "G85F0208.DAT"
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  WF.
       01  WR PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-FS PIC XX.
       LINKAGE SECTION.
       01  MODE-OP PIC X.
       PROCEDURE DIVISION USING MODE-OP.
       DECLARATIVES.
       D-WF SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON WF.
       D-WF-1.
           DISPLAY "USE " WS-FS.
       END DECLARATIVES.
       S-MAIN SECTION.
       P-FC.
           IF MODE-OP = "O"
               OPEN OUTPUT WF
               DISPLAY "O " WS-FS
               MOVE "AAAA" TO WR
               WRITE WR.
           IF MODE-OP = "W"
               MOVE "BBBB" TO WR
               WRITE WR
               DISPLAY "W " WS-FS.
           IF MODE-OP = "C"
               CLOSE WF
               DISPLAY "C " WS-FS.
           EXIT PROGRAM.
       END PROGRAM G85F02FC.
