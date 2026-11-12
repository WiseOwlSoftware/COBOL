      *>--- conformance-case
      *> id: pb5250_same_record_area_partly_in_sort_area
      *> rules: [SR-12.4.6.4.3-10]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5250 - the twin of pb5250-same-record-area-partly-in-sort-area-85. §2023:12.4.6.4.3 SR10 asks only
      *> a FILE-AREA format SAME clause to lie inside the sort-merge-area clauses that name one of its files, so a
      *> record-area clause that does not is legal from 2002 (§2002:12.3.6.2 SR10); §1985:XI.2.5.3 SR5c asked it at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5250Y.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S1 ASSIGN TO "PB5250Y.TMP".
           SELECT F1 ASSIGN TO "PB5250Y1.DAT".
           SELECT F2 ASSIGN TO "PB5250Y2.DAT".
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
           DISPLAY "PB5250Y OK".
           STOP RUN.
