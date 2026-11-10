      *>--- conformance-case
      *> id: 1985-GR-VII_3_3_3-5
      *> rules: [1985-GR-VII.3.3.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.3.3 GR5 (THE BLOCK CONTAINS CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BLKMAIN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXTFILE ASSIGN TO "EXTF".
       DATA DIVISION.
       FILE SECTION.
       FD  EXTFILE IS EXTERNAL
           BLOCK CONTAINS 1 TO 4 RECORDS.
       01  EXT-REC PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT EXTFILE.
           CALL "BLKWRITE".
           CLOSE EXTFILE.
           OPEN INPUT EXTFILE.
           READ EXTFILE AT END DISPLAY "EOF".
           DISPLAY EXT-REC.
           CLOSE EXTFILE.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BLKWRITE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXTFILE ASSIGN TO "EXTF".
       DATA DIVISION.
       FILE SECTION.
       FD  EXTFILE IS EXTERNAL
           BLOCK CONTAINS 1 TO 4 RECORDS.
       01  EXT-REC PIC X(5).
       PROCEDURE DIVISION.
       WRITE-PARA.
           MOVE "HELLO" TO EXT-REC.
           WRITE EXT-REC.
           EXIT PROGRAM.
       END PROGRAM BLKWRITE.
       END PROGRAM BLKMAIN.
