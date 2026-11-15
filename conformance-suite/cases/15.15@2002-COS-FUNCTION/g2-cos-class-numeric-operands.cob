      *>--- conformance-case
      *> id: g2-cos-class-numeric-operands
      *> rules: [2002-AR-15.15.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.15.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.2 type 9 - "An arithmetic expression or a numeric
      *> data item shall be specified." Every leg writes a
      *> class-numeric argument-1 of a different shape (literal,
      *> binary item, signed display item, scaled item, packed-decimal
      *> item, arithmetic expression, and the MESSAGE COUNT item of a
      *> 2002 input CD, an unsigned 6-digit integer by §2002:A.1.2.3
      *> GR2k), and each must be ACCEPTED and evaluated.
      *> §2002:15.15.3 RV1 - "the approximation of the cosine of
      *> argument-1"; COMPUTE ROUNDED into +9.9(6) gives, for any
      *> approximation within 5E-7 of the true value:
      *>   ZERO cos(0)   = 1            -> +1.000000
      *>   ONE  cos(1)   = 0.54030230.. -> +0.540302
      *>   NEG  cos(-1)  = 0.54030230.. -> +0.540302
      *>   HALF cos(0.5) = 0.87758256.. -> +0.877583
      *>   PACK cos(-2)  = -0.4161468.. -> -0.416147
      *>   EXPR cos(1+2) = -0.9899924.. -> -0.989992
      *>   CD   cos(4)   = -0.6536436.. -> -0.653644
      *> A leg fails if its argument shape is refused, or if the value
      *> the function receives loses scale, sign or packed decoding.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2COS01.
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
           COMPUTE R ROUNDED = FUNCTION COS(0)
           DISPLAY "ZERO=" R
           COMPUTE R ROUNDED = FUNCTION COS(W-ONE)
           DISPLAY "ONE=" R
           COMPUTE R ROUNDED = FUNCTION COS(W-NEG)
           DISPLAY "NEG=" R
           COMPUTE R ROUNDED = FUNCTION COS(W-HALF)
           DISPLAY "HALF=" R
           COMPUTE R ROUNDED = FUNCTION COS(W-PK)
           DISPLAY "PACK=" R
           COMPUTE R ROUNDED = FUNCTION COS(W-ONE + 2)
           DISPLAY "EXPR=" R
           MOVE 4 TO IN-COUNT
           COMPUTE R ROUNDED = FUNCTION COS(IN-COUNT)
           DISPLAY "CD=" R
           STOP RUN.
