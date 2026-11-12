      *>--- conformance-case
      *> id: pb5250-same-record-area-partly-in-sort-area-85
      *> rules: [1985-SR-XI.2.5.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5250 - §1985:XI.2.5.3 SR5c: F1, which is no sort file, is in a SAME clause (SAME RECORD AREA, whose
      *> format is SAME [ RECORD ] AREA, §1985:VII.2.13.2) and in a SAME SORT AREA clause, so "all of the files named
      *> in that SAME clause must be named in that SAME SORT AREA" clause; F2 is not. COBOL2738 at 85. From 2002
      *> only a file-area clause is asked (§2023:12.4.6.4.3 SR10): the twin pb5250_same_record_area_partly_in_sort_area
      *> runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5250X.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S1 ASSIGN TO "PB5250X.TMP".
           SELECT F1 ASSIGN TO "PB5250X1.DAT".
           SELECT F2 ASSIGN TO "PB5250X2.DAT".
       I-O-CONTROL.
           SAME RECORD AREA FOR F1 F2
           SAME SORT AREA FOR S1 F1.
       DATA DIVISION.
       FILE SECTION.
       SD  S1.
       01  RS1 PIC X(4).
       FD  F1.
       01  RF1 PIC X(4).
       FD  F2.
       01  RF2 PIC X(4).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "PB5250X OK".
           STOP RUN.
