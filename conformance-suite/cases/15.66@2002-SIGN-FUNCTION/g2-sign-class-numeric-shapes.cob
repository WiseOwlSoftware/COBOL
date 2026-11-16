      *>--- conformance-case
      *> id: g2-sign-class-numeric-shapes
      *> rules: [2002-AR-15.66.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.66.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.2 AR9 - "An arithmetic expression or a numeric
      *> data item shall be specified." Each leg writes a class-
      *> numeric argument of another shape (literal, signed display
      *> item, scaled item, packed-decimal item, arithmetic
      *> expression, a PIC 9 USAGE NATIONAL item - class numeric in
      *> 2002 by §2002:8.5.2.9 item 1 and Table 3, kb/Work PB5625
      *> reading B - and the MESSAGE COUNT item of a 2002 input CD),
      *> and each must be ACCEPTED and evaluated. Expected values:
      *> §2002:15.66.3 RV1 - (1) positive, (0) zero, (-1) negative
      *> COMPUTEd ROUNDED into +9:
      *>   LIT SIGN(5) = +1
      *>   ZERO SIGN(0) = +0
      *>   ITEM SIGN(W-I) = -1
      *>   SCALE SIGN(W-SC) = +1
      *>   PACK SIGN(W-PK) = -1
      *>   EXPR SIGN(W-I + 12) = +0
      *>   NAT SIGN(W-NN) = -1
      *>   CD SIGN(IN-COUNT) = +1
      *> A leg fails if its argument shape is refused (the class
      *> check misfiring on a legal operand) or if the value the
      *> function receives is not that shape's value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FSG01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-I   PIC S9(3) VALUE -12.
       01  W-SC  PIC S9V99 VALUE 1.25.
       01  W-PK  PIC S9(3)V99 USAGE PACKED-DECIMAL VALUE -0.01.
       01  W-NN  PIC S9(3) USAGE NATIONAL VALUE -5.
       01  R       PIC +9.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 3 TO IN-COUNT
           COMPUTE R ROUNDED =
               FUNCTION SIGN(5)
           DISPLAY "LIT=" R
           COMPUTE R ROUNDED =
               FUNCTION SIGN(0)
           DISPLAY "ZERO=" R
           COMPUTE R ROUNDED =
               FUNCTION SIGN(W-I)
           DISPLAY "ITEM=" R
           COMPUTE R ROUNDED =
               FUNCTION SIGN(W-SC)
           DISPLAY "SCALE=" R
           COMPUTE R ROUNDED =
               FUNCTION SIGN(W-PK)
           DISPLAY "PACK=" R
           COMPUTE R ROUNDED =
               FUNCTION SIGN(W-I + 12)
           DISPLAY "EXPR=" R
           COMPUTE R ROUNDED =
               FUNCTION SIGN(W-NN)
           DISPLAY "NAT=" R
           COMPUTE R ROUNDED =
               FUNCTION SIGN(IN-COUNT)
           DISPLAY "CD=" R
           STOP RUN.
