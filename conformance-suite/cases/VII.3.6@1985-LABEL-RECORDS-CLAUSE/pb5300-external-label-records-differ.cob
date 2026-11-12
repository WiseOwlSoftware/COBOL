      *>--- conformance-case
      *> id: pb5300-external-label-records-differ
      *> rules: [1985-GR-VII.3.6.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5300. §1985:VII.3.6.3 GR4: "all LABEL RECORDS clauses in
      *> the run unit which are associated with this file connector must
      *> have the same specification". MAIN says STANDARD and the program it
      *> calls says OMITTED for the one external connector: refused (COBOL6847).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5300X1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5300x1.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           LABEL RECORDS ARE STANDARD.
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT SF
           MOVE "ABCD" TO SF-REC
           WRITE SF-REC
           CLOSE SF
           OPEN INPUT SF
           CALL "P5300X1S"
           CLOSE SF
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5300X1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5300X1S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5300x1.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           LABEL RECORDS ARE OMITTED.
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           READ SF AT END DISPLAY "EOF"
               NOT AT END DISPLAY "READ " SF-REC
           END-READ
           EXIT PROGRAM.
       END PROGRAM P5300X1S.
