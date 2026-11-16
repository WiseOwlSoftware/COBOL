      *>--- conformance-case
      *> id: g85_f02_09
      *> rules: [1985-GR-X.5.4.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.4.4 GR3 - EXIT PROGRAM in INITIAL G85F02IX acts as
      *> CANCEL "G85F02IX": the file it leaves open is implicitly closed
      *> (the caller opens it, MAIN OPEN 00, and reads IIII), the next
      *> OPEN OUTPUT finds it closed (O 00, not 41), and its contained
      *> G85F02IY is cancelled too (Y1 on each call). A lost record, 41
      *> or Y2 fails a leg.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0209.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT INFL ASSIGN TO "G85F0209.DAT"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  INFL.
       01  RR PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS  PIC XX.
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "G85F02IX".
           OPEN INPUT INFL.
           DISPLAY "MAIN OPEN " FS.
           READ INFL AT END MOVE "EOF " TO RR.
           DISPLAY "MAIN " RR.
           READ INFL AT END MOVE "EOF " TO RR.
           DISPLAY "MAIN " RR.
           CLOSE INFL.
           CALL "G85F02IX".
           STOP RUN.
       END PROGRAM G85F0209.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02IX IS INITIAL PROGRAM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WF ASSIGN TO "G85F0209.DAT"
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  WF.
       01  WR PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-FS PIC XX.
       PROCEDURE DIVISION.
       P-IX.
           OPEN OUTPUT WF.
           DISPLAY "O " WS-FS.
           MOVE "IIII" TO WR.
           WRITE WR.
           CALL "G85F02IY".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02IY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-IY.
           ADD 1 TO N.
           DISPLAY "Y" N.
           EXIT PROGRAM.
       END PROGRAM G85F02IY.
       END PROGRAM G85F02IX.
