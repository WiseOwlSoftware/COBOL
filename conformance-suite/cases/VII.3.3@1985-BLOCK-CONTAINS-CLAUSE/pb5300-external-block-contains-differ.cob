      *>--- conformance-case
      *> id: pb5300-external-block-contains-differ
      *> rules: [1985-GR-VII.3.3.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5300. §1985:VII.3.3.3 GR5: "all BLOCK CONTAINS clauses in
      *> the run unit which are associated with that file connector must have
      *> the same value for integer-1 and integer-2". BLOCK CONTAINS 5 against
      *> BLOCK CONTAINS 6: refused (COBOL6847).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5300X2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5300x2.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           BLOCK CONTAINS 5 RECORDS.
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT SF
           MOVE "ABCD" TO SF-REC
           WRITE SF-REC
           CLOSE SF
           OPEN INPUT SF
           CALL "P5300X2S"
           CLOSE SF
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5300X2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5300X2S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5300x2.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           BLOCK CONTAINS 6 RECORDS.
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           READ SF AT END DISPLAY "EOF"
               NOT AT END DISPLAY "READ " SF-REC
           END-READ
           EXIT PROGRAM.
       END PROGRAM P5300X2S.
