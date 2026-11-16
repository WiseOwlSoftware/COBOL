      *>--- conformance-case
      *> id: g2-acos-class-numeric-shapes
      *> rules: [2002-AR-15.7.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.7.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.2 AR9 - "An arithmetic expression or a numeric
      *> data item shall be specified." Each leg writes a class-
      *> numeric argument of another shape (literal, signed display
      *> item, scaled item, packed-decimal item, arithmetic
      *> expression, a PIC 9 USAGE NATIONAL item - class numeric in
      *> 2002 by §2002:8.5.2.9 item 1 and Table 3, kb/Work PB5625
      *> reading B - and the MESSAGE COUNT item of a 2002 input CD),
      *> and each must be ACCEPTED and evaluated. Expected values:
      *> §2002:15.7.3 RV1 - the approximation of the arccosine of
      *>   argument-1
      *> COMPUTEd ROUNDED into +9.9(6):
      *>   LIT ACOS(1) = +0.000000
      *>   ZERO ACOS(0) = +1.570796
      *>   ITEM ACOS(W-I) = +3.141593
      *>   SCALE ACOS(W-SC) = +1.047198
      *>   PACK ACOS(W-PK) = +2.094395
      *>   EXPR ACOS(W-SC - 0.5) = +1.570796
      *>   NAT ACOS(W-NN) = +0.000000
      *>   CD ACOS(IN-COUNT) = +1.570796
      *> A leg fails if its argument shape is refused (the class
      *> check misfiring on a legal operand) or if the value the
      *> function receives is not that shape's value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ACS01.
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
               FUNCTION ACOS(1)
           DISPLAY "LIT=" R
           COMPUTE R ROUNDED =
               FUNCTION ACOS(0)
           DISPLAY "ZERO=" R
           COMPUTE R ROUNDED =
               FUNCTION ACOS(W-I)
           DISPLAY "ITEM=" R
           COMPUTE R ROUNDED =
               FUNCTION ACOS(W-SC)
           DISPLAY "SCALE=" R
           COMPUTE R ROUNDED =
               FUNCTION ACOS(W-PK)
           DISPLAY "PACK=" R
           COMPUTE R ROUNDED =
               FUNCTION ACOS(W-SC - 0.5)
           DISPLAY "EXPR=" R
           COMPUTE R ROUNDED =
               FUNCTION ACOS(W-NN)
           DISPLAY "NAT=" R
           COMPUTE R ROUNDED =
               FUNCTION ACOS(IN-COUNT)
           DISPLAY "CD=" R
           STOP RUN.
