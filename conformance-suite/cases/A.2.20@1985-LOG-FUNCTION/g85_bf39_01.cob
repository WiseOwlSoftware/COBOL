      *>--- conformance-case
      *> id: g85_bf39_01
      *> rules: [1989-AR-A.2.20.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.20.3 AR2 - LOG with arguments that satisfy
      *> "The value of argument-1 must be greater than zero.": a
      *> data item 1, a data item 10, a fraction 0.5 and an arithmetic
      *> expression 2.5 * 4. Each is > 0, so a 1985 compiler must
      *> accept and evaluate every reference (§1989:A.2.20.4 RV1: the
      *> approximation of the logarithm to the base e). Rounded to 4
      *> places: ln 1 = 0.0000, ln 10 = 2.3026 (2.302585...),
      *> ln 0.5 = -0.6931 (-0.693147...). A compiler that refuses a
      *> legal positive argument, or that returns a wrong value or
      *> sign for a fraction, fails a line. LOG is numeric, so it is
      *> used only in an arithmetic expression (§1985:IV.4.3.8.3.3 SR4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3901.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-ONE  PIC 9 VALUE 1.
       01 W-TEN  PIC 99 VALUE 10.
       01 W-HALF PIC 9V9 VALUE 0.5.
       01 W-R    PIC S9V9(4).
       01 W-E    PIC -9.9999.
       PROCEDURE DIVISION.
       P1.
           COMPUTE W-R ROUNDED = FUNCTION LOG (W-ONE).
           MOVE W-R TO W-E.
           DISPLAY "LOG-1=" W-E.
           COMPUTE W-R ROUNDED = FUNCTION LOG (W-TEN).
           MOVE W-R TO W-E.
           DISPLAY "LOG-10=" W-E.
           COMPUTE W-R ROUNDED = FUNCTION LOG (W-HALF).
           MOVE W-R TO W-E.
           DISPLAY "LOG-HALF=" W-E.
           COMPUTE W-R ROUNDED = FUNCTION LOG (2.5 * 4).
           MOVE W-R TO W-E.
           DISPLAY "LOG-EXPR=" W-E.
           STOP RUN.
