      *>--- conformance-case
      *> id: pb5412_from_part_of_the_record_area
      *> rules: [GR-14.9.51.4-5]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5412 - the LEGAL side of 1985:VII.4.7.3 SR1 ("Record-
      *>   name-1 and identifier-1 must not reference the same storage
      *>   area"): each FROM operand below names PART of the record area
      *>   or a storage the compiler cannot place as the whole of it, so
      *>   none is "the same storage area" and every WRITE compiles at
      *>   every edition: a slice shorter than the record (1:5), a slice
      *>   starting past its first position (2:), a slice whose length
      *>   is a run-time quantity (1:L), one occurrence of a table of
      *>   two, an alias of a part (66 without and with THROUGH), and a
      *>   Working-Storage item. The refused side is the six
      *>   pb5412-* negatives.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5412P1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5412P1.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1.
           05 R1-A PIC X(4).
           05 R1-B PIC X(3).
           05 R1-C PIC X(3).
       66  R1-HALF RENAMES R1-A.
       66  R1-PART RENAMES R1-A THRU R1-B.
       01  R1-T.
           05 R1-E PIC X(5) OCCURS 2 TIMES.
       WORKING-STORAGE SECTION.
       01  L PIC 99 VALUE 10.
       01  W PIC X(10) VALUE "ABCDEFGHIJ".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE W TO R1.
           WRITE R1 FROM R1 (1:5).
           WRITE R1 FROM R1 (2:).
           WRITE R1 FROM R1 (1:L).
           WRITE R1 FROM R1-E (1).
           WRITE R1 FROM R1-HALF.
           WRITE R1 FROM R1-PART.
           WRITE R1 FROM W.
           CLOSE F1.
           DISPLAY "WRITE FROM PART OK".
           STOP RUN.
