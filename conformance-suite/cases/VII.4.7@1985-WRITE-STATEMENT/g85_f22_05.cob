      *>--- conformance-case
      *> id: g85_f22_05
      *> rules: [1985-GR-VII.4.7.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.7.4 GR7 - each WRITE releases a logical record
      *> to the operating system: the three records written (in that
      *> order) are what a later OPEN INPUT reads back, then end of
      *> file. A leg fails if a record is lost, altered, duplicated
      *> or reordered.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2205.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "G85F2205.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           LABEL RECORDS ARE STANDARD.
       01  R PIC X(5).
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           MOVE "ALPHA" TO R.
           WRITE R.
           MOVE "BRAVO" TO R.
           WRITE R.
           MOVE "DELTA" TO R.
           WRITE R.
           CLOSE F.
           OPEN INPUT F.
       P2.
           READ F AT END GO TO P3.
           ADD 1 TO N.
           DISPLAY N " " R.
           GO TO P2.
       P3.
           DISPLAY "COUNT " N.
           CLOSE F.
           STOP RUN.
