      *>--- conformance-case
      *> id: g2-exp-class-numeric-operands
      *> rules: [2002-AR-15.29.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.29.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.2 AR9 - "An arithmetic expression or a numeric
      *> data item shall be specified." Every leg writes a
      *> class-numeric argument-1 of a different shape (literal,
      *> signed display, scaled display, packed-decimal, national
      *> numeric, BINARY-CHAR, FLOAT-LONG, arithmetic expression, and
      *> the MESSAGE COUNT item of a 2002 input CD, an integer of 6
      *> digits without an operational sign by §2002:A.1.2.3 GR2k);
      *> each must be ACCEPTED and evaluated.
      *> §2002:15.29.3 RV1 - "(FUNCTION E ** (argument-1))"; each
      *> value is COMPUTEd ROUNDED into +9(3).9(6). Native
      *> arithmetic returns an implementor-defined approximation
      *> (§2002:15.3.1), and any approximation within 5E-8 of the
      *> true value (NEG lies 5.9E-8 from a boundary) gives:
      *>   LIT  e**1    = 2.7182818..  -> +002.718282
      *>   ZERO e**0    = 1            -> +001.000000
      *>   NEG  e**-1   = 0.3678794..  -> +000.367879
      *>   HALF e**0.5  = 1.6487212..  -> +001.648721
      *>   PACK e**-2   = 0.1353352..  -> +000.135335
      *>   NAT  e**1.5  = 4.4816890..  -> +004.481689
      *>   BCHR e**3    = 20.085536..  -> +020.085537
      *>   FLT  e**2    = 7.3890560..  -> +007.389056
      *>   EXPR e**2.5  = 12.182493..  -> +012.182494
      *>   CD   e**4    = 54.598150..  -> +054.598150
      *> A leg fails if its argument shape is refused (the class
      *> check misfiring on a legal operand), or if the value of that
      *> shape is not the one the function receives (scale, sign,
      *> packed, national, binary or float decoding lost).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXP01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-ONE   PIC S9(4) COMP VALUE 1.
       01  W-NEG   PIC S9 VALUE -1.
       01  W-HALF  PIC 9V9 VALUE 0.5.
       01  W-PK    PIC S9(3)V99 USAGE PACKED-DECIMAL VALUE -2.00.
       01  W-NU    PIC S9V9 USAGE NATIONAL VALUE 1.5.
       01  W-BC    USAGE BINARY-CHAR VALUE 3.
       01  W-FL    USAGE FLOAT-LONG.
       01  R       PIC +9(3).9(6).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 2 TO W-FL
           COMPUTE R ROUNDED = FUNCTION EXP(1)
           DISPLAY "LIT=" R
           COMPUTE R ROUNDED = FUNCTION EXP(0)
           DISPLAY "ZERO=" R
           COMPUTE R ROUNDED = FUNCTION EXP(W-NEG)
           DISPLAY "NEG=" R
           COMPUTE R ROUNDED = FUNCTION EXP(W-HALF)
           DISPLAY "HALF=" R
           COMPUTE R ROUNDED = FUNCTION EXP(W-PK)
           DISPLAY "PACK=" R
           COMPUTE R ROUNDED = FUNCTION EXP(W-NU)
           DISPLAY "NAT=" R
           COMPUTE R ROUNDED = FUNCTION EXP(W-BC)
           DISPLAY "BCHR=" R
           COMPUTE R ROUNDED = FUNCTION EXP(W-FL)
           DISPLAY "FLT=" R
           COMPUTE R ROUNDED = FUNCTION EXP(W-ONE + W-ONE + W-HALF)
           DISPLAY "EXPR=" R
           MOVE 4 TO IN-COUNT
           COMPUTE R ROUNDED = FUNCTION EXP(IN-COUNT)
           DISPLAY "CD=" R
           STOP RUN.
