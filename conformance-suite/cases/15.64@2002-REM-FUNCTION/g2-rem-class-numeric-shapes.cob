      *>--- conformance-case
      *> id: g2-rem-class-numeric-shapes
      *> rules: [2002-AR-15.64.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.64.2 AR1 - "Argument-1 and argument-2 shall be of
      *>   class numeric."
      *> §2002:15.2 AR9 - "An arithmetic expression or a numeric
      *> data item shall be specified." Each leg writes a class-
      *> numeric argument of another shape (literal, signed display
      *> item, scaled item, packed-decimal item, arithmetic
      *> expression, a PIC 9 USAGE NATIONAL item - class numeric in
      *> 2002 by §2002:8.5.2.9 item 1 and Table 3, kb/Work PB5625
      *> reading B - and the MESSAGE COUNT item of a 2002 input CD),
      *> and each must be ACCEPTED and evaluated. Expected values:
      *> §2002:15.64.3 RV1 - ((argument-1) - ((argument-2) * FUNCTION
      *>   INTEGER-PART ((argument-1) / (argument-2))))
      *> COMPUTEd ROUNDED into +9(3).9(2):
      *>   LIT REM(11 4) = +003.00
      *>   NEG1 REM(W-I 4) = -003.00
      *>   NEG2 REM(11 -4) = +003.00
      *>   SCALE REM(W-SC 2) = +001.50
      *>   PACK REM(W-PK 1) = -000.50
      *>   EXPR REM(10 + 3, 2 * 2) = +001.00
      *>   NAT REM(W-NU W-NN) = +004.00
      *>   CD REM(IN-COUNT 5) = +002.00
      *> A leg fails if its argument shape is refused (the class
      *> check misfiring on a legal operand) or if the value the
      *> function receives is not that shape's value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REM01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-I   PIC S9(3) VALUE -11.
       01  W-SC  PIC S9V9 VALUE 7.5.
       01  W-PK  PIC S9(3)V99 USAGE PACKED-DECIMAL VALUE -2.50.
       01  W-NN  PIC S9(3) USAGE NATIONAL VALUE -5.
       01  W-NU  PIC 9(3) USAGE NATIONAL VALUE 4.
       01  R       PIC +9(3).9(2).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 17 TO IN-COUNT
           COMPUTE R ROUNDED =
               FUNCTION REM(11 4)
           DISPLAY "LIT=" R
           COMPUTE R ROUNDED =
               FUNCTION REM(W-I 4)
           DISPLAY "NEG1=" R
           COMPUTE R ROUNDED =
               FUNCTION REM(11 -4)
           DISPLAY "NEG2=" R
           COMPUTE R ROUNDED =
               FUNCTION REM(W-SC 2)
           DISPLAY "SCALE=" R
           COMPUTE R ROUNDED =
               FUNCTION REM(W-PK 1)
           DISPLAY "PACK=" R
           COMPUTE R ROUNDED =
               FUNCTION REM(10 + 3, 2 * 2)
           DISPLAY "EXPR=" R
           COMPUTE R ROUNDED =
               FUNCTION REM(W-NU W-NN)
           DISPLAY "NAT=" R
           COMPUTE R ROUNDED =
               FUNCTION REM(IN-COUNT 5)
           DISPLAY "CD=" R
           STOP RUN.
