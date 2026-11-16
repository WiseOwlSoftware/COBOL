      *>--- conformance-case
      *> id: g2-integer-part-standard-arithmetic
      *> rules: [2002-RV-15.38.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.38.3 RV1 - "The equivalent arithmetic expression is:
      *> (FUNCTION SIGN (argument-1) * FUNCTION INTEGER (FUNCTION ABS
      *> (argument-1))) where the arguments for the SIGN and ABS
      *> functions are the same as for the INTEGER-PART function
      *> itself." Under native arithmetic the value is "an
      *> implementor-defined approximation" of that expression
      *> (§2002:15.3.1), so this program runs under ARITHMETIC IS
      *> STANDARD (§2002:8.8.1.3), where "the returned value shall equal
      *> the value of the equivalent arithmetic expression"
      *> (§2002:15.3.1). Each value, edited by +9(3) (BIG by +9(30)):
      *>   M15  -1.5   NOTE: "the value -1 is returned"      -> -001
      *>   P15  +1.5   NOTE: "the value +1 is returned"      -> +001
      *>   ZERO 0      NOTE: "the value zero is returned"    -> +000
      *>   M10  -1.0   NOTE: "the value -1 is returned"      -> -001
      *>   P10  +1.0   NOTE: "the value +1 is returned"      -> +001
      *>   PACK -98.76 packed: -1 * INTEGER(98.76)            -> -098
      *>   EXPR -19 / 4 = -4.75: -1 * INTEGER(4.75) = -4 (the INTEGER
      *>        function itself would give -5)               -> -004
      *>   BIG  -123456789012345678901234567890.7 (31 digits, inside the
      *>        32-digit standard intermediate)
      *>                         -> -123456789012345678901234567890
      *>   EQ   the relation INTEGER-PART(X) = (SIGN(X) *
      *>        INTEGER(ABS(X))) for X = -7.25 is true
      *>        (§2002:15.3.1 NOTE)                           -> EQ=Y
      *> A leg fails if the function floors a negative argument (M15,
      *> PACK and EXPR would be one lower), loses the sign or packed
      *> decoding, or carries the 31-digit argument in binary64.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INP01.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-M15    PIC S9V9 VALUE -1.5.
       01  W-P15    PIC S9V9 VALUE +1.5.
       01  W-ZERO   PIC S9V9 VALUE ZERO.
       01  W-M10    PIC S9V9 VALUE -1.0.
       01  W-P10    PIC S9V9 VALUE +1.0.
       01  W-PACK   PIC S9(3)V99 USAGE PACKED-DECIMAL VALUE -98.76.
       01  W-BIG    PIC S9(30)V9
                    VALUE -123456789012345678901234567890.7.
       01  W-EQ     PIC S9(3)V99 VALUE -7.25.
       01  R        PIC +9(3).
       01  RB       PIC +9(30).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R = FUNCTION INTEGER-PART(W-M15)
           DISPLAY "M15=" R
           COMPUTE R = FUNCTION INTEGER-PART(W-P15)
           DISPLAY "P15=" R
           COMPUTE R = FUNCTION INTEGER-PART(W-ZERO)
           DISPLAY "ZERO=" R
           COMPUTE R = FUNCTION INTEGER-PART(W-M10)
           DISPLAY "M10=" R
           COMPUTE R = FUNCTION INTEGER-PART(W-P10)
           DISPLAY "P10=" R
           COMPUTE R = FUNCTION INTEGER-PART(W-PACK)
           DISPLAY "PACK=" R
           COMPUTE R = FUNCTION INTEGER-PART(-19 / 4)
           DISPLAY "EXPR=" R
           COMPUTE RB = FUNCTION INTEGER-PART(W-BIG)
           DISPLAY "BIG=" RB
           IF FUNCTION INTEGER-PART(W-EQ) =
              (FUNCTION SIGN (W-EQ) *
               FUNCTION INTEGER (FUNCTION ABS (W-EQ)))
               DISPLAY "EQ=Y" ELSE DISPLAY "EQ=N" END-IF
           STOP RUN.
