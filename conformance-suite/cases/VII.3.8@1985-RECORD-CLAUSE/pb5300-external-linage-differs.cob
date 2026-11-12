      *>--- conformance-case
      *> id: pb5300-external-linage-differs
      *> rules: [1985-GR-VII.3.8.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5300. §1985:VII.3.8.4 GR2: the file description entries
      *> associated with an external file connector "must specify the same
      *> values for integer-1 or integer-2 and integer-3". LINAGE 5 against
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
