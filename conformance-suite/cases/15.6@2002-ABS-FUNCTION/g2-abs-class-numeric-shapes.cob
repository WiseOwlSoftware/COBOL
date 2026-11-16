      *>--- conformance-case
      *> id: g2-abs-class-numeric-shapes
      *> rules: [2002-AR-15.6.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.6.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.2 AR9 - "An arithmetic expression or a numeric
      *> data item shall be specified." Each leg writes a class-
      *> numeric argument of another shape (literal, signed display
      *> item, scaled item, packed-decimal item, arithmetic
      *> expression, a PIC 9 USAGE NATIONAL item - class numeric in
      *> 2002 by §2002:8.5.2.9 item 1 and Table 3, kb/Work PB5625
      *> reading B - and the MESSAGE COUNT item of a 2002 input CD),
      *> and each must be ACCEPTED and evaluated. Expected values:
      *> §2002:15.6.3 RV1 - (argument-1) when zero or positive, (-
      *>   (argument-1)) when negative
      *> COMPUTEd ROUNDED into +9(3).9(2):
      *>   LIT ABS(-7) = +007.00
      *>   ZERO ABS(0) = +000.00
      *>   ITEM ABS(W-I) = +012.00
      *>   SCALE ABS(W-SC) = +001.25
      *>   PACK ABS(W-PK) = +002.50
      *>   EXPR ABS(W-I + 2) = +010.00
      *>   NAT ABS(W-NN) = +005.00
      *>   CD ABS(IN-COUNT) = +004.00
      *> A leg fails if its argument shape is refused (the class
      *> check misfiring on a legal operand) or if the value the
      *> function receives is not that shape's value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ABS01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-I   PIC S9(3) VALUE -12.
       01  W-SC  PIC S9V99 VALUE 1.25.
       01  W-PK  PIC S9(3)V99 USAGE PACKED-DECIMAL VALUE -2.50.
       01  W-NN  PIC S9(3) USAGE NATIONAL VALUE -5.
       01  R       PIC +9(3).9(2).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 4 TO IN-COUNT
           COMPUTE R ROUNDED =
               FUNCTION ABS(-7)
           DISPLAY "LIT=" R
           COMPUTE R ROUNDED =
               FUNCTION ABS(0)
           DISPLAY "ZERO=" R
           COMPUTE R ROUNDED =
               FUNCTION ABS(W-I)
           DISPLAY "ITEM=" R
           COMPUTE R ROUNDED =
               FUNCTION ABS(W-SC)
           DISPLAY "SCALE=" R
           COMPUTE R ROUNDED =
               FUNCTION ABS(W-PK)
           DISPLAY "PACK=" R
           COMPUTE R ROUNDED =
               FUNCTION ABS(W-I + 2)
           DISPLAY "EXPR=" R
           COMPUTE R ROUNDED =
               FUNCTION ABS(W-NN)
           DISPLAY "NAT=" R
           COMPUTE R ROUNDED =
               FUNCTION ABS(IN-COUNT)
           DISPLAY "CD=" R
           STOP RUN.
