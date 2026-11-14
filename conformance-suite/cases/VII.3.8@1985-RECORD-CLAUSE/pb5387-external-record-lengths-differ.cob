      *>--- conformance-case
      *> id: pb5387-external-record-lengths-differ
      *> rules: [1985-GR-VII.3.8.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5387. §1985:VII.3.8.4 GR2: "If the RECORD clause is not
      *> specified, all record description entries associated with this file
      *> connector must be the same length." Neither entry writes a RECORD
      *> clause; one describes a record of 4 character positions and the
      *> other a record of 6: refused (COBOL6847).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5387C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5387c.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL.
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT SF
           MOVE "ABCD" TO SF-REC
           WRITE SF-REC
           CLOSE SF
           OPEN INPUT SF
           CALL "P5387CS"
           CLOSE SF
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5387C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5387CS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5387c.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL.
       01  SF-REC PIC X(6).
       PROCEDURE DIVISION.
       P.
           READ SF AT END DISPLAY "EOF"
               NOT AT END DISPLAY "READ " SF-REC
           END-READ
           EXIT PROGRAM.
       END PROGRAM P5387CS.
