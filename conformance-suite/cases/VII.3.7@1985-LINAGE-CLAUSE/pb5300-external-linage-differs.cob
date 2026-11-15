      *>--- conformance-case
      *> id: pb5300-external-linage-differs
      *> rules: [1985-GR-VII.3.7.4-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5300. §1985:VII.3.7.4 GR11b: the file description entries
      *> associated with an external file connector must have "The same
      *> corresponding values for integer-1, integer-2, integer-3, and
      *> integer-4, if specified". LINAGE 5 against
      *> LINAGE 6: refused (COBOL6847).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5300X3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5300x3.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           LINAGE IS 5 LINES.
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT SF
           MOVE "ABCD" TO SF-REC
           WRITE SF-REC
           CLOSE SF
           OPEN INPUT SF
           CALL "P5300X3S"
           CLOSE SF
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5300X3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5300X3S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5300x3.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           LINAGE IS 6 LINES.
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           READ SF AT END DISPLAY "EOF"
               NOT AT END DISPLAY "READ " SF-REC
           END-READ
           EXIT PROGRAM.
       END PROGRAM P5300X3S.
