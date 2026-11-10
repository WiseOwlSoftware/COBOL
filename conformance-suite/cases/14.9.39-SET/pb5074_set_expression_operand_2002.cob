      *>--- conformance-case
      *> id: pb5074_set_expression_operand_2002
      *> rules: [FMT-14.9.39.2, GR-14.9.39.4-2, GR-14.9.39.4-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5074 - 14.9.39.2 prints arithmetic-expression-1 as
      *>   the Format 1 sender and arithmetic-expression-2 as the Format
      *>   2 amount. SET IX TO 1 + 1 selects occurrence 2 and SET IX UP
      *>   BY N with N PIC 9V9 holding 1.0 moves it to 3. Expected:
      *>   "B" then "C". It fails if either operand is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5074SE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T VALUE "ABC".
           05  E PIC X OCCURS 3 INDEXED BY IX.
       01  N PIC 9V9 VALUE 1.0.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX TO 1 + 1
           DISPLAY E (IX)
           SET IX UP BY N
           DISPLAY E (IX)
           STOP RUN.
