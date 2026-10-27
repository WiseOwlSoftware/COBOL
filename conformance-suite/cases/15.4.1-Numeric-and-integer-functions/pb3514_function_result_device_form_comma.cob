*>--- conformance-case
*> id: pb3514_function_result_device_form_comma
*> rules: [GR-14.9.11.4-1, RV-15.4.1-L2.2, DOC-A.1-92, DOC-A.1-56]
*> accept: [2002, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3514 (DOC-A.1-92) under DECIMAL-POINT IS COMMA: the decimal separator of a function's device form is the
      *> program's, the comma, exactly as it is for a data item (pb3505_display_numeric_decimal_comma) and as a numeric
      *> literal is written (§12.3.7.4 GR14 a: the comma is "the character written in numeric literals to represent the
      *> decimal separator"). Every description is the rule of pb3514_function_result_device_form: a copy of the argument's
      *> item (MAX, MIN, MEDIAN, a literal), or the value's own (NUMVAL, INTEGER, SUM, MEAN, REM); the comma is the only
      *> difference from that test, and the lines were CHECKED against `cobc -x -std=default` 3.2.0, which agrees.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3514C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U1 PIC 9(5)V99 VALUE 12,5.
       01 S1 PIC S9(5)V99 VALUE -12,5.
       01 X PIC 9V99 VALUE 1,5.
       01 Y PIC 9V99 VALUE 2,25.
       01 V PIC V9(3) VALUE ,5.
       PROCEDURE DIVISION.
           DISPLAY "MAX-ITEM=" FUNCTION MAX(X Y) "|"
           DISPLAY "MAX-US=" FUNCTION MAX(U1 S1) "|"
           DISPLAY "MIN-US=" FUNCTION MIN(U1 S1) "|"
           DISPLAY "MAX-LIT=" FUNCTION MAX(1,5 2,25) "|"
           DISPLAY "MIN-V=" FUNCTION MIN(V 1) "|"
           DISPLAY "SUM=" FUNCTION SUM(X Y) "|"
           DISPLAY "SUM-NEG=" FUNCTION SUM(S1 X) "|"
           DISPLAY "MEAN=" FUNCTION MEAN(X Y) "|"
           DISPLAY "INTEGER=" FUNCTION INTEGER(S1) "|"
           DISPLAY "NUMVAL=" FUNCTION NUMVAL("-1,5") "|"
           DISPLAY "NUMVAL-TINY=" FUNCTION NUMVAL("0,000000001") "|"
           DISPLAY "REM=" FUNCTION REM(S1 5) "|"
           DISPLAY "MEDIAN=" FUNCTION MEDIAN(U1 S1 X Y) "|"
           STOP RUN.
