      *>--- conformance-case
      *> id: pb5982-external-linage-two-programs-not-external
      *> rules: [1985-GR-VII.3.7.4-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5982. §1985:VII.3.7.4 GR11c: two programs describe one
      *> external file connector and each names a different plain
      *> WORKING-STORAGE item (LN, LM) as its LINAGE data-name-1. Neither
      *> is an external data item, so the same corresponding external
      *> data item cannot be referenced by both: each entry is refused
      *> (COBOL1624).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5982L2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFILE ASSIGN TO "p5982l2.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  PFILE IS EXTERNAL
           LINAGE IS LN LINES.
       01  PR PIC X(20).
       WORKING-STORAGE SECTION.
       01  LN PIC 99 VALUE 10.
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT PFILE
           MOVE "HELLO" TO PR
           WRITE PR
           CALL "P5982L2S"
           CLOSE PFILE
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5982L2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5982L2S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFILE ASSIGN TO "p5982l2.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  PFILE IS EXTERNAL
           LINAGE IS LM LINES.
       01  PR PIC X(20).
       WORKING-STORAGE SECTION.
       01  LM PIC 99 VALUE 20.
       PROCEDURE DIVISION.
       P.
           MOVE "WORLD" TO PR
           WRITE PR
           EXIT PROGRAM.
       END PROGRAM P5982L2S.
