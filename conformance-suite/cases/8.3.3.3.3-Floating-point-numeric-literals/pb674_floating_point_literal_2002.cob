*>--- conformance-case
*> id: pb674_floating_point_literal_2002
*> rules: [SR-8.3.3.3.3-1, GR-8.3.3.3.3-5]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB674 - the FLOATING-POINT NUMERIC LITERAL, 8.3.3.3.3 1): "A floating-point numeric literal is formed
      *> from two fixed-point numeric literals separated by the letter 'E' without intervening spaces", and 5): its
      *> value "is the algebraic product of the value of its significand and the quantity derived by raising ten to
      *> the power of the exponent". COBOL-85's numeric literal is the fixed-point form alone, so the form is a
      *> COBOL-2002 introduction (VCR row 7.37, derived): this program, which declares NO floating-point data item,
      *> compiles at 2002 and later and is refused at 85 (negative/pb674-floating-point-literal-85).
      *> The literal is written in every position class a numeric literal has, and each position must keep the
      *> value rule 5 gives, so a position the gate failed to reach would still compile at 85 and a position the
      *> evaluator failed to reach would print another number:
      *>   VALUE            1.5E2   = 1.5 x 10^2    = 150      -> A  = 150.00
      *>   MOVE source      2.5E1   = 2.5 x 10^1    = 25       -> B  = 25.00
      *>   COMPUTE operand  2.5E-1  = 2.5 x 10^-1   = 0.25     -> B  = A + 0.25 = 150.25
      *>   ADD operand      1.0E1   = 1.0 x 10^1    = 10       -> B  = 160.25
      *>   relation operand 1.6E2   = 1.6 x 10^2    = 160      -> B > 160 is true
      *>   function argument 1.25E2 = 1.25 x 10^2   = 125      -> C  = INTEGER (125) = 125
      *> Each leg can fail on its own: a wrong exponent sign prints 160.25 as 0.00 or 1600.25, and a literal read as
      *> its significand alone prints 1.50 for A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB674FP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(5)V99 VALUE 1.5E2.
       01 B PIC 9(5)V99.
       01 C PIC 9(3).
       01 ED PIC ZZZZ9.99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE A TO ED
           DISPLAY "VALUE " ED
           MOVE 2.5E1 TO B
           MOVE B TO ED
           DISPLAY "MOVE " ED
           COMPUTE B = A + 2.5E-1
           MOVE B TO ED
           DISPLAY "COMPUTE " ED
           ADD 1.0E1 TO B
           MOVE B TO ED
           DISPLAY "ADD " ED
           IF B > 1.6E2
               DISPLAY "RELATION TRUE"
           ELSE
               DISPLAY "RELATION FALSE"
           END-IF
           COMPUTE C = FUNCTION INTEGER (1.25E2)
           DISPLAY "FUNCTION " C
           STOP RUN.
