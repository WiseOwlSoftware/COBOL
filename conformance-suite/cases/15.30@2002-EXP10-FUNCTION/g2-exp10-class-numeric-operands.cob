      *>--- conformance-case
      *> id: g2-exp10-class-numeric-operands
      *> rules: [2002-AR-15.30.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.30.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.2 AR9 - "An arithmetic expression or a numeric
      *> data item shall be specified." Every leg writes a
      *> class-numeric argument-1 of a different shape (literal,
      *> signed display, scaled display, packed-decimal, national
      *> numeric, BINARY-CHAR, FLOAT-LONG, arithmetic expression, and
      *> the MESSAGE COUNT item of a 2002 input CD, an integer of 6
      *> digits without an operational sign by §2002:A.1.2.3 GR2k);
      *> each must be ACCEPTED and evaluated.
      *> §2002:15.30.3 RV1 - "(10 ** (argument-1))"; each value is
      *> COMPUTEd ROUNDED into +9(4).9(6). Native arithmetic
      *> returns an implementor-defined approximation
      *> (§2002:15.3.1), and any approximation within 5E-8 of the
      *> true value (FLT lies 9.0E-8 from a boundary) gives:
      *>   LIT  10**2     = 100          -> +0100.000000
      *>   ZERO 10**0     = 1            -> +0001.000000
      *>   NEG  10**-1    = 0.1          -> +0000.100000
      *>   HALF 10**0.5   = 3.1622776..  -> +0003.162278
      *>   PACK 10**-2    = 0.01         -> +0000.010000
      *>   NAT  10**1.5   = 31.622776..  -> +0031.622777
      *>   BCHR 10**3     = 1000         -> +1000.000000
      *>   FLT  10**0.25  = 1.7782794..  -> +0001.778279
      *>   EXPR 10**-0.5  = 0.3162277..  -> +0000.316228
      *>   CD   10**1     = 10           -> +0010.000000
      *> A leg fails if its argument shape is refused (the class
      *> check misfiring on a legal operand), or if the value of that
      *> shape is not the one the function receives (scale, sign,
      *> packed, national, binary or float decoding lost).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXT01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-ONE   PIC S9(4) COMP VALUE 1.
       01  W-NEG   PIC S9 VALUE -1.
       01  W-HALF  PIC 9V9 VALUE 0.5.
       01  W-PK    PIC S9(3)V99 USAGE PACKED-DECIMAL VALUE -2.00.
       01  W-NU    PIC S9V9 USAGE NATIONAL VALUE 1.5.
       01  W-BC    USAGE BINARY-CHAR VALUE 3.
       01  W-FL    USAGE FLOAT-LONG.
       01  R       PIC +9(4).9(6).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 0.25 TO W-FL
           COMPUTE R ROUNDED = FUNCTION EXP10(2)
           DISPLAY "LIT=" R
           COMPUTE R ROUNDED = FUNCTION EXP10(0)
           DISPLAY "ZERO=" R
           COMPUTE R ROUNDED = FUNCTION EXP10(W-NEG)
           DISPLAY "NEG=" R
           COMPUTE R ROUNDED = FUNCTION EXP10(W-HALF)
           DISPLAY "HALF=" R
           COMPUTE R ROUNDED = FUNCTION EXP10(W-PK)
           DISPLAY "PACK=" R
           COMPUTE R ROUNDED = FUNCTION EXP10(W-NU)
           DISPLAY "NAT=" R
           COMPUTE R ROUNDED = FUNCTION EXP10(W-BC)
           DISPLAY "BCHR=" R
           COMPUTE R ROUNDED = FUNCTION EXP10(W-FL)
           DISPLAY "FLT=" R
           COMPUTE R ROUNDED = FUNCTION EXP10(W-ONE - W-HALF * 3)
           DISPLAY "EXPR=" R
           MOVE 1 TO IN-COUNT
           COMPUTE R ROUNDED = FUNCTION EXP10(IN-COUNT)
           DISPLAY "CD=" R
           STOP RUN.
