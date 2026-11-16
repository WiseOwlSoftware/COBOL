      *>--- conformance-case
      *> id: g2-sum-class-numeric-shapes
      *> rules: [2002-AR-15.71.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.71.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.2 AR9 - "An arithmetic expression or a numeric
      *> data item shall be specified." Each leg writes a class-
      *> numeric argument of another shape (literal, signed display
      *> item, scaled item, packed-decimal item, arithmetic
      *> expression, a PIC 9 USAGE NATIONAL item - class numeric in
      *> 2002 by §2002:8.5.2.9 item 1 and Table 3, kb/Work PB5625
      *> reading B - and the MESSAGE COUNT item of a 2002 input CD),
      *> and each must be ACCEPTED and evaluated. Expected values:
      *> §2002:15.71.3 RV1 - (argument-1(1) + argument-1(2) + ... +
      *>   argument-1(n))
      *> COMPUTEd ROUNDED into +9(3).9(2):
      *>   LIT SUM(1 2 3) = +006.00
      *>   ONE SUM(-7) = -007.00
      *>   ITEM SUM(W-I W-SC W-PK) = -013.25
      *>   EXPR SUM(2 * 3, 4) = +010.00
      *>   NAT SUM(W-NN W-NU) = -001.00
      *>   CD SUM(IN-COUNT 1) = +042.00
      *> A leg fails if its argument shape is refused (the class
      *> check misfiring on a legal operand) or if the value the
      *> function receives is not that shape's value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FSM01.
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
           MOVE 41 TO IN-COUNT
           COMPUTE R ROUNDED =
               FUNCTION SUM(1 2 3)
           DISPLAY "LIT=" R
           COMPUTE R ROUNDED =
               FUNCTION SUM(-7)
           DISPLAY "ONE=" R
           COMPUTE R ROUNDED =
               FUNCTION SUM(W-I W-SC W-PK)
           DISPLAY "ITEM=" R
           COMPUTE R ROUNDED =
               FUNCTION SUM(2 * 3, 4)
           DISPLAY "EXPR=" R
           COMPUTE R ROUNDED =
               FUNCTION SUM(W-NN W-NU)
           DISPLAY "NAT=" R
           COMPUTE R ROUNDED =
               FUNCTION SUM(IN-COUNT 1)
           DISPLAY "CD=" R
           STOP RUN.
