      *>--- conformance-case
      *> id: g85_f22_02
      *> rules: [1985-GR-VII.4.5.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.5.4 GR2 - after a successful READ, REWRITE
      *> logically replaces the record that READ accessed: the
      *> second READ reaches record 2, so record 2 (and only it)
      *> is replaced; records 1 and 3 keep their content. A leg
      *> fails if the wrong record is replaced, or REWRITE is not
      *> successful (status 00) after a successful READ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2202.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "G85F2202.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           LABEL RECORDS ARE STANDARD.
       01  R PIC X(6).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           MOVE "REC1-A" TO R.
           WRITE R.
           MOVE "REC2-A" TO R.
           WRITE R.
           MOVE "REC3-A" TO R.
           WRITE R.
           CLOSE F.
           OPEN I-O F.
           READ F AT END DISPLAY "EOF".
           READ F AT END DISPLAY "EOF".
           DISPLAY "READ " R.
           MOVE "REC2-X" TO R.
           REWRITE R.
           DISPLAY "REWRITE STATUS " FS.
           CLOSE F.
           OPEN INPUT F.
       P2.
           READ F AT END GO TO P3.
           DISPLAY R.
           GO TO P2.
       P3.
           CLOSE F.
           STOP RUN.
