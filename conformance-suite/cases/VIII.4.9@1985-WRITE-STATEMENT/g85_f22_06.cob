      *>--- conformance-case
      *> id: g85_f22_06
      *> rules: [1985-GR-VIII.4.9.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.9.4 GR7 - WRITE releases a logical record to
      *> the operating system: records written at relative record
      *> numbers 3 and 1 are read back by key and then in
      *> sequential order 1, 3. A leg fails if a record is lost or
      *> placed at the wrong relative record number.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2206.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "G85F2206.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           LABEL RECORDS ARE STANDARD.
       01  R PIC X(5).
       WORKING-STORAGE SECTION.
       01  RK PIC 9(4).
       01  FS PIC XX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           MOVE 3 TO RK.
           MOVE "THREE" TO R.
           WRITE R INVALID KEY DISPLAY "INVALID 3".
           DISPLAY "WRITE 3 " FS.
           MOVE 1 TO RK.
           MOVE "ONE" TO R.
           WRITE R INVALID KEY DISPLAY "INVALID 1".
           DISPLAY "WRITE 1 " FS.
           CLOSE F.
           OPEN INPUT F.
           MOVE 3 TO RK.
           READ F INVALID KEY DISPLAY "NO 3".
           DISPLAY "KEY 3 " R.
           MOVE 1 TO RK.
           READ F INVALID KEY DISPLAY "NO 1".
           DISPLAY "KEY 1 " R.
           CLOSE F.
           OPEN INPUT F.
       P2.
           READ F NEXT RECORD AT END GO TO P3.
           DISPLAY "SEQ " RK " " R.
           GO TO P2.
       P3.
           CLOSE F.
           STOP RUN.
