      *>--- conformance-case
      *> id: pb3861_set_index_to_integer_literal
      *> rules: [FMT-14.9.39.2]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3861 - the positive control of
      *> pb3861-set-index-to-alphanumeric-literal: ISO 14.9.39.2 Format 1
      *> sets an index-name from a numeric literal (arithmetic-expression-1
      *> at 2023, integer-1 in the earlier editions), so SET IX TO 3 sets
      *> IX to the third occurrence of TE. The VALUE clause gives TE the
      *> characters A, B, C and D, so TE (IX) is C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3861SI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB VALUE "ABCD".
          05 TE PIC X OCCURS 4 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET IX TO 3.
           DISPLAY TE (IX).
           STOP RUN.
