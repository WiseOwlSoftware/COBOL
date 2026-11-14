      *>--- conformance-case
      *> id: pb5387-external-record-own-lengths-differ
      *> rules: [1985-GR-VII.3.8.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5387. §1985:VII.3.8.4 GR2: "If the RECORD clause is not
      *> specified, all record description entries associated with this file
      *> connector must be the same length." The one external entry writes
      *> no RECORD clause and describes records of 4 and 6 character
      *> positions: refused (COBOL6847), though no other entry exists.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5387D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5387d.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL.
       01  SF-REC PIC X(4).
       01  SF-R2 PIC X(6).
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT SF
           MOVE "ABCD" TO SF-REC
           WRITE SF-REC
           CLOSE SF
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5387D.
