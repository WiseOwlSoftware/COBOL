*>--- conformance-case
*> id: pb3951_length_of_numeric_function_result
*> rules: [RV-15.50.4-3, RV-15.14.4-1, DOC-A.1-92]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3951 (DOC-A.1-92). ISO §8.4.3.2.1 makes a function-identifier the data item the evaluation of the function
      *> produces, §15.4 puts the returned value in a temporary elementary data item, and §15.50.4 r3 / §15.14.4 r1 measure a
      *> data item that is neither boolean nor national "in alphanumeric character positions" / "in number of bytes". §15.4.1
      *> leaves the temporary's characteristics and representation to the implementor under native arithmetic, so CLAUDE.md rule 1's
      *> precedence gives them to GnuCOBOL 3.2.0, and the temporary is the ONE item DISPLAY shows (pb3514_function_result_device_form*):
      *> LENGTH and BYTE-LENGTH measure it, and the answers below are its sizes. The first implementation measured a fixed
      *> S9(30) item and answered 30 for every numeric function.
      *> (1) ABS, MAX, MIN, an odd count's MEDIAN and a single argument's MEAN return the CONTENT of an argument, so the temporary
      *>     is a COPY of that argument's item and has its bytes: 7 for S9(5)V99 DISPLAY, 8 with a SIGN SEPARATE, 2 for S9(4) COMP,
      *>     5 for S9(7)V99 COMP-3, 4 and 8 for COMP-1 and COMP-2; a literal's field has a position for every digit written and one
      *>     for a sign written (-12.50 is 5, 12.50 is 4); a nested function hands its caller the temporary it made.
      *> (2) DATE-OF-INTEGER is an unsigned DISPLAY integer of eight digits, DAY-OF-INTEGER of seven.
      *> (3) Every other exact function is the item its VALUE describes: a value under 2**31 (2**32 unsigned) with at most 9
      *>     fraction digits is a 4-byte item, one under 2**63 (2**64) with at most 18 fraction digits an 8-byte item, any other the
      *>     DISPLAY item of its digits. So INTEGER(S) is 4, NUMVAL("-2147483648") 8, NUMVAL("4294967295") 4, NUMVAL("4294967296") 8,
      *>     NUMVAL("12345678901234567890123") 23. BYTE-LENGTH is the same number.
      *> Each line fails on a fixed-width answer, and the argument-copy lines fail on any width that is not the argument's own.
      *> Every line is identical to cobc 3.2.0 (the lines it differs on are pb3951_length_of_numeric_function_result_not_copied).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3951A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S   PIC S9(5)V99 VALUE -12.5.
       01 SEP PIC S9(5)V99 SIGN IS TRAILING SEPARATE VALUE -12.5.
       01 U   PIC 9(5)V99 VALUE 12.5.
       01 I1  PIC 9(3) VALUE 7.
       01 I2  PIC S9(3) VALUE -14.
       01 C4  PIC S9(4) COMP VALUE -3.
       01 C18 PIC S9(18) COMP VALUE -3.
       01 PK  PIC S9(7)V99 COMP-3 VALUE 5.25.
       01 PU  PIC 9(5) COMP-3 VALUE 5.
       01 F1  COMP-1 VALUE -1.5.
       01 F2  COMP-2 VALUE -1.5.
       01 BIG PIC 9(18) VALUE 123456789012345678.
       01 TB.
          05 TBE PIC S9(3)V9 OCCURS 3 VALUE 1.5.
       PROCEDURE DIVISION.
           DISPLAY "ABS-S=" FUNCTION LENGTH(FUNCTION ABS(S))
           DISPLAY "ABS-U=" FUNCTION LENGTH(FUNCTION ABS(U))
           DISPLAY "ABS-SEP=" FUNCTION LENGTH(FUNCTION ABS(SEP))
           DISPLAY "ABS-C4=" FUNCTION LENGTH(FUNCTION ABS(C4))
           DISPLAY "ABS-C18=" FUNCTION LENGTH(FUNCTION ABS(C18))
           DISPLAY "ABS-PK=" FUNCTION LENGTH(FUNCTION ABS(PK))
           DISPLAY "ABS-PU=" FUNCTION LENGTH(FUNCTION ABS(PU))
           DISPLAY "ABS-F1=" FUNCTION LENGTH(FUNCTION ABS(F1))
           DISPLAY "ABS-F2=" FUNCTION LENGTH(FUNCTION ABS(F2))
           DISPLAY "ABS-BIG=" FUNCTION LENGTH(FUNCTION ABS(BIG))
           DISPLAY "ABS-LIT-SIGNED=" FUNCTION LENGTH(FUNCTION ABS(-12.50))
           DISPLAY "ABS-LIT=" FUNCTION LENGTH(FUNCTION ABS(12.50))
           DISPLAY "ABS-LIT-ZEROS=" FUNCTION LENGTH(FUNCTION ABS(+007))
           DISPLAY "ABS-EXPR=" FUNCTION LENGTH(FUNCTION ABS(I1 + I2))
           DISPLAY "ABS-ABS=" FUNCTION LENGTH(FUNCTION ABS(FUNCTION ABS(PK)))
           DISPLAY "ABS-INTEGER=" FUNCTION LENGTH(FUNCTION ABS(FUNCTION INTEGER(S)))
           DISPLAY "MAX-S-U=" FUNCTION LENGTH(FUNCTION MAX(S U))
           DISPLAY "MAX-PK-S=" FUNCTION LENGTH(FUNCTION MAX(PK S))
           DISPLAY "MIN-PK-S=" FUNCTION LENGTH(FUNCTION MIN(PK S))
           DISPLAY "MAX-I=" FUNCTION LENGTH(FUNCTION MAX(I1 I2))
           DISPLAY "MAX-LITERALS=" FUNCTION LENGTH(FUNCTION MAX(5 12.50 007))
           DISPLAY "MAX-TABLE=" FUNCTION LENGTH(FUNCTION MAX(TBE(ALL)))
           DISPLAY "MAX-NESTED=" FUNCTION LENGTH(FUNCTION MAX(FUNCTION ABS(S) 3))
           DISPLAY "MEDIAN-ODD=" FUNCTION LENGTH(FUNCTION MEDIAN(I1 I2 7))
           DISPLAY "MEDIAN-EVEN=" FUNCTION LENGTH(FUNCTION MEDIAN(I1 I2))
           DISPLAY "MEAN-ONE=" FUNCTION LENGTH(FUNCTION MEAN(S))
           DISPLAY "MEAN-TWO=" FUNCTION LENGTH(FUNCTION MEAN(S U))
           DISPLAY "DATE-OF-INTEGER=" FUNCTION LENGTH(FUNCTION DATE-OF-INTEGER(100))
           DISPLAY "DAY-OF-INTEGER=" FUNCTION LENGTH(FUNCTION DAY-OF-INTEGER(100))
           DISPLAY "SUM=" FUNCTION LENGTH(FUNCTION SUM(I1 I2))
           DISPLAY "INTEGER=" FUNCTION LENGTH(FUNCTION INTEGER(S))
           DISPLAY "INTEGER-PART=" FUNCTION LENGTH(FUNCTION INTEGER-PART(S))
           DISPLAY "MOD=" FUNCTION LENGTH(FUNCTION MOD(I1 3))
           DISPLAY "FACTORIAL=" FUNCTION LENGTH(FUNCTION FACTORIAL(5))
           DISPLAY "NUMVAL-31=" FUNCTION LENGTH(FUNCTION NUMVAL("2147483647"))
           DISPLAY "NUMVAL-2-31=" FUNCTION LENGTH(FUNCTION NUMVAL("-2147483648"))
           DISPLAY "NUMVAL-2-32M=" FUNCTION LENGTH(FUNCTION NUMVAL("4294967295"))
           DISPLAY "NUMVAL-2-32=" FUNCTION LENGTH(FUNCTION NUMVAL("4294967296"))
           DISPLAY "NUMVAL-2-64=" FUNCTION LENGTH(FUNCTION NUMVAL("18446744073709551616"))
           DISPLAY "NUMVAL-FRAC9=" FUNCTION LENGTH(FUNCTION NUMVAL("0.123456789"))
           DISPLAY "NUMVAL-FRAC10=" FUNCTION LENGTH(FUNCTION NUMVAL("0.1234567891"))
           DISPLAY "NUMVAL-WIDE=" FUNCTION LENGTH(FUNCTION NUMVAL("12345678901234567890123"))
           DISPLAY "NUMVAL-ZEROS=" FUNCTION LENGTH(FUNCTION NUMVAL("12.50"))
           DISPLAY "BYTE-ABS=" FUNCTION BYTE-LENGTH(FUNCTION ABS(S))
           DISPLAY "BYTE-MAX=" FUNCTION BYTE-LENGTH(FUNCTION MAX(PK S))
           DISPLAY "BYTE-WIDE=" FUNCTION BYTE-LENGTH(FUNCTION NUMVAL("12345678901234567890123"))
           DISPLAY "BYTE-DATE=" FUNCTION BYTE-LENGTH(FUNCTION DATE-OF-INTEGER(100))
           STOP RUN.
