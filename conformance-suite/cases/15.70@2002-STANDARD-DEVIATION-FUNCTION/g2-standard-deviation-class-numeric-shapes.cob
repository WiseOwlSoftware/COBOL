      *>--- conformance-case
      *> id: g2-standard-deviation-class-numeric-shapes
      *> rules: [2002-AR-15.70.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.70.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.2 AR9 - "An arithmetic expression or a numeric
      *> data item shall be specified." Each leg writes a class-
      *> numeric argument of another shape (literal, signed display
      *> item, scaled item, packed-decimal item, arithmetic
      *> expression, a PIC 9 USAGE NATIONAL item - class numeric in
      *> 2002 by §2002:8.5.2.9 item 1 and Table 3, kb/Work PB5625
      *> reading B - and the MESSAGE COUNT item of a 2002 input CD),
      *> and each must be ACCEPTED and evaluated. Expected values:
      *> §2002:15.70.3 RV1 - (FUNCTION SQRT (FUNCTION VARIANCE
      *>   (argument-list)))
      *> COMPUTEd ROUNDED into +9(3).9(6):
      *>   LIT STANDARD-DEVIATION(2 4 4 4 5 5 7 9) = +002.000000
      *>   ONE STANDARD-DEVIATION(5) = +000.000000
      *>   ITEM STANDARD-DEVIATION(W-I 3) = +003.000000
      *>   SCALE STANDARD-DEVIATION(W-PK W-SC) = +001.875000
      *>   EXPR STANDARD-DEVIATION(1 + 1, 2 * 3) = +002.000000
      *>   NAT STANDARD-DEVIATION(W-NN W-NU) = +004.500000
      *>   CD STANDARD-DEVIATION(IN-COUNT 0) = +004.000000
      *> A leg fails if its argument shape is refused (the class
      *> check misfiring on a legal operand) or if the value the
      *> function receives is not that shape's value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2STD01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-I   PIC S9(3) VALUE -3.
       01  W-SC  PIC S9V99 VALUE 1.25.
       01  W-PK  PIC S9(3)V99 USAGE PACKED-DECIMAL VALUE -2.50.
       01  W-NN  PIC S9(3) USAGE NATIONAL VALUE -5.
       01  W-NU  PIC 9(3) USAGE NATIONAL VALUE 4.
       01  R       PIC +9(3).9(6).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 8 TO IN-COUNT
           COMPUTE R ROUNDED =
               FUNCTION STANDARD-DEVIATION(2 4 4 4 5 5 7 9)
           DISPLAY "LIT=" R
           COMPUTE R ROUNDED =
               FUNCTION STANDARD-DEVIATION(5)
           DISPLAY "ONE=" R
           COMPUTE R ROUNDED =
               FUNCTION STANDARD-DEVIATION(W-I 3)
           DISPLAY "ITEM=" R
           COMPUTE R ROUNDED =
               FUNCTION STANDARD-DEVIATION(W-PK W-SC)
           DISPLAY "SCALE=" R
           COMPUTE R ROUNDED =
               FUNCTION STANDARD-DEVIATION(1 + 1, 2 * 3)
           DISPLAY "EXPR=" R
           COMPUTE R ROUNDED =
               FUNCTION STANDARD-DEVIATION(W-NN W-NU)
           DISPLAY "NAT=" R
           COMPUTE R ROUNDED =
               FUNCTION STANDARD-DEVIATION(IN-COUNT 0)
           DISPLAY "CD=" R
           STOP RUN.
