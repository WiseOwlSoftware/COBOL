      *>--- conformance-case
      *> id: pb4305_arithmetic_expression_subscript_2002
      *> rules: [FMT-8.4.2.3.2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - 8.4.2.3.2 prints arithmetic-expression-1 as a
      *>   subscript (2002 Annex F.2 item 133). I is 1, so E (I * 2) is
      *>   the second of "ABCD" and E (I + I + 1) the third. Expected:
      *>   "BC". It fails if an expression subscript is refused or
      *>   evaluated wrongly.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305SX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T VALUE "ABCD".
           05 E PIC X OCCURS 4.
       01 I PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY E (I * 2) E (I + I + 1)
           STOP RUN.
