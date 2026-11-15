      *>--- conformance-case
      *> id: g2-atan-class-numeric-operands
      *> rules: [2002-AR-15.10.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.10.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.2 type 9 - "An arithmetic expression or a numeric
      *> data item shall be specified." Every leg below writes a
      *> class-numeric argument-1 of a different shape (literal,
      *> signed display item, scaled item, packed-decimal item,
      *> arithmetic expression, and the MESSAGE COUNT item of a 2002
      *> input CD, an unsigned 6-digit integer by §2002:A.1.2.3 GR2k),
      *> and each must be ACCEPTED and evaluated.
      *> §2002:15.10.3 RV1 - "the approximation of the arctangent of
      *> argument-1"; each value is COMPUTEd ROUNDED into +9.9(6), so
      *> any approximation within 5E-7 of the true value gives:
      *>   LIT  atan(1)    = 0.78539816.. -> +0.785398
      *>   ZERO atan(0)    = 0            -> +0.000000
      *>   NEG  atan(-1)   = -0.7853981.. -> -0.785398
      *>   HALF atan(0.5)  = 0.46364760.. -> +0.463648
      *>   PACK atan(-2)   = -1.1071487.. -> -1.107149
      *>   EXPR atan(1+.5) = 0.98279372.. -> +0.982794
      *>   CD   atan(3)    = 1.24904577.. -> +1.249046
      *> A leg fails if its argument shape is refused (the COBOL1627
      *> class check misfiring on a legal operand), or if the value
      *> of that shape is not the one the function receives (scale,
      *> sign or packed decoding lost on the way in).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ATN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-ONE   PIC S9(4) COMP VALUE 1.
       01  W-NEG   PIC S9 VALUE -1.
       01  W-HALF  PIC 9V9 VALUE 0.5.
       01  W-PK    PIC S9(3)V99 USAGE PACKED-DECIMAL VALUE -2.00.
       01  R       PIC +9.9(6).
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R ROUNDED = FUNCTION ATAN(1)
           DISPLAY "LIT=" R
           COMPUTE R ROUNDED = FUNCTION ATAN(0)
           DISPLAY "ZERO=" R
           COMPUTE R ROUNDED = FUNCTION ATAN(W-NEG)
           DISPLAY "NEG=" R
           COMPUTE R ROUNDED = FUNCTION ATAN(W-HALF)
           DISPLAY "HALF=" R
           COMPUTE R ROUNDED = FUNCTION ATAN(W-PK)
           DISPLAY "PACK=" R
           COMPUTE R ROUNDED = FUNCTION ATAN(W-ONE + W-HALF)
           DISPLAY "EXPR=" R
           MOVE 3 TO IN-COUNT
           COMPUTE R ROUNDED = FUNCTION ATAN(IN-COUNT)
           DISPLAY "CD=" R
           STOP RUN.
