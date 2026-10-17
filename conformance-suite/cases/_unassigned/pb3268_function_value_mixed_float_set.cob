*>--- conformance-case
*> id: pb3268_function_value_mixed_float_set
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3268 (review round 3, R1-1 and R1-2) - an EXACT function value moved to a set of
      *> receivers that mixes a floating-point item with a fixed-point one, or a binary32 item with a
      *> binary64 one. ISO 14.9.25.4 GR1 writes the single evaluation as MOVE a TO temp / MOVE temp TO b /
      *> MOVE temp TO c, "where 'temp' is an intermediate result item provided by the implementor", and
      *> 15.4.1 makes "the returned value is the same for all instances of a given function", so each
      *> receiver must store what the MOVE of the function to that ONE receiver stores. A float receiver
      *> keeps its significant digits at any magnitude, which no fixed-point window does: a 12-integer-digit
      *> window cut C below to 567890123456.5, and a 21.17 window stored 0 for 1.234E-21. Each line compares
      *> the several-receiver store with the one-receiver store of the same function (SAME), and shows the
      *> fixed-point receiver, which truncates its high-order digits (14.6.8.2 rule 4).
      *>
      *> M1  NUMVAL("1234567890123456.5") TO S C: C equals the one-receiver C; S holds 456.50 -> 45650.
      *> M2  SUM(A A), A = 617283945061728.25, is 1234567890123456.50: the same as M1.
      *> M3  NUMVAL("0.000000000000000000001234") TO H F (FLOAT-SHORT, FLOAT-LONG): each equals its
      *>     one-receiver store, and F is not zero.
      *> M4  NUMVAL("12345678901234567890123456") TO H F: each equals its one-receiver store.
      *> M5  INTEGER(1.5E40) TO N C: the integer 15 followed by 39 zeros; C equals the one-receiver C and
      *>     N (PIC 9(5)) holds its low-order five digits, 00000.
      *> M6  NUMVAL("1.0000000596046447753906251") TO H S: H equals the one-receiver H (one rounding into
      *>     binary32, the 14.6.8.3 rule 2 conversion kb/Work PB1110 applies to FLOAT-SHORT) and S holds
      *>     1.00 -> 00100.
      *> M7  NUMVAL("1.2345678901234567890123") TO E S, E PIC +9.9(20)E+99: E holds the value truncated
      *>     to 21 significand digits, +1.23456789012345678901E+00, and S holds 1.23 -> 00123.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3268MX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S  PIC 9(3)V99.
       01 N  PIC 9(5).
       01 C  USAGE FLOAT-LONG.
       01 CQ USAGE FLOAT-LONG.
       01 H  USAGE FLOAT-SHORT.
       01 HQ USAGE FLOAT-SHORT.
       01 F  USAGE FLOAT-LONG.
       01 FQ USAGE FLOAT-LONG.
       01 E  PIC +9.9(20)E+99.
       01 A  PIC 9(15)V99 VALUE 617283945061728.25.
       PROCEDURE DIVISION.
           MOVE FUNCTION NUMVAL ("1234567890123456.5") TO CQ
           MOVE FUNCTION NUMVAL ("1234567890123456.5") TO S C
           IF C = CQ DISPLAY "M1=SAME " S ELSE DISPLAY "M1=DIFFERENT " S END-IF
           MOVE ZERO TO S C
           MOVE FUNCTION SUM (A A) TO CQ
           MOVE FUNCTION SUM (A A) TO S C
           IF C = CQ DISPLAY "M2=SAME " S ELSE DISPLAY "M2=DIFFERENT " S END-IF
           MOVE FUNCTION NUMVAL ("0.000000000000000000001234") TO FQ
           MOVE FUNCTION NUMVAL ("0.000000000000000000001234") TO HQ
           MOVE FUNCTION NUMVAL ("0.000000000000000000001234") TO H F
           IF F = FQ AND H = HQ AND F NOT = 0
               DISPLAY "M3=SAME"
           ELSE
               DISPLAY "M3=DIFFERENT"
           END-IF
           MOVE FUNCTION NUMVAL ("12345678901234567890123456") TO FQ
           MOVE FUNCTION NUMVAL ("12345678901234567890123456") TO HQ
           MOVE FUNCTION NUMVAL ("12345678901234567890123456") TO H F
           IF F = FQ AND H = HQ DISPLAY "M4=SAME" ELSE DISPLAY "M4=DIFFERENT" END-IF
           MOVE 1 TO N
           MOVE FUNCTION INTEGER (1.5E40) TO CQ
           MOVE FUNCTION INTEGER (1.5E40) TO N C
           IF C = CQ DISPLAY "M5=SAME " N ELSE DISPLAY "M5=DIFFERENT " N END-IF
           MOVE FUNCTION NUMVAL ("1.0000000596046447753906251") TO HQ
           MOVE FUNCTION NUMVAL ("1.0000000596046447753906251") TO H S
           IF H = HQ DISPLAY "M6=SAME " S ELSE DISPLAY "M6=DIFFERENT " S END-IF
           MOVE FUNCTION NUMVAL ("1.2345678901234567890123") TO E S
           DISPLAY "M7=" E " " S
           STOP RUN.
