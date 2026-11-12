      *>--- conformance-case
      *> id: pb5300-external-fd-clauses-agree
      *> rules: [1985-GR-VII.3.6.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5300. The agreement rules of an external connector's file
      *> description entries speak of the clauses the entries WRITE ("all LABEL
      *> RECORDS clauses in the run unit which are associated with this file
      *> connector", §1985:VII.3.6.3 GR4; likewise BLOCK CONTAINS, §1985:VII.3.3.3
      *> GR5, and VALUE OF, §1985:VII.3.9.4 GR2). MAIN and the program it calls
      *> write LABEL RECORDS STANDARD and BLOCK CONTAINS 5 both ways, MAIN
      *> writes VALUE OF FILE-ID "A" and the other entry writes none: they agree.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5300X6.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5300x6.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           LABEL RECORDS ARE STANDARD
           BLOCK CONTAINS 5 RECORDS
           VALUE OF FILE-ID "A".
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT SF
           MOVE "ABCD" TO SF-REC
           WRITE SF-REC
           CLOSE SF
           OPEN INPUT SF
           CALL "P5300X6S"
           CLOSE SF
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5300X6.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5300X6S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5300x6.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           LABEL RECORDS ARE STANDARD
           BLOCK CONTAINS 5 RECORDS.
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           READ SF AT END DISPLAY "EOF"
               NOT AT END DISPLAY "READ " SF-REC
           END-READ
           EXIT PROGRAM.
       END PROGRAM P5300X6S.
