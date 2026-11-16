      *>--- conformance-case
      *> id: g85_f02_05
      *> rules: [1985-GR-X.5.2.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.4 GR7 - G85F02FW leaves its file open across calls:
      *> the WRITE on the second entry finds it open (W 00) and the read
      *> back shows both records in order; after CANCEL the program is
      *> initial again, its file not open, so OPEN OUTPUT gives 00, not
      *> 41 (file already open). A wrong status or record fails a leg.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0205.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT INFL ASSIGN TO "G85F0205.DAT"
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
           MOVE "O" TO OP. CALL "G85F02FW" USING OP.
           MOVE "W" TO OP. CALL "G85F02FW" USING OP.
           MOVE "C" TO OP. CALL "G85F02FW" USING OP.
           OPEN INPUT INFL.
           READ INFL AT END MOVE "EOF " TO RR.
           DISPLAY "MAIN " RR.
           READ INFL AT END MOVE "EOF " TO RR.
           DISPLAY "MAIN " RR.
           CLOSE INFL.
           MOVE "O" TO OP. CALL "G85F02FW" USING OP.
           CANCEL "G85F02FW".
           MOVE "O" TO OP. CALL "G85F02FW" USING OP.
           MOVE "C" TO OP. CALL "G85F02FW" USING OP.
           STOP RUN.
       END PROGRAM G85F0205.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02FW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WF ASSIGN TO "G85F0205.DAT"
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
       P-FW.
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
       END PROGRAM G85F02FW.
