      *>--- conformance-case
      *> id: g2-fraction-part-class-numeric-operands
      *> rules: [2002-AR-15.32.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.32.2 AR1 - "Argument-1 shall be of the class
      *> numeric." §2002:15.2 AR9 - "An arithmetic expression or a
      *> numeric data item shall be specified." Every leg writes a
      *> class-numeric argument-1 of a different shape (literal,
      *> signed display, scaled display, packed-decimal, national
      *> numeric, BINARY-CHAR, FLOAT-LONG, arithmetic expression, and
      *> the MESSAGE COUNT item of a 2002 input CD, an integer of 6
      *> digits without an operational sign by §2002:A.1.2.3 GR2k);
      *> each must be ACCEPTED and evaluated.
      *> §2002:15.32.3 RV1 - "(argument-1 - FUNCTION INTEGER-PART
      *> (argument-1))", with §2002:15.38.3 RV1 INTEGER-PART =
      *> SIGN(arg) * INTEGER(ABS(arg)), so the fraction keeps the
      *> sign of argument-1. Every value is exact; into +9.9(6):
      *>   LIT  3.75    -> 3.75 - 3      -> +0.750000
      *>   ZERO 0       -> 0             -> +0.000000
      *>   NEG  -1      -> -1 - (-1) = 0 -> +0.000000
      *>   HALF 0.5     -> 0.5 - 0       -> +0.500000
      *>   PACK -2.25   -> -2.25 - (-2)  -> -0.250000
      *>   NAT  -1.5    -> -1.5 - (-1)   -> -0.500000
      *>   BCHR 3       -> 3 - 3         -> +0.000000
      *>   FLT  -3.125  -> -3.125 - (-3) -> -0.125000
      *>   EXPR 1 + 0.5 -> 1.5 - 1       -> +0.500000
      *>   CD   7       -> 7 - 7         -> +0.000000
      *> A leg fails if its argument shape is refused (the class
      *> check misfiring on a legal operand), if the integer part is
      *> taken as the floor for a negative argument (PACK would be
      *> +0.75, NAT +0.5, FLT +0.875), or if the value of that shape
      *> is not the one the function receives.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FRP02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-ONE   PIC S9(4) COMP VALUE 1.
       01  W-NEG   PIC S9 VALUE -1.
       01  W-HALF  PIC 9V9 VALUE 0.5.
       01  W-PK    PIC S9(3)V99 USAGE PACKED-DECIMAL VALUE -2.25.
       01  W-NU    PIC S9V9 USAGE NATIONAL VALUE -1.5.
       01  W-BC    USAGE BINARY-CHAR VALUE 3.
       01  W-FL    USAGE FLOAT-LONG.
       01  R       PIC +9.9(6).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE -3.125 TO W-FL
           COMPUTE R = FUNCTION FRACTION-PART(3.75)
           DISPLAY "LIT=" R
           COMPUTE R = FUNCTION FRACTION-PART(0)
           DISPLAY "ZERO=" R
           COMPUTE R = FUNCTION FRACTION-PART(W-NEG)
           DISPLAY "NEG=" R
           COMPUTE R = FUNCTION FRACTION-PART(W-HALF)
           DISPLAY "HALF=" R
           COMPUTE R = FUNCTION FRACTION-PART(W-PK)
           DISPLAY "PACK=" R
           COMPUTE R = FUNCTION FRACTION-PART(W-NU)
           DISPLAY "NAT=" R
           COMPUTE R = FUNCTION FRACTION-PART(W-BC)
           DISPLAY "BCHR=" R
           COMPUTE R = FUNCTION FRACTION-PART(W-FL)
           DISPLAY "FLT=" R
           COMPUTE R = FUNCTION FRACTION-PART(W-ONE + W-HALF)
           DISPLAY "EXPR=" R
           MOVE 7 TO IN-COUNT
           COMPUTE R = FUNCTION FRACTION-PART(IN-COUNT)
           DISPLAY "CD=" R
           STOP RUN.
