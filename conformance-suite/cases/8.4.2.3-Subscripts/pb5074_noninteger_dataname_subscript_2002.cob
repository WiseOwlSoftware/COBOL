      *>--- conformance-case
      *> id: pb5074_noninteger_dataname_subscript_2002
      *> rules: [FMT-8.4.2.3.2, GR-8.4.2.3.4-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5074 - 8.4.2.3.2 prints arithmetic-expression-1 as a
      *>   subscript, and an identifier of a numeric item is one
      *>   (8.8.1.1); GR1 b) takes the subscript from its evaluation,
      *>   so I PIC 9V9 holding 2.0 selects occurrence 2. Expected:
      *>   "B". It fails if the non-integer item is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5074SD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T VALUE "ABC".
           05  E PIC X OCCURS 3.
       01  I PIC 9V9 VALUE 2.0.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY E (I)
           STOP RUN.
