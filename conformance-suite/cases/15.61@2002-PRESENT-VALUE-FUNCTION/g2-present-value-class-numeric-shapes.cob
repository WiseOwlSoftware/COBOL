      *>--- conformance-case
      *> id: g2-present-value-class-numeric-shapes
      *> rules: [2002-AR-15.61.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.61.2 AR1 - "Argument-1 and argument-2 shall be of
      *>   the class numeric."
      *> §2002:15.2 AR9 - "An arithmetic expression or a numeric
      *> data item shall be specified." Each leg writes a class-
      *> numeric argument of another shape (literal, signed display
      *> item, scaled item, packed-decimal item, arithmetic
      *> expression, a PIC 9 USAGE NATIONAL item - class numeric in
      *> 2002 by §2002:8.5.2.9 item 1 and Table 3, kb/Work PB5625
      *> reading B - and the MESSAGE COUNT item of a 2002 input CD),
      *> and each must be ACCEPTED and evaluated. Expected values:
      *> §2002:15.61.3 RV1 - a) (argument-2 / (1 + argument-1)); b)
      *>   a2(1)/(1+a1) + a2(2)/(1+a1)**2
      *> COMPUTEd ROUNDED into +9(4).9(4):
      *>   LIT PRESENT-VALUE(0 100) = +0100.0000
      *>   TWO PRESENT-VALUE(1 100 100) = +0075.0000
      *>   ITEM PRESENT-VALUE(1 W-I) = +0050.0000
      *>   SCALE PRESENT-VALUE(W-SC 125) = +0100.0000
      *>   PACK PRESENT-VALUE(W-SC W-PK) = +0002.0000
      *>   EXPR PRESENT-VALUE(W-SC - 0.25, 3 + 4) = +0007.0000
      *>   NAT PRESENT-VALUE(W-NR W-NN) = +0002.0000
      *>   CD PRESENT-VALUE(1 IN-COUNT) = +0004.0000
      *> A leg fails if its argument shape is refused (the class
      *> check misfiring on a legal operand) or if the value the
      *> function receives is not that shape's value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRV01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-I   PIC S9(3) VALUE 100.
       01  W-SC  PIC S9V99 VALUE 0.25.
       01  W-PK  PIC S9(3)V99 USAGE PACKED-DECIMAL VALUE 2.50.
       01  W-NN  PIC S9(3) USAGE NATIONAL VALUE 4.
       01  W-NR  PIC 9 USAGE NATIONAL VALUE 1.
       01  R       PIC +9(4).9(4).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 8 TO IN-COUNT
           COMPUTE R ROUNDED =
               FUNCTION PRESENT-VALUE(0 100)
           DISPLAY "LIT=" R
           COMPUTE R ROUNDED =
               FUNCTION PRESENT-VALUE(1 100 100)
           DISPLAY "TWO=" R
           COMPUTE R ROUNDED =
               FUNCTION PRESENT-VALUE(1 W-I)
           DISPLAY "ITEM=" R
           COMPUTE R ROUNDED =
               FUNCTION PRESENT-VALUE(W-SC 125)
           DISPLAY "SCALE=" R
           COMPUTE R ROUNDED =
               FUNCTION PRESENT-VALUE(W-SC W-PK)
           DISPLAY "PACK=" R
           COMPUTE R ROUNDED =
               FUNCTION PRESENT-VALUE(W-SC - 0.25, 3 + 4)
           DISPLAY "EXPR=" R
           COMPUTE R ROUNDED =
               FUNCTION PRESENT-VALUE(W-NR W-NN)
           DISPLAY "NAT=" R
           COMPUTE R ROUNDED =
               FUNCTION PRESENT-VALUE(1 IN-COUNT)
           DISPLAY "CD=" R
           STOP RUN.
