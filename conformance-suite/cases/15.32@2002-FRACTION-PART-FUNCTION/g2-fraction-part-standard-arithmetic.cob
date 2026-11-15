      *>--- conformance-case
      *> id: g2-fraction-part-standard-arithmetic
      *> rules: [2002-RV-15.32.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.32.3 RV1 - "The equivalent arithmetic expression is:
      *> (argument-1 - FUNCTION INTEGER-PART (argument-1)) where the
      *> argument for the INTEGER-PART function is the same as for the
      *> FRACTION-PART function itself." Under native arithmetic the
      *> value is "an implementor-defined approximation" of that
      *> expression (§2002:15.3.1), so this program runs under
      *> ARITHMETIC IS STANDARD (§2002:8.8.1.3 - "Standard arithmetic is
      *> in effect when the ARITHMETIC IS STANDARD clause is specified
      *> in the OPTIONS paragraph"), where "the returned value shall
      *> equal the value of the equivalent arithmetic expression"
      *> (§2002:15.3.1). Each value, edited by +9.9(6):
      *>   P15  +1.5        NOTE: "+0.5 is returned"       -> +0.500000
      *>   M15  -1.5        NOTE: "-0.5 is returned"       -> -0.500000
      *>   DEC  -123.4567   -123.4567 - (-123)              -> -0.456700
      *>   INT  42          42 - 42 = 0                     -> +0.000000
      *>   PACK 7.75 packed 7.75 - 7                        -> +0.750000
      *>   EXPR 10 / 4      2.5 - 2 (an expression argument)-> +0.500000
      *>   BIG  -1234567890123456789012345.678901 (31 digits, inside
      *>        the 32-digit standard intermediate)          -> -0.678901
      *>   EQ   the relation FRACTION-PART(X) = (X - INTEGER-PART(X))
      *>        for X = -7.25 is true (§2002:15.3.1 NOTE)    -> EQ=Y
      *> A leg fails if the fraction takes the floor's sign (M15 and
      *> DEC would give +0.5 and +0.5433), if scale or packed decoding
      *> is lost, or if the 31-digit argument is carried in binary64
      *> (BIG loses its low-order digits).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FRP01.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-P15    PIC S9V9 VALUE +1.5.
       01  W-M15    PIC S9V9 VALUE -1.5.
       01  W-DEC    PIC S9(3)V9(4) VALUE -123.4567.
       01  W-INT    PIC S9(4) VALUE 42.
       01  W-PACK   PIC S9(3)V99 USAGE PACKED-DECIMAL VALUE 7.75.
       01  W-BIG    PIC S9(25)V9(6)
                    VALUE -1234567890123456789012345.678901.
       01  W-EQ     PIC S9(3)V99 VALUE -7.25.
       01  R        PIC +9.9(6).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R = FUNCTION FRACTION-PART(W-P15)
           DISPLAY "P15=" R
           COMPUTE R = FUNCTION FRACTION-PART(W-M15)
           DISPLAY "M15=" R
           COMPUTE R = FUNCTION FRACTION-PART(W-DEC)
           DISPLAY "DEC=" R
           COMPUTE R = FUNCTION FRACTION-PART(W-INT)
           DISPLAY "INT=" R
           COMPUTE R = FUNCTION FRACTION-PART(W-PACK)
           DISPLAY "PACK=" R
           COMPUTE R = FUNCTION FRACTION-PART(10 / 4)
           DISPLAY "EXPR=" R
           COMPUTE R = FUNCTION FRACTION-PART(W-BIG)
           DISPLAY "BIG=" R
           IF FUNCTION FRACTION-PART(W-EQ) =
              (W-EQ - FUNCTION INTEGER-PART(W-EQ))
               DISPLAY "EQ=Y" ELSE DISPLAY "EQ=N" END-IF
           STOP RUN.
