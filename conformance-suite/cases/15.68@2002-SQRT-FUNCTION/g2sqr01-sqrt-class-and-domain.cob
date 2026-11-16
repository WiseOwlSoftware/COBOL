      *>--- conformance-case
      *> id: g2sqr01-sqrt-class-and-domain
      *> rules: [2002-AR-15.68.2-1, 2002-AR-15.68.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.68.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.68.2 AR2 - "The value of argument-1 shall be zero or
      *>   positive."
      *> No ARITHMETIC clause, so native arithmetic (§2002:11.8.4.2
      *> GR3) and §2002:15.68.3 RV3: "the absolute value of the
      *> approximation of the square root of argument-1", COMPUTEd
      *> ROUNDED into +9(2).9(6). Class-numeric shapes (AR1), each a
      *> zero or positive value (AR2), accepted and evaluated:
      *>   LIT SQRT(4) = +02.000000
      *>   ZERO SQRT(0) = +00.000000
      *>   ITEM SQRT(W-I) = +09.000000
      *>   SCALE SQRT(W-SC) = +01.500000
      *>   PACK SQRT(W-PK) = +02.500000
      *>   EXPR SQRT(W-I + 19) = +10.000000
      *>   NAT SQRT(W-NU) = +04.000000
      *>   CD SQRT(IN-COUNT) = +07.000000
      *>   IRR SQRT(2) = +01.414214
      *>   (W-I 81, W-SC 2.25, W-PK 6.25, W-NU PIC 9(3) USAGE
      *>   NATIONAL 16 - class numeric in 2002, kb/Work PB5625 B -
      *>   IN-COUNT 49, sqrt(2) = 1.41421356..)
      *> NEG - argument-1 = -4 violates AR2: an incorrect argument
      *>   value, so §2002:15.2 sets EC-ARGUMENT-FUNCTION; checking
      *>   is on, the declarative runs (§2002:14.8.45.3 GR3) and
      *>   records the exception-name (§2002:15.28.2 RV1); RESUME AT
      *>   NEXT STATEMENT returns after the COMPUTE (§2002:14.8.31.3
      *>   GR2): "Y EC-ARGUMENT-FUNCTION".
      *> Each legal leg also shows W-RAISED = N: zero and positive
      *> values raise nothing.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SQR01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-I   PIC S9(3) VALUE 81.
       01  W-SC  PIC 9V99 VALUE 2.25.
       01  W-PK  PIC S9(3)V99 USAGE PACKED-DECIMAL VALUE 6.25.
       01  W-NU  PIC 9(3) USAGE NATIONAL VALUE 16.
       01  W-NEG PIC S9 VALUE -4.
       01  R     PIC +9(2).9(6).
       01  W-RAISED PIC X VALUE "N".
       01  ES    PIC X(20) VALUE SPACES.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           MOVE "Y" TO W-RAISED
           MOVE FUNCTION EXCEPTION-STATUS TO ES
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       P-MAIN.
           MOVE 49 TO IN-COUNT
           COMPUTE R ROUNDED = FUNCTION SQRT(4)
           DISPLAY "LIT=" R " " W-RAISED
           COMPUTE R ROUNDED = FUNCTION SQRT(0)
           DISPLAY "ZERO=" R " " W-RAISED
           COMPUTE R ROUNDED = FUNCTION SQRT(W-I)
           DISPLAY "ITEM=" R " " W-RAISED
           COMPUTE R ROUNDED = FUNCTION SQRT(W-SC)
           DISPLAY "SCALE=" R " " W-RAISED
           COMPUTE R ROUNDED = FUNCTION SQRT(W-PK)
           DISPLAY "PACK=" R " " W-RAISED
           COMPUTE R ROUNDED = FUNCTION SQRT(W-I + 19)
           DISPLAY "EXPR=" R " " W-RAISED
           COMPUTE R ROUNDED = FUNCTION SQRT(W-NU)
           DISPLAY "NAT=" R " " W-RAISED
           COMPUTE R ROUNDED = FUNCTION SQRT(IN-COUNT)
           DISPLAY "CD=" R " " W-RAISED
           COMPUTE R ROUNDED = FUNCTION SQRT(2)
           DISPLAY "IRR=" R " " W-RAISED
           MOVE "N" TO W-RAISED
           MOVE SPACES TO ES
           COMPUTE R ROUNDED = FUNCTION SQRT(W-NEG)
           DISPLAY "NEG=" W-RAISED " " ES
           STOP RUN.
