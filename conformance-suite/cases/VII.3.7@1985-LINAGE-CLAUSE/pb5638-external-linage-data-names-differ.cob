      *>--- conformance-case
      *> id: pb5638-external-linage-data-names-differ
      *> rules: [1985-GR-VII.3.7.4-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5638 (its sweep). §1985:VII.3.7.4 GR11: the entries
      *> of an external file connector "must have: c. The same
      *> corresponding external data items referenced by data-name-1,
      *> data-name-2, data-name-3, and data-name-4." Both entries write
      *> LINAGE IS data-name-1 LINES, one naming LN1 and the other LN2:
      *> two items of the one external record X1, at character
      *> positions 1 and 3, so not the same external data item: refused
      *> (COBOL6847). Before the fix a data-name operand was undecided
      *> at compile time and this compiled.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5638L2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5638l2.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           LINAGE IS LN1 LINES.
       01  SF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  X1 IS EXTERNAL.
           05 LN1 PIC 99.
           05 LN2 PIC 99.
       PROCEDURE DIVISION.
       P.
           CALL "P5638L2S"
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5638L2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5638L2S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5638l2.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           LINAGE IS LN2 LINES.
       01  SF-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  X1 IS EXTERNAL.
           05 LN1 PIC 99.
           05 LN2 PIC 99.
       PROCEDURE DIVISION.
       P.
           EXIT PROGRAM.
       END PROGRAM P5638L2S.
