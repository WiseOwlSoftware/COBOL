      *>--- conformance-case
      *> id: g85_bf04_03
      *> rules: [1985-GR-IV.4.3.8.3.4-1, 1985-GR-IV.4.3.8.3.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.3.4 GR1 - the function definition fixes the
      *> class
      *> §1985:IV.4.3.8.3.4 GR2 - arguments left to right, nesting
      * GR1 lines (UC, UCLEN, MAX, MAXLEN, MOD, NUMVAL, IMAX): UPPER-
      * CASE is alphanumeric of the argument's length (A.2.44): [ABCDE
      * ], 5. MAX of alphanumeric arguments is alphanumeric and "the
      * size of the returned value is the same as the size of the
      * selected argument-1" (A.2.23.4 RV3): "ZZ" > "ABC" ("ZZ " against
      * "ABC"), so [ZZ ...] and length 2 (a fixed-size result answers
      * 3). MOD is integer: 17 mod 5 + 1 = 3. NUMVAL is numeric: 12.50 *
      * 2 = 25.00. MAX of all-integer arguments is integer: +0007. GR2
      * lines: NEST, REV2 and EXPR evaluate an argument that is itself a
      * function-identifier, the same function (MAX(MAX(1 9) 4) = 9,
      * REVERSE(REVERSE("ABC")) = ABC) and an expression containing one
      * (MOD(LENGTH("ABCDEFG") + 3, 4) = 10 mod 4 = 2). ORDER pins the
      * left-to-right order through RANDOM's state (A.2.35.3 AR4,
      * A.2.35.4 RV2): after RANDOM(11), MEAN(RANDOM(7) RANDOM RANDOM)
      * evaluated left to right sees the first three numbers of seed 7,
      * the same three W-A, W-B, W-C get below, so the means agree.
      * Right to left, the two seedless references continue seed 11's
      * sequence and only one number comes from seed 7: ORDER=OTHER.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0403.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X10 PIC X(10).
       01 W-N4 PIC 9(4).
       01 W-D PIC 99V99.
       01 W-DE PIC Z9.99.
       01 W-S PIC S9(4) SIGN LEADING SEPARATE.
       01 W-R0 PIC V9(15).
       01 W-A PIC V9(15).
       01 W-B PIC V9(15).
       01 W-C PIC V9(15).
       01 W-M1 PIC V9(15).
       01 W-M2 PIC V9(15).
       01 W-DF PIC S9V9(15).
       PROCEDURE DIVISION.
       P1.
           MOVE FUNCTION UPPER-CASE ("abcde") TO W-X10.
           DISPLAY "UC=[" W-X10 "]".
           COMPUTE W-N4 =
               FUNCTION LENGTH (FUNCTION UPPER-CASE ("abcde")).
           DISPLAY "UCLEN=" W-N4.
           MOVE FUNCTION MAX ("ZZ" "ABC") TO W-X10.
           DISPLAY "MAX=[" W-X10 "]".
           COMPUTE W-N4 = FUNCTION LENGTH (FUNCTION MAX ("ZZ" "ABC")).
           DISPLAY "MAXLEN=" W-N4.
           COMPUTE W-N4 = FUNCTION MOD (17 5) + 1.
           DISPLAY "MOD=" W-N4.
           COMPUTE W-D = FUNCTION NUMVAL ("12.50") * 2.
           MOVE W-D TO W-DE.
           DISPLAY "NUMVAL=" W-DE.
           COMPUTE W-S = FUNCTION MAX (3 -8 7).
           DISPLAY "IMAX=" W-S.
           COMPUTE W-S = FUNCTION MAX (FUNCTION MAX (1 9) 4).
           DISPLAY "NEST=" W-S.
           MOVE FUNCTION REVERSE (FUNCTION REVERSE ("ABC")) TO W-X10.
           DISPLAY "REV2=[" W-X10 "]".
           COMPUTE W-N4 =
               FUNCTION MOD (FUNCTION LENGTH ("ABCDEFG") + 3, 4).
           DISPLAY "EXPR=" W-N4.
           COMPUTE W-R0 = FUNCTION RANDOM (11).
           COMPUTE W-M1 = FUNCTION MEAN (FUNCTION RANDOM (7)
               FUNCTION RANDOM FUNCTION RANDOM).
           COMPUTE W-A = FUNCTION RANDOM (7).
           COMPUTE W-B = FUNCTION RANDOM.
           COMPUTE W-C = FUNCTION RANDOM.
           COMPUTE W-M2 = FUNCTION MEAN (W-A W-B W-C).
           COMPUTE W-DF = W-M1 - W-M2.
           IF W-DF < 0.0000001 AND W-DF > -0.0000001
               DISPLAY "ORDER=LEFT-TO-RIGHT"
           ELSE
               DISPLAY "ORDER=OTHER"
           END-IF.
           STOP RUN.
