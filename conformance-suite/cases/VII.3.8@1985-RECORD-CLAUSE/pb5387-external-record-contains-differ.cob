      *>--- conformance-case
      *> id: pb5387-external-record-contains-differ
      *> rules: [1985-GR-VII.3.8.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5387. §1985:VII.3.8.4 GR2: the file description entries
      *> associated with an external file connector "must specify the same
      *> values for integer-1". RECORD CONTAINS 4 against RECORD CONTAINS 6:
      *> refused (COBOL6847), as the BLOCK CONTAINS and LABEL RECORDS
      *> disagreements are.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5387A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5387a.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           RECORD CONTAINS 4 CHARACTERS.
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT SF
           MOVE "ABCD" TO SF-REC
           WRITE SF-REC
           CLOSE SF
           OPEN INPUT SF
           CALL "P5387AS"
           CLOSE SF
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5387A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5387AS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5387a.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           RECORD CONTAINS 6 CHARACTERS.
       01  SF-REC PIC X(6).
       PROCEDURE DIVISION.
       P.
           READ SF AT END DISPLAY "EOF"
               NOT AT END DISPLAY "READ " SF-REC
           END-READ
           EXIT PROGRAM.
       END PROGRAM P5387AS.
