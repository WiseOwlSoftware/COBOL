      *>--- conformance-case
      *> id: pb5300-external-code-set-differs
      *> rules: [1985-GR-VII.3.4.4-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5300. §1985:VII.3.4.4 GR3: "all CODE-SET clauses in the run
      *> unit which are associated with that file connector must have the
      *> same character set". STANDARD-1 against the native set: refused
      *> (COBOL6847).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5300X5.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL1 IS STANDARD-1
           ALPHABET AL2 IS NATIVE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5300x5.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           CODE-SET IS AL1.
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT SF
           MOVE "ABCD" TO SF-REC
           WRITE SF-REC
           CLOSE SF
           OPEN INPUT SF
           CALL "P5300X5S"
           CLOSE SF
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5300X5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5300X5S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL1 IS STANDARD-1
           ALPHABET AL2 IS NATIVE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5300x5.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           CODE-SET IS AL2.
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           READ SF AT END DISPLAY "EOF"
               NOT AT END DISPLAY "READ " SF-REC
           END-READ
           EXIT PROGRAM.
       END PROGRAM P5300X5S.
