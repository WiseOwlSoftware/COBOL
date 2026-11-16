      *>--- conformance-case
      *> id: g2-sin-class-numeric-shapes
      *> rules: [2002-AR-15.67.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.67.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.2 AR9 - "An arithmetic expression or a numeric
      *> data item shall be specified." Each leg writes a class-
      *> numeric argument of another shape (literal, signed display
      *> item, scaled item, packed-decimal item, arithmetic
      *> expression, a PIC 9 USAGE NATIONAL item - class numeric in
      *> 2002 by §2002:8.5.2.9 item 1 and Table 3, kb/Work PB5625
      *> reading B - and the MESSAGE COUNT item of a 2002 input CD),
      *> and each must be ACCEPTED and evaluated. Expected values:
      *> §2002:15.67.3 RV1 - the approximation of the sine of
      *>   argument-1
      *> COMPUTEd ROUNDED into +9.9(6):
      *>   LIT SIN(1) = +0.841471
      *>   ZERO SIN(0) = +0.000000
      *>   ITEM SIN(W-I) = -0.841471
      *>   SCALE SIN(W-SC) = +0.479426
      *>   PACK SIN(W-PK) = -0.598472
      *>   EXPR SIN(W-SC + 1) = +0.997495
      *>   NAT SIN(W-NN) = +0.909297
      *>   CD SIN(IN-COUNT) = +0.141120
      *> A leg fails if its argument shape is refused (the class
      *> check misfiring on a legal operand) or if the value the
      *> function receives is not that shape's value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SIN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-I   PIC S9 VALUE -1.
       01  W-SC  PIC 9V9 VALUE 0.5.
       01  W-PK  PIC S9V99 USAGE PACKED-DECIMAL VALUE -2.50.
       01  W-NN  PIC S9 USAGE NATIONAL VALUE 2.
       01  R       PIC +9.9(6).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 3 TO IN-COUNT
           COMPUTE R ROUNDED =
               FUNCTION SIN(1)
           DISPLAY "LIT=" R
           COMPUTE R ROUNDED =
               FUNCTION SIN(0)
           DISPLAY "ZERO=" R
           COMPUTE R ROUNDED =
               FUNCTION SIN(W-I)
           DISPLAY "ITEM=" R
           COMPUTE R ROUNDED =
               FUNCTION SIN(W-SC)
           DISPLAY "SCALE=" R
           COMPUTE R ROUNDED =
               FUNCTION SIN(W-PK)
           DISPLAY "PACK=" R
           COMPUTE R ROUNDED =
               FUNCTION SIN(W-SC + 1)
           DISPLAY "EXPR=" R
           COMPUTE R ROUNDED =
               FUNCTION SIN(W-NN)
           DISPLAY "NAT=" R
           COMPUTE R ROUNDED =
               FUNCTION SIN(IN-COUNT)
           DISPLAY "CD=" R
           STOP RUN.
