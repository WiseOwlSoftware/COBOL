*>--- conformance-case
*> id: pb3514_function_result_device_form
*> rules: [GR-14.9.11.4-1, RV-15.4.1-L2.2, DOC-A.1-92, DOC-A.1-56]
*> accept: [2002, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3514 (owner adjudication 2026-10-25, DOC-A.1-92): DISPLAY of a numeric or integer FUNCTION's returned value
      *> shows the value as the data item it is (ISO §15.4: "a temporary elementary data item"), by the description that item
      *> has. §15.4.1 leaves the description to the implementor under native arithmetic and §14.9.11.4 GR1 leaves the DISPLAY
      *> conversion to the implementor, so the precedence of CLAUDE.md rule 1 gives both to GnuCOBOL 3.2.0, whose temporary is
      *> one of three things, and DISPLAY then shows it as DISPLAY shows any numeric item (DOC-A.1-56):
      *>
      *> 1. A COPY OF AN ARGUMENT'S ITEM, for the functions that return the CONTENT of an argument: MAX (§15.59.4 r1), MIN
      *>    (§15.63.4 r1, the FIRST of equal values in both), MEDIAN of an odd count (§15.61) and MEAN of one argument
      *>    (§15.60). So MAX(U1 S1) is U1's item and shows what DISPLAY U1 shows, and a literal argument is a field of the
      *>    digits written (MAX(3 -14 0 8 -3) is the literal 8; MAX(005 3) the literal 005).
      *> 2. AN UNSIGNED DISPLAY INTEGER OF FIXED WIDTH: DATE-OF-INTEGER eight digits, DAY-OF-INTEGER seven.
      *> 3. THE DESCRIPTION THE VALUE ALONE GIVES, for every other exact function: the trailing zeros of the fraction are
      *>    dropped; a value under 2**31 (2**32 unsigned) with at most 9 fraction digits is a 9-position item, one under
      *>    2**63 (2**64) with at most 18 fraction digits a 20-position item, any other exactly its own digits; the item is
      *>    signed exactly when the value is negative. So INTEGER(-12.5) is -000000013, NUMVAL("12.50") is 00000012.5,
      *>    NUMVAL("0.000000001") is .000000001 and FACTORIAL(20) is 02432902008176640000.
      *>
      *> A call the binder FOLDS to a literal shows as the literal when GnuCOBOL folds the same call (LENGTH of a fixed item:
      *> 7), and the LENGTH of a run-time-length item is a computed value (000000003). A binary64 result keeps the image a
      *> floating-point item has (PB3505): pb3514_function_result_device_form_not_copied.
      *>
      *> EXPECTED VALUES are derived from those three descriptions and the declarations, then CHECKED line by line against
      *> `cobc -x -std=default` 3.2.0 (identical except the rows of pb3514_function_result_device_form_not_copied).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3514A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U1 PIC 9(5)V99 VALUE 12.5.
       01 S1 PIC S9(5)V99 VALUE -12.5.
       01 S1P PIC S9(5)V99 VALUE 12.5.
       01 I1 PIC 9(3) VALUE 7.
       01 S2 PIC S9(3) VALUE -14.
       01 N1 PIC 9V9 VALUE 2.5.
       01 BIG PIC 9(12) VALUE 123456789012.
       01 BIG2 PIC 9(18) VALUE 123456789012345678.
       01 TX PIC X(9) VALUE " -3.75   ".
       01 CHR PIC X VALUE "A".
       01 DT PIC 9(8) VALUE 20240131.
       01 DY PIC 9(7) VALUE 2024031.
       01 INT1 PIC 9(5) VALUE 20240.
       01 ZR PIC 9(3) VALUE 0.
       01 C3 PIC S9(5)V99 COMP-3 VALUE -12.5.
       01 C3U PIC 9(5)V99 COMP-3 VALUE 12.5.
       01 B4 PIC S9(4) COMP VALUE -123.
       01 SL PIC S9(3)V99 SIGN LEADING SEPARATE VALUE -1.5.
       01 ST PIC S9(3)V99 SIGN TRAILING SEPARATE VALUE -1.5.
       01 SO PIC S9(3)V99 SIGN LEADING VALUE -1.5.
       01 N PIC 9 VALUE 3.
       01 VG.
          05 VE PIC X OCCURS 1 TO 9 DEPENDING ON N.
       PROCEDURE DIVISION.
      *> --- 1. a copy of an argument's item ---
           DISPLAY "MAX-UU=" FUNCTION MAX(U1 N1) "|"
           DISPLAY "MAX-US=" FUNCTION MAX(U1 S1) "|"
           DISPLAY "MAX-SU=" FUNCTION MAX(S1 U1) "|"
           DISPLAY "MAX-IS=" FUNCTION MAX(I1 S2) "|"
           DISPLAY "MAX-SI=" FUNCTION MAX(S2 I1) "|"
           DISPLAY "MAX-LIT=" FUNCTION MAX(3 -14 0 8 -3) "|"
           DISPLAY "MAX-LITD=" FUNCTION MAX(1.5 2.25) "|"
           DISPLAY "MAX-LIT0=" FUNCTION MAX(005 3) "|"
           DISPLAY "MAX-LITP=" FUNCTION MAX(+5 3) "|"
           DISPLAY "MAX-MIX=" FUNCTION MAX(I1 2.5) "|"
           DISPLAY "MAX-C3=" FUNCTION MAX(C3 C3U) "|"
           DISPLAY "MAX-EXPR=" FUNCTION MAX(S1 + 1 3) "|"
           DISPLAY "MAX-EXPR2=" FUNCTION MAX(S1 + 100 3) "|"
           DISPLAY "MIN-US=" FUNCTION MIN(U1 S1) "|"
           DISPLAY "MIN-IS=" FUNCTION MIN(I1 S2) "|"
           DISPLAY "MIN-LIT=" FUNCTION MIN(3 -14 0 8 -3) "|"
           DISPLAY "MIN-SIGNS=" FUNCTION MIN(SL ST SO) "|"
           DISPLAY "MIN-ST=" FUNCTION MIN(ST 5) "|"
      *> equal values: the FIRST argument's item
           DISPLAY "MAX-EQ=" FUNCTION MAX(U1 S1P) "|"
           DISPLAY "MIN-EQ=" FUNCTION MIN(S1P U1) "|"
      *> a function nested as an argument is the temporary its own function made
           DISPLAY "MAX-NEST=" FUNCTION MAX(FUNCTION INTEGER(S1) 3) "|"
           DISPLAY "MAX-NEST2=" FUNCTION MAX(FUNCTION INTEGER(S1) -20) "|"
           DISPLAY "MED1=" FUNCTION MEDIAN(ST) "|"
           DISPLAY "MED3=" FUNCTION MEDIAN(U1 S1 N1) "|"
           DISPLAY "MED5=" FUNCTION MEDIAN(SL ST SO 5 7) "|"
           DISPLAY "MEAN1=" FUNCTION MEAN(ST) "|"
      *> --- 2. an unsigned DISPLAY integer of fixed width ---
           DISPLAY "DOI=" FUNCTION DATE-OF-INTEGER(INT1) "|"
           DISPLAY "DOI0=" FUNCTION DATE-OF-INTEGER(0) "|"
           DISPLAY "DAYOI=" FUNCTION DAY-OF-INTEGER(INT1) "|"
           DISPLAY "DAYOI0=" FUNCTION DAY-OF-INTEGER(0) "|"
      *> --- 3. the description the value alone gives ---
           DISPLAY "INTEGER-S=" FUNCTION INTEGER(S1) "|"
           DISPLAY "INTEGER-U=" FUNCTION INTEGER(U1) "|"
           DISPLAY "INTEGER-BIG=" FUNCTION INTEGER(BIG2) "|"
           DISPLAY "INTPART-S=" FUNCTION INTEGER-PART(S1) "|"
           DISPLAY "SUM-I=" FUNCTION SUM(I1 S2) "|"
           DISPLAY "SUM-D=" FUNCTION SUM(U1 N1) "|"
           DISPLAY "SUM-NEG=" FUNCTION SUM(S1 S2) "|"
           DISPLAY "SUM-BIG=" FUNCTION SUM(BIG BIG2) "|"
           DISPLAY "SUM-ZERO=" FUNCTION SUM(S1P S1) "|"
           DISPLAY "MEAN=" FUNCTION MEAN(I1 S2) "|"
           DISPLAY "MEAN4=" FUNCTION MEAN(1 2 3 4) "|"
           DISPLAY "MEDIAN4=" FUNCTION MEDIAN(U1 S1 N1 I1) "|"
           DISPLAY "MIDRANGE=" FUNCTION MIDRANGE(U1 S1) "|"
           DISPLAY "RANGE=" FUNCTION RANGE(U1 S1) "|"
           DISPLAY "RANGE-I=" FUNCTION RANGE(I1 S2) "|"
           DISPLAY "MOD=" FUNCTION MOD(S2 5) "|"
           DISPLAY "REM=" FUNCTION REM(S1 5) "|"
           DISPLAY "REM2=" FUNCTION REM(U1 5) "|"
           DISPLAY "NUMVAL=" FUNCTION NUMVAL(TX) "|"
           DISPLAY "NUMVAL-L=" FUNCTION NUMVAL("12.50") "|"
           DISPLAY "NUMVAL-INT=" FUNCTION NUMVAL("42") "|"
           DISPLAY "NUMVAL-NEG=" FUNCTION NUMVAL("-42") "|"
           DISPLAY "NUMVAL-ZERO=" FUNCTION NUMVAL("0") "|"
           DISPLAY "NUMVAL-33BITS=" FUNCTION NUMVAL("4294967296") "|"
           DISPLAY "NUMVAL-NEG32=" FUNCTION NUMVAL("-2147483648") "|"
           DISPLAY "NUMVAL-S9=" FUNCTION NUMVAL("0.9999999999") "|"
           DISPLAY "NUMVAL-S9B=" FUNCTION NUMVAL("12345678.123456789") "|"
           DISPLAY "NUMVAL-S18=" FUNCTION NUMVAL("0.123456789012345678") "|"
           DISPLAY "NUMVAL-S19=" FUNCTION NUMVAL("0.1234567890123456789") "|"
           DISPLAY "NUMVAL-TINY=" FUNCTION NUMVAL("0.000000001") "|"
           DISPLAY "NUMVAL-I64=" FUNCTION NUMVAL("18446744073709551616") "|"
           DISPLAY "NUMVAL-WIDE=" FUNCTION NUMVAL("1234567890123456789012") "|"
           DISPLAY "ORD=" FUNCTION ORD(CHR) "|"
           DISPLAY "ORD-MAX=" FUNCTION ORD-MAX(U1 S1 N1) "|"
           DISPLAY "ORD-MIN=" FUNCTION ORD-MIN(U1 S1 N1) "|"
           DISPLAY "FACT=" FUNCTION FACTORIAL(5) "|"
           DISPLAY "FACT-20=" FUNCTION FACTORIAL(20) "|"
           DISPLAY "FACT-25=" FUNCTION FACTORIAL(25) "|"
           DISPLAY "FACT-0=" FUNCTION FACTORIAL(ZR) "|"
           DISPLAY "IOD=" FUNCTION INTEGER-OF-DATE(DT) "|"
           DISPLAY "IODY=" FUNCTION INTEGER-OF-DAY(DY) "|"
      *> --- the folded and the run-time LENGTH ---
           DISPLAY "LENGTH-FIXED=" FUNCTION LENGTH(U1) "|"
           DISPLAY "LENGTH-LIT=" FUNCTION LENGTH("ABC") "|"
           DISPLAY "LENGTH-VAR=" FUNCTION LENGTH(VG) "|"
           STOP RUN.
