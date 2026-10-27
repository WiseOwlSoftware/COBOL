      *>--- conformance-case
      *> id: pb3846_all_zero_in_expression_85
      *> rules: [SR-8.3.3.6.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ALL ZERO AS AN OPERAND INSIDE AN ARITHMETIC EXPRESSION, AT 85
      *> (kb/Work PB3846). ISO 8.8.1.1: "An arithmetic expression may be
      *> ... the figurative constant ZERO (ZEROS, ZEROES)", and 8.3.3.6.2
      *> Format 1 prints that constant with ALL as an optional word.
      *> 8.3.3.6.3 SR1 a) bars "ZERO ... without the ALL phrase" where a
      *> literal is numeric-restricted, an edge DERIVED at 2002 (VCR Table
      *> 7 row 7.28, kb/Work PB1415): at 85 the ALL is redundant and each
      *> ALL ZERO below is the numeric value 0 (8.3.3.6.4 GR4). The 2002
      *> refusal is pb3846-all-zero-in-expression. Before the fix every
      *> leg but the last was a bare COBOL0001 (the operator's neighbour
      *> was the ALL, so the ZERO was not read as arithmetic); the last
      *> parsed and failed in the C# backend. Each leg fails if the
      *> operand is not 0 or its position is lost:
      *>   1 + ALL ZERO            -> 0001  (right operand)
      *>   ALL ZEROS + 2           -> 0002  (left operand)
      *>   (ALL ZEROES) + 3        -> 0003  (parenthesised, left)
      *>   4 - (ALL ZERO)          -> 0004  (parenthesised, right)
      *>   - ALL ZERO              -> 0000  (unary minus)
      *>   2 ** ALL ZERO           -> 0001  (exponent: 2 ** 0 = 1)
      *>   3 * ALL ZERO + 6        -> 0006  (product, then sum)
      *>   IF X + ALL ZERO = 6     -> EQ    (a relation's operand)
      *>   T(ALL ZERO + 2:2)       -> BC    (a ref-mod position, ABCD)
      *>   88 C VALUE ALL ZERO -5  -> C TRUE over V = 0 (a VALUE list,
      *>     where the next signed literal made the ZERO arithmetic)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3846A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X    PIC 9(4) VALUE 7.
       01 T    PIC X(4) VALUE "ABCD".
       01 V    PIC S9 VALUE 0.
          88 C VALUE ALL ZERO -5.
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE X = 1 + ALL ZERO
           DISPLAY X
           COMPUTE X = ALL ZEROS + 2
           DISPLAY X
           COMPUTE X = (ALL ZEROES) + 3
           DISPLAY X
           COMPUTE X = 4 - (ALL ZERO)
           DISPLAY X
           COMPUTE X = - ALL ZERO
           DISPLAY X
           COMPUTE X = 2 ** ALL ZERO
           DISPLAY X
           COMPUTE X = 3 * ALL ZERO + 6
           DISPLAY X
           IF X + ALL ZERO = 6
               DISPLAY "EQ"
           ELSE
               DISPLAY "NE"
           END-IF
           DISPLAY T(ALL ZERO + 2:2)
           IF C
               DISPLAY "C TRUE"
           ELSE
               DISPLAY "C FALSE"
           END-IF
           STOP RUN.
