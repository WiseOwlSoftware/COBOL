      *>--- conformance-case
      *> id: g2-asin-class-numeric-shapes
      *> rules: [2002-AR-15.9.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.9.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.2 AR9 - "An arithmetic expression or a numeric
      *> data item shall be specified." Each leg writes a class-
      *> numeric argument of another shape (literal, signed display
      *> item, scaled item, packed-decimal item, arithmetic
      *> expression, a PIC 9 USAGE NATIONAL item - class numeric in
      *> 2002 by §2002:8.5.2.9 item 1 and Table 3, kb/Work PB5625
      *> reading B - and the MESSAGE COUNT item of a 2002 input CD),
      *> and each must be ACCEPTED and evaluated. Expected values:
      *> §2002:15.9.3 RV1 - the approximation of the arcsine of
      *>   argument-1
      *> COMPUTEd ROUNDED into +9.9(6):
      *>   LIT ASIN(1) = +1.570796
      *>   ZERO ASIN(0) = +0.000000
      *>   ITEM ASIN(W-I) = -1.570796
      *>   SCALE ASIN(W-SC) = +0.523599
      *>   PACK ASIN(W-PK) = -0.523599
      *>   EXPR ASIN(W-SC + 0.5) = +1.570796
      *>   NAT ASIN(W-NN) = +1.570796
      *>   CD ASIN(IN-COUNT) = +0.000000
      *> A leg fails if its argument shape is refused (the class
      *> check misfiring on a legal operand) or if the value the
      *> function receives is not that shape's value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ASN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-I   PIC S9 VALUE -1.
       01  W-SC  PIC 9V9 VALUE 0.5.
       01  W-PK  PIC S9V99 USAGE PACKED-DECIMAL VALUE -0.50.
       01  W-NN  PIC S9 USAGE NATIONAL VALUE 1.
       01  R       PIC +9.9(6).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 0 TO IN-COUNT
           COMPUTE R ROUNDED =
               FUNCTION ASIN(1)
           DISPLAY "LIT=" R
           COMPUTE R ROUNDED =
               FUNCTION ASIN(0)
           DISPLAY "ZERO=" R
           COMPUTE R ROUNDED =
               FUNCTION ASIN(W-I)
           DISPLAY "ITEM=" R
           COMPUTE R ROUNDED =
               FUNCTION ASIN(W-SC)
           DISPLAY "SCALE=" R
           COMPUTE R ROUNDED =
               FUNCTION ASIN(W-PK)
           DISPLAY "PACK=" R
           COMPUTE R ROUNDED =
               FUNCTION ASIN(W-SC + 0.5)
           DISPLAY "EXPR=" R
           COMPUTE R ROUNDED =
               FUNCTION ASIN(W-NN)
           DISPLAY "NAT=" R
           COMPUTE R ROUNDED =
               FUNCTION ASIN(IN-COUNT)
           DISPLAY "CD=" R
           STOP RUN.
