      *>--- conformance-case
      *> id: pb3846-all-zero-in-expression
      *> rules: [SR-8.3.3.6.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3846. ISO 8.3.3.6.3 SR1 a): "If the literal is
      *> restricted to a numeric literal, the only figurative constant
      *> permitted is ZERO (ZEROS, ZEROES) without the ALL phrase"
      *> (cite.py --check 8.3.3.6.3 -> OK). An operand of an arithmetic
      *> expression is such a position (8.8.1.1), so ALL ZERO beside an
      *> operator is barred from 2002 (VCR Table 7 row 7.28, kb/Work
      *> PB1415) exactly as in ADD ALL ZERO TO X; at 85 it is accepted
      *> (pb3846_all_zero_in_expression_85). Before the fix this was a
      *> bare COBOL0001 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3846N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X    PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE X = 1 + ALL ZERO
           DISPLAY "X=" X
           STOP RUN.
