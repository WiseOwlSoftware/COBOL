      *>--- conformance-case
      *> id: g2-variance-class-numeric-shapes
      *> rules: [2002-AR-15.79.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.79.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.2 AR9 - "An arithmetic expression or a numeric
      *> data item shall be specified." Each leg writes a class-
      *> numeric argument of another shape (literal, signed display
      *> item, scaled item, packed-decimal item, arithmetic
      *> expression, a PIC 9 USAGE NATIONAL item - class numeric in
      *> 2002 by §2002:8.5.2.9 item 1 and Table 3, kb/Work PB5625
      *> reading B - and the MESSAGE COUNT item of a 2002 input CD),
      *> and each must be ACCEPTED and evaluated. Expected values:
      *> §2002:15.79.3 RV1 - a) one argument (0); c) SUM((a(i) -
      *>   MEAN)**2) / n
      *> COMPUTEd ROUNDED into +9(3).9(6):
      *>   LIT VARIANCE(2 4 4 4 5 5 7 9) = +004.000000
      *>   ONE VARIANCE(5) = +000.000000
      *>   ITEM VARIANCE(W-I 3) = +009.000000
      *>   SCALE VARIANCE(W-PK W-SC) = +003.515625
      *>   EXPR VARIANCE(1 + 1, 2 * 3) = +004.000000
      *>   NAT VARIANCE(W-NN W-NU) = +020.250000
      *>   CD VARIANCE(IN-COUNT 0) = +016.000000
      *> A leg fails if its argument shape is refused (the class
      *> check misfiring on a legal operand) or if the value the
      *> function receives is not that shape's value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VAR01.
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
               FUNCTION VARIANCE(2 4 4 4 5 5 7 9)
           DISPLAY "LIT=" R
           COMPUTE R ROUNDED =
               FUNCTION VARIANCE(5)
           DISPLAY "ONE=" R
           COMPUTE R ROUNDED =
               FUNCTION VARIANCE(W-I 3)
           DISPLAY "ITEM=" R
           COMPUTE R ROUNDED =
               FUNCTION VARIANCE(W-PK W-SC)
           DISPLAY "SCALE=" R
           COMPUTE R ROUNDED =
               FUNCTION VARIANCE(1 + 1, 2 * 3)
           DISPLAY "EXPR=" R
           COMPUTE R ROUNDED =
               FUNCTION VARIANCE(W-NN W-NU)
           DISPLAY "NAT=" R
           COMPUTE R ROUNDED =
               FUNCTION VARIANCE(IN-COUNT 0)
           DISPLAY "CD=" R
           STOP RUN.
