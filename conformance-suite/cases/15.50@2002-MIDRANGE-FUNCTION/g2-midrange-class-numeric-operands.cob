      *>--- conformance-case
      *> id: g2-midrange-class-numeric-operands
      *> rules: [2002-AR-15.50.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.50.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.2 AR9 - "An arithmetic expression or a numeric
      *> data item shall be specified." The format repeats
      *> argument-1, so every position of the list is argument-1;
      *> each leg puts a class-numeric argument of a different shape
      *> (literal, signed display, scaled display, packed-decimal,
      *> national numeric, BINARY-CHAR, FLOAT-LONG, arithmetic
      *> expression, and the MESSAGE COUNT item of a 2002 input CD,
      *> an integer of 6 digits without an operational sign by
      *> §2002:A.1.2.3 GR2k) into the list; each must be ACCEPTED.
      *> §2002:15.50.3 RV1 - "((FUNCTION MAX (argument-list) +
      *> FUNCTION MIN (argument-list)) / 2)". Every value is exact;
      *> into +9(3).9(4):
      *>   LIT  (4, -1, 9, 2)    -> (9 + -1)/2      -> +004.0000
      *>   NEG  (W-NEG, 6)       -> (6 + -1)/2      -> +002.5000
      *>   HALF (W-HALF, W-NEG)  -> (0.5 + -1)/2    -> -000.2500
      *>   PACK (W-PK, 0)        -> (0 + -2.25)/2   -> -001.1250
      *>   NAT  (W-NU, 10)       -> (10 + -1.5)/2   -> +004.2500
      *>   BCHR (W-BC, 1)        -> (3 + 1)/2       -> +002.0000
      *>   FLT  (W-FL, 0)        -> (0 + -3.125)/2  -> -001.5625
      *>   EXPR (1 + 0.5, 9)     -> (9 + 1.5)/2     -> +005.2500
      *>   CD   (IN-COUNT, 2)    -> (6 + 2)/2       -> +004.0000
      *>   MIX  (every item shape above in one list, plus W-TEN)
      *>        max 10, min -3.125 -> 6.875/2        -> +003.4375
      *> A leg fails if one shape in the list is refused (the class
      *> check misfiring on a legal operand), or if its value is not
      *> the one compared for MAX and MIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MID01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-ONE   PIC S9(4) COMP VALUE 1.
       01  W-NEG   PIC S9 VALUE -1.
       01  W-HALF  PIC 9V9 VALUE 0.5.
       01  W-PK    PIC S9(3)V99 USAGE PACKED-DECIMAL VALUE -2.25.
       01  W-NU    PIC S9V9 USAGE NATIONAL VALUE -1.5.
       01  W-BC    USAGE BINARY-CHAR VALUE 3.
       01  W-FL    USAGE FLOAT-LONG.
       01  W-TEN   PIC 99 VALUE 10.
       01  R       PIC +9(3).9(4).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE -3.125 TO W-FL
           MOVE 6 TO IN-COUNT
           COMPUTE R = FUNCTION MIDRANGE(4, -1, 9, 2)
           DISPLAY "LIT=" R
           COMPUTE R = FUNCTION MIDRANGE(W-NEG, 6)
           DISPLAY "NEG=" R
           COMPUTE R = FUNCTION MIDRANGE(W-HALF, W-NEG)
           DISPLAY "HALF=" R
           COMPUTE R = FUNCTION MIDRANGE(W-PK, 0)
           DISPLAY "PACK=" R
           COMPUTE R = FUNCTION MIDRANGE(W-NU, 10)
           DISPLAY "NAT=" R
           COMPUTE R = FUNCTION MIDRANGE(W-BC, 1)
           DISPLAY "BCHR=" R
           COMPUTE R = FUNCTION MIDRANGE(W-FL, 0)
           DISPLAY "FLT=" R
           COMPUTE R = FUNCTION MIDRANGE(W-ONE + W-HALF, 9)
           DISPLAY "EXPR=" R
           COMPUTE R = FUNCTION MIDRANGE(IN-COUNT, 2)
           DISPLAY "CD=" R
           COMPUTE R = FUNCTION MIDRANGE(W-NEG, W-HALF, W-PK, W-NU,
               W-BC, W-FL, W-ONE + W-HALF, IN-COUNT, W-TEN)
           DISPLAY "MIX=" R
           STOP RUN.
