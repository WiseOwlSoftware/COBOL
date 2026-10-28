*>--- conformance-case
*> id: pb3951_length_of_abs_below_2014
*> rules: [RV-15.50.4-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3951: the control for pb3951_length_of_numeric_function_result, which is accepted from COBOL 2014. FUNCTION ABS is a
      *> COBOL-2014 function (ISO/IEC 1989:2014 §15), so a program that measures the returned value of ABS is refused below it by name
      *> (COBOL1502) however LENGTH measures the temporary; the same measurement of COBOL-2002 functions (SQRT, PI, ANNUITY, NUMVAL)
      *> is accepted at 2002 by pb3951_length_of_numeric_function_result_not_copied.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3951C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC S9(5)V99 VALUE -12.5.
       PROCEDURE DIVISION.
           DISPLAY FUNCTION LENGTH(FUNCTION ABS(S))
           STOP RUN.
