*>--- conformance-case
*> id: pb3514_function_result_device_form_abs
*> rules: [GR-14.9.11.4-1, RV-15.4.1-L2.2, DOC-A.1-92, DOC-A.1-56]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3514 (DOC-A.1-92), the functions the 2002 and 2014 editions added to the device-form matrix of
      *> pb3514_function_result_device_form. ABS (§15.7.1: "returns the absolute value of the argument") returns the argument's
      *> CONTENT magnitude in a copy of the argument's item, so the temporary has the argument's description and its sign
      *> position: ABS of a signed item shows a leading `+` (a signed item always shows its sign), ABS of an unsigned
      *> item shows none, and ABS of a negative LITERAL is a signed field of the digits written. A nested function is the
      *> temporary its own function made: ABS(INTEGER(-12.5)) copies INTEGER's SIGNED 9-position item (the value was
      *> negative when the item was made), +000000013. An arithmetic expression is a value-described temporary, signed
      *> when the expression's value is negative.
      *>
      *> SIGN is -1, 0 or 1 in a 9-position item signed only when negative; FRACTION-PART drops the trailing zeros of the
      *> fraction; HIGHEST-ALGEBRAIC / LOWEST-ALGEBRAIC are values GnuCOBOL evaluates at run time (not a fold), so the
      *> value describes the item; BYTE-LENGTH of a fixed item is a fold (7), of a run-time-length item a computed value.
      *> Every line was CHECKED against `cobc -x -std=default` 3.2.0 and is identical.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3514B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U1 PIC 9(5)V99 VALUE 12.5.
       01 S1 PIC S9(5)V99 VALUE -12.5.
       01 S2 PIC S9(3) VALUE -14.
       01 I1 PIC 9(3) VALUE 7.
       01 ZR PIC 9(3) VALUE 0.
       01 Z1 PIC S9(3) VALUE 0.
       01 C3 PIC S9(5)V99 COMP-3 VALUE -12.5.
       01 C3U PIC 9(5)V99 COMP-3 VALUE 12.5.
       01 B4 PIC S9(4) COMP VALUE -123.
       01 B5 PIC 9(9) COMP-5 VALUE 5.
       01 BD BINARY-DOUBLE SIGNED VALUE -7.
       01 SL PIC S9(3)V99 SIGN LEADING SEPARATE VALUE -1.5.
       01 ST PIC S9(3)V99 SIGN TRAILING SEPARATE VALUE -1.5.
       01 SO PIC S9(3)V99 SIGN LEADING VALUE -1.5.
       01 PP PIC 9(3)PP VALUE 1200.
       01 LP PIC PP999 VALUE 0.00123.
       01 N9 PIC S9(9) VALUE -123456789.
       01 W30 PIC S9(30) VALUE -123456789012345678901234567890.
       01 F1 COMP-1 VALUE 1.5.
       01 F2 COMP-2 VALUE 2.5.
       01 DT PIC 9(8) VALUE 20240131.
       01 DY PIC 9(7) VALUE 2024031.
       01 TX PIC X(9) VALUE " -3.75   ".
       01 TXC PIC X(12) VALUE "$1,234.50".
       01 N PIC 9 VALUE 3.
       01 VG.
          05 VE PIC X OCCURS 1 TO 9 DEPENDING ON N.
       PROCEDURE DIVISION.
           DISPLAY "ABS-S=" FUNCTION ABS(S1) "|"
           DISPLAY "ABS-U=" FUNCTION ABS(U1) "|"
           DISPLAY "ABS-I=" FUNCTION ABS(S2) "|"
           DISPLAY "ABS-Z=" FUNCTION ABS(Z1) "|"
           DISPLAY "ABS-ZR=" FUNCTION ABS(ZR) "|"
           DISPLAY "ABS-C3=" FUNCTION ABS(C3) "|"
           DISPLAY "ABS-C3U=" FUNCTION ABS(C3U) "|"
           DISPLAY "ABS-B4=" FUNCTION ABS(B4) "|"
           DISPLAY "ABS-B5=" FUNCTION ABS(B5) "|"
           DISPLAY "ABS-BD=" FUNCTION ABS(BD) "|"
           DISPLAY "ABS-SL=" FUNCTION ABS(SL) "|"
           DISPLAY "ABS-ST=" FUNCTION ABS(ST) "|"
           DISPLAY "ABS-SO=" FUNCTION ABS(SO) "|"
           DISPLAY "ABS-PP=" FUNCTION ABS(PP) "|"
           DISPLAY "ABS-LP=" FUNCTION ABS(LP) "|"
           DISPLAY "ABS-N9=" FUNCTION ABS(N9) "|"
           DISPLAY "ABS-W30=" FUNCTION ABS(W30) "|"
           DISPLAY "ABS-F1=" FUNCTION ABS(F1) "|"
           DISPLAY "ABS-F2=" FUNCTION ABS(F2) "|"
           DISPLAY "ABS-LITN=" FUNCTION ABS(-12.50) "|"
           DISPLAY "ABS-LITN0=" FUNCTION ABS(-00012.50) "|"
           DISPLAY "ABS-LITP=" FUNCTION ABS(+12.50) "|"
           DISPLAY "ABS-LITU=" FUNCTION ABS(12.50) "|"
           DISPLAY "ABS-LIT0=" FUNCTION ABS(0) "|"
           DISPLAY "ABS-EXPR=" FUNCTION ABS(S1 + 1) "|"
           DISPLAY "ABS-EXPRP=" FUNCTION ABS(U1 + 1) "|"
           DISPLAY "ABS-NEST=" FUNCTION ABS(FUNCTION ABS(S1)) "|"
           DISPLAY "ABS-NESTI=" FUNCTION ABS(FUNCTION INTEGER(S1)) "|"
           DISPLAY "ABS-NESTU=" FUNCTION ABS(FUNCTION INTEGER(U1)) "|"
           DISPLAY "MAX-NEST=" FUNCTION MAX(FUNCTION ABS(S1) 3) "|"
           DISPLAY "MAX-B=" FUNCTION MAX(B4 BD) "|"
           DISPLAY "MAX-FI=" FUNCTION MAX(F2 B5) "|"
           DISPLAY "SIGN-N=" FUNCTION SIGN(S1) "|"
           DISPLAY "SIGN-P=" FUNCTION SIGN(U1) "|"
           DISPLAY "SIGN-Z=" FUNCTION SIGN(ZR) "|"
           DISPLAY "FRAC-S=" FUNCTION FRACTION-PART(S1) "|"
           DISPLAY "FRAC-U=" FUNCTION FRACTION-PART(U1) "|"
           DISPLAY "FRAC-I=" FUNCTION FRACTION-PART(I1) "|"
           DISPLAY "FRAC-LP=" FUNCTION FRACTION-PART(LP) "|"
           DISPLAY "INT-LP=" FUNCTION INTEGER(LP) "|"
           DISPLAY "SUM-PP=" FUNCTION SUM(PP LP) "|"
           DISPLAY "NUMVAL-C=" FUNCTION NUMVAL-C(TXC) "|"
           DISPLAY "TEST-NUMVAL=" FUNCTION TEST-NUMVAL(TX) "|"
           DISPLAY "TEST-DATE=" FUNCTION TEST-DATE-YYYYMMDD(DT) "|"
           DISPLAY "TEST-DAY=" FUNCTION TEST-DAY-YYYYDDD(DY) "|"
           DISPLAY "HIGH-U=" FUNCTION HIGHEST-ALGEBRAIC(U1) "|"
           DISPLAY "LOW-S=" FUNCTION LOWEST-ALGEBRAIC(S1) "|"
           DISPLAY "LOW-U=" FUNCTION LOWEST-ALGEBRAIC(U1) "|"
           DISPLAY "HIGH-INT=" FUNCTION HIGHEST-ALGEBRAIC(ZR) "|"
           DISPLAY "LOW-COMP3=" FUNCTION LOWEST-ALGEBRAIC(C3) "|"
           DISPLAY "HIGH-COMP=" FUNCTION HIGHEST-ALGEBRAIC(B4) "|"
           DISPLAY "BYTELEN-FIXED=" FUNCTION BYTE-LENGTH(U1) "|"
           DISPLAY "BYTELEN-VAR=" FUNCTION BYTE-LENGTH(VG) "|"
           STOP RUN.
