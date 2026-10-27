*>--- conformance-case
*> id: pb3839_numval_f_one_value
*> rules: [RV-15.69.4-2, GR-14.7.4.3-2, GR-14.7.4.3-3, GR-14.7.4.3-4, GR-14.7.4.3-5, GR-14.7.4.3-6, GR-14.7.4.3-8,
*>     GR-14.7.4.3-9, GR-14.7.4.3-10]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB3839 - NUMVAL-F HAS ONE VALUE, WHATEVER READS IT. 15.69.4 r2 makes the native returned value "an
      *> approximation of the numeric value represented by argument-1" and 15.4.1 leaves "the characteristics and
      *> representation of the returned value" to the implementor, so the native DIGITS are the implementor's
      *> choice; WiseOwl COBOL's documented determination (docs/CONFORMANCE.md, item 92) is the exact parse,
      *> which is also the value 15.69.4 r3 requires under standard-decimal arithmetic. The expected values below
      *> are therefore DERIVED, not observed: the exact decimal value of the argument, rounded ONCE by the rule of
      *> the receiving statement - 14.7.4.1 puts the rounding "relative to the size provided for the resultant
      *> identifier" and 14.7.4.3 r2 makes a statement with no ROUNDED phrase TRUNCATION. Before PB3839 a
      *> fixed-point receiver was handed the parse truncated at max(receiver scale, 9) places and then rounded it
      *> AGAIN, a binary64 went to DISPLAY, a relation and a COMP-2, and so ROUNDED MODE AWAY-FROM-ZERO of
      *> NUMVAL-F("0.12340000000001") into a PIC 9V9(4) stored 0.1234 where NUMVAL (whose value 15.67.4 r1 fixes)
      *> stored 0.1235.
      *>
      *> Each leg fails on the old compiler for the reason named: A* (scale 4, every rounding mode and both
      *> signs) - the 9-place truncation; B* (scale 0, 9, 12 and a trailing-P receiver) - the same truncation at a
      *> receiver whose scale is at or above the floor; C* (an operand) - the value inside an expression;
      *> D* (DISPLAY) - the binary64 image where the value has more than 17 digits or an exponent; E* - a relation
      *> between two values 10**-20 apart, equal in binary64; F* - the COMP-2 landings, which must agree with the
      *> decimal literal of the same value (a MOVE rounds to nearest, COMPUTE with no ROUNDED phrase truncates -
      *> CONFORMANCE.md item 81 - and the function must do exactly what the literal does).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3839NVF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R4   PIC S9V9(4)  SIGN LEADING SEPARATE.
       01 R0   PIC S9(3)    SIGN LEADING SEPARATE.
       01 R9   PIC S9V9(9)  SIGN LEADING SEPARATE.
       01 R12  PIC S9V9(12) SIGN LEADING SEPARATE.
       01 RP   PIC S9(2)P(2) SIGN LEADING SEPARATE.
       01 F1   COMP-2.
       01 F2   COMP-2.
       01 F3   COMP-2.
       01 F4   COMP-2.
       PROCEDURE DIVISION.
      *> A: scale 4, +0.12340000000001 and -0.12340000000001, every rounding mode.
           COMPUTE R4 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION NUMVAL-F("0.12340000000001").
           DISPLAY "A1=" R4.
           COMPUTE R4 ROUNDED MODE NEAREST-AWAY-FROM-ZERO = FUNCTION NUMVAL-F("0.12340000000001").
           DISPLAY "A2=" R4.
           COMPUTE R4 ROUNDED MODE NEAREST-EVEN = FUNCTION NUMVAL-F("0.12340000000001").
           DISPLAY "A3=" R4.
           COMPUTE R4 ROUNDED MODE NEAREST-TOWARD-ZERO = FUNCTION NUMVAL-F("0.12340000000001").
           DISPLAY "A4=" R4.
           COMPUTE R4 ROUNDED MODE TOWARD-GREATER = FUNCTION NUMVAL-F("0.12340000000001").
           DISPLAY "A5=" R4.
           COMPUTE R4 ROUNDED MODE TOWARD-LESSER = FUNCTION NUMVAL-F("0.12340000000001").
           DISPLAY "A6=" R4.
           COMPUTE R4 ROUNDED MODE TRUNCATION = FUNCTION NUMVAL-F("0.12340000000001").
           DISPLAY "A7=" R4.
           COMPUTE R4 = FUNCTION NUMVAL-F("0.12340000000001").
           DISPLAY "A8=" R4.
           COMPUTE R4 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION NUMVAL-F("-0.12340000000001").
           DISPLAY "A9=" R4.
           COMPUTE R4 ROUNDED MODE TOWARD-GREATER = FUNCTION NUMVAL-F("-0.12340000000001").
           DISPLAY "A10=" R4.
           COMPUTE R4 ROUNDED MODE TOWARD-LESSER = FUNCTION NUMVAL-F("-0.12340000000001").
           DISPLAY "A11=" R4.
           COMPUTE R4 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION NUMVAL-F("1.2340000000001E-1").
           DISPLAY "A12=" R4.
      *> B: scale 0, 9, 12 and a trailing-P receiver (a digit position left of the point).
           COMPUTE R0 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION NUMVAL-F("12.0000000001").
           DISPLAY "B1=" R0.
           COMPUTE R9 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION NUMVAL-F("0.1234567890001").
           DISPLAY "B2=" R9.
           COMPUTE R12 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION NUMVAL-F("0.1234567890123456").
           DISPLAY "B3=" R12.
           COMPUTE RP ROUNDED MODE AWAY-FROM-ZERO = FUNCTION NUMVAL-F("1200.00000000001").
           DISPLAY "B4=" RP.
      *> C: the same reference as an operand of an expression.
           COMPUTE R4 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION NUMVAL-F("0.12340000000001") + 0.
           DISPLAY "C1=" R4.
           COMPUTE R4 ROUNDED MODE AWAY-FROM-ZERO = FUNCTION NUMVAL-F("0.12340000000001") * 1.
           DISPLAY "C2=" R4.
      *> D: DISPLAY, the item-92 text form of the exact value at its own scale.
           DISPLAY "D1=" FUNCTION NUMVAL-F("0.12340000000001").
           DISPLAY "D2=" FUNCTION NUMVAL-F("5E+30").
           DISPLAY "D3=" FUNCTION NUMVAL-F("1.5E-12").
           DISPLAY "D4=" FUNCTION NUMVAL-F("-3.25").
           DISPLAY "D5=" FUNCTION NUMVAL-F("12345678901234567890E-20").
      *> E: two values 10**-20 apart are different values, and NUMVAL-F agrees with NUMVAL (15.67.4 r1).
           IF FUNCTION NUMVAL-F("0.12345678901234567891") = FUNCTION NUMVAL-F("0.12345678901234567892")
               DISPLAY "E1=EQUAL"
           ELSE
               DISPLAY "E1=DIFFERENT"
           END-IF.
           IF FUNCTION NUMVAL-F("0.12345678901234567891") < FUNCTION NUMVAL-F("0.12345678901234567892")
               DISPLAY "E2=LESS"
           ELSE
               DISPLAY "E2=NOT-LESS"
           END-IF.
           IF FUNCTION NUMVAL-F("0.12340000000001") = FUNCTION NUMVAL("0.12340000000001")
               DISPLAY "E3=EQUAL"
           ELSE
               DISPLAY "E3=DIFFERENT"
           END-IF.
      *> F: a COMP-2 receives what the decimal literal of the same value is given, statement by statement.
           MOVE FUNCTION NUMVAL-F("0.12340000000001") TO F1.
           MOVE 0.12340000000001 TO F2.
           COMPUTE F3 = FUNCTION NUMVAL-F("0.12340000000001").
           COMPUTE F4 = 0.12340000000001.
           IF F1 = F2
               DISPLAY "F1=SAME"
           ELSE
               DISPLAY "F1=DIFFERENT"
           END-IF.
           IF F3 = F4
               DISPLAY "F2=SAME"
           ELSE
               DISPLAY "F2=DIFFERENT"
           END-IF.
           STOP RUN.
       END PROGRAM PB3839NVF.
