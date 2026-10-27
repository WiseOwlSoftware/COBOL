*>--- conformance-case
*> id: pb3514_function_result_device_form_standard_decimal
*> rules: [GR-14.9.11.4-1, RV-15.4.1-L2.2, DOC-A.1-92, DOC-A.1-56]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3514 (DOC-A.1-92) under ARITHMETIC IS STANDARD-DECIMAL (§8.8.1.5; §15.4.1: "the returned value for numeric
      *> and integer functions is contained in a temporary standard data item in the intermediate form defined for the
      *> arithmetic mode in effect"). A function the native mode answers in binary64 (SQRT, EXP, PI) answers on the SDIDI
      *> here, an exact decimal value, so the value alone describes its temporary exactly as for every other exact function
      *> (pb3514_function_result_device_form): SQRT(4) is 2 in a 9-position item, EXP(0) is 1, SQRT(2) and PI are DISPLAY
      *> items of their 34 significant digits (the SDIDI's decimal128 precision, §8.8.1.5.2). The selection functions copy
      *> an argument's item in every mode. Expected values are derived from the SDIDI values and the description rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3514D.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U1 PIC 9(5)V99 VALUE 12.5.
       01 S1 PIC S9(5)V99 VALUE -12.5.
       01 X PIC 9V99 VALUE 1.5.
       PROCEDURE DIVISION.
           DISPLAY "SQRT4=" FUNCTION SQRT(4) "|"
           DISPLAY "SQRT2=" FUNCTION SQRT(2) "|"
           DISPLAY "EXP0=" FUNCTION EXP(0) "|"
           DISPLAY "PI=" FUNCTION PI "|"
           DISPLAY "MAX-US=" FUNCTION MAX(U1 S1) "|"
           DISPLAY "ABS-S=" FUNCTION ABS(S1) "|"
           DISPLAY "MIN-US=" FUNCTION MIN(U1 S1) "|"
           DISPLAY "SUM=" FUNCTION SUM(U1 X) "|"
           DISPLAY "INTEGER=" FUNCTION INTEGER(S1) "|"
           DISPLAY "NUMVAL=" FUNCTION NUMVAL("12.50") "|"
           DISPLAY "MEAN=" FUNCTION MEAN(1 2 3) "|"
           DISPLAY "ORD-MAX=" FUNCTION ORD-MAX(U1 S1 X) "|"
           STOP RUN.
