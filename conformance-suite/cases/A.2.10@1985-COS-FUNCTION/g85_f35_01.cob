      *>--- conformance-case
      *> id: g85_f35_01
      *> rules: [1989-AR-A.2.10.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.10.3 AR1 - "Argument-1 must be class numeric." The
      *> permitted form: COS takes a numeric argument, and
      *> §1989:A.2.2 AR1 says of a numeric argument "An arithmetic
      *> expression must be specified." - so a numeric literal, a
      *> numeric data item and an arithmetic expression over one are
      *> each legal and each must be accepted at 85 and evaluated.
      *> §1989:A.2.3 GR2: a numeric function "may be used only in an
      *> arithmetic expression", hence COMPUTE. Values:
      *> §1989:A.2.10.4 RV1, the cosine of argument-1 in radians,
      *> rounded to four places, edited:
      *>   COS(0)            = 1             -> " 1.0000" (literal)
      *>   COS(W-ZERO = 0)   = 1             -> " 1.0000" (data item)
      *>   COS(3.14159265)   = -1 + 1.8E-17  -> "-1.0000" (data item)
      *>   COS(W-PI / 2) = COS(1.570796325) = 1.8E-9    -> " 0.0000"
      *>   COS(W-PI / 3) = COS(1.04719755) = 0.500000001 -> " 0.5000"
      *> Each leg fails if the argument form is refused at 85 or the
      *> expression's value is not the argument the function evaluates.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3501.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-ZERO   PIC S9V9(8) VALUE 0.
       01  W-PI     PIC S9V9(8) VALUE 3.14159265.
       01  W-R      PIC S9V9(4).
       01  W-E      PIC -9.9999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-R ROUNDED = FUNCTION COS (0).
           MOVE W-R TO W-E.
           DISPLAY "LITERAL=" W-E.
           COMPUTE W-R ROUNDED = FUNCTION COS (W-ZERO).
           MOVE W-R TO W-E.
           DISPLAY "ITEM-0=" W-E.
           COMPUTE W-R ROUNDED = FUNCTION COS (W-PI).
           MOVE W-R TO W-E.
           DISPLAY "ITEM-PI=" W-E.
           COMPUTE W-R ROUNDED = FUNCTION COS (W-PI / 2).
           MOVE W-R TO W-E.
           DISPLAY "EXPR-PI/2=" W-E.
           COMPUTE W-R ROUNDED = FUNCTION COS (W-PI / 3).
           MOVE W-R TO W-E.
           DISPLAY "EXPR-PI/3=" W-E.
           STOP RUN.
