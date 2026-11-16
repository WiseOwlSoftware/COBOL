      *>--- conformance-case
      *> id: g2-range-class-numeric-shapes
      *> rules: [2002-AR-15.63.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.63.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.2 AR9 - "An arithmetic expression or a numeric
      *> data item shall be specified." Each leg writes a class-
      *> numeric argument of another shape (literal, signed display
      *> item, scaled item, packed-decimal item, arithmetic
      *> expression, a PIC 9 USAGE NATIONAL item - class numeric in
      *> 2002 by §2002:8.5.2.9 item 1 and Table 3, kb/Work PB5625
      *> reading B - and the MESSAGE COUNT item of a 2002 input CD),
      *> and each must be ACCEPTED and evaluated. Expected values:
      *> §2002:15.63.3 RV1 - (FUNCTION MAX (argument-list) - FUNCTION
      *>   MIN (argument-list))
      *> COMPUTEd ROUNDED into +9(3).9(2):
      *>   LIT RANGE(3 10 7) = +007.00
      *>   ONE RANGE(5) = +000.00
      *>   ITEM RANGE(W-I 3) = +015.00
      *>   SCALE RANGE(W-PK W-SC) = +003.75
      *>   EXPR RANGE(1 + 1, 5) = +003.00
      *>   NAT RANGE(W-NN W-NU) = +009.00
      *>   CD RANGE(IN-COUNT 2) = +007.00
      *> A leg fails if its argument shape is refused (the class
      *> check misfiring on a legal operand) or if the value the
      *> function receives is not that shape's value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RNG01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-I   PIC S9(3) VALUE -12.
       01  W-SC  PIC S9V99 VALUE 1.25.
       01  W-PK  PIC S9(3)V99 USAGE PACKED-DECIMAL VALUE -2.50.
       01  W-NN  PIC S9(3) USAGE NATIONAL VALUE -5.
       01  W-NU  PIC 9(3) USAGE NATIONAL VALUE 4.
       01  R       PIC +9(3).9(2).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 9 TO IN-COUNT
           COMPUTE R ROUNDED =
               FUNCTION RANGE(3 10 7)
           DISPLAY "LIT=" R
           COMPUTE R ROUNDED =
               FUNCTION RANGE(5)
           DISPLAY "ONE=" R
           COMPUTE R ROUNDED =
               FUNCTION RANGE(W-I 3)
           DISPLAY "ITEM=" R
           COMPUTE R ROUNDED =
               FUNCTION RANGE(W-PK W-SC)
           DISPLAY "SCALE=" R
           COMPUTE R ROUNDED =
               FUNCTION RANGE(1 + 1, 5)
           DISPLAY "EXPR=" R
           COMPUTE R ROUNDED =
               FUNCTION RANGE(W-NN W-NU)
           DISPLAY "NAT=" R
           COMPUTE R ROUNDED =
               FUNCTION RANGE(IN-COUNT 2)
           DISPLAY "CD=" R
           STOP RUN.
