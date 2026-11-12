      *>--- conformance-case
      *> id: pb5300-external-value-of-differs
      *> rules: [1985-GR-VII.3.9.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5300. §1985:VII.3.9.4 GR2: "all VALUE OF clauses in the run
      *> unit which are associated with that file connector must be
      *> consistent. The implementor will specify the consistency rules"
      *> (docs/CONFORMANCE.md section 3.7: the same implementor-name is given
      *> the same literal). FILE-ID "A" against FILE-ID "B": refused (COBOL6847).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5300X4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5300x4.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           VALUE OF FILE-ID "A".
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT SF
           MOVE "ABCD" TO SF-REC
           WRITE SF-REC
           CLOSE SF
           OPEN INPUT SF
           CALL "P5300X4S"
           CLOSE SF
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5300X4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5300X4S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5300x4.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           VALUE OF FILE-ID "B".
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           READ SF AT END DISPLAY "EOF"
               NOT AT END DISPLAY "READ " SF-REC
           END-READ
           EXIT PROGRAM.
       END PROGRAM P5300X4S.
