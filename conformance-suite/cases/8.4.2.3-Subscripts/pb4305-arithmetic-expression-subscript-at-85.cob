      *>--- conformance-case
      *> id: pb4305-arithmetic-expression-subscript-at-85
      *> rules: [FMT-8.4.2.3.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - the edition half of
      *>   pb4305_arithmetic_expression_subscript_2002. COBOL-85
      *>   subscripted by an integer or by a data-name or index-name
      *>   plus or minus an integer only (2002 Annex F.2 item 133).
      *>   Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305NSX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T VALUE "ABCD".
           05 E PIC X OCCURS 4.
       01 I PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY E (I * 2)
           STOP RUN.
