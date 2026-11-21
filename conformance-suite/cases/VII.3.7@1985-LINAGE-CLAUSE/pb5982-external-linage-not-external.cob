      *>--- conformance-case
      *> id: pb5982-external-linage-not-external
      *> rules: [1985-GR-VII.3.7.4-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5982. §1985:VII.3.7.4 GR11c: the entries of an external
      *> file connector must have "The same corresponding external data
      *> items referenced by data-name-1, data-name-2, data-name-3, and
      *> data-name-4", so a LINAGE data-name of an external file names
      *> an external data item. LN here is a plain WORKING-STORAGE item,
      *> so the entry is refused (COBOL1624), as the PADDING CHARACTER
      *> item is (§1985:VII.2.7.4 GR6). The legal side is
      *> pb5638-external-linage-agree, whose item is external.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5982L1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFILE ASSIGN TO "p5982l1.dat".
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
           CLOSE PFILE
           DISPLAY "DONE"
           STOP RUN.
