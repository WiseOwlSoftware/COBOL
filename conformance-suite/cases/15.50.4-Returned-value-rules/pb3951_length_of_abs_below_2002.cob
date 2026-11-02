*>--- conformance-case
*> id: pb3951_length_of_abs_below_2002
*> rules: [RV-15.50.4-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3951, PB4296: the control for pb3951_length_of_numeric_function_result, which is accepted from COBOL 2002. FUNCTION ABS
      *> is a COBOL-2002 function (ISO/IEC 1989:2002 Annex F.2 item 78 lists it among the new intrinsic functions; tests/version-matrix/
      *> intrinsic-introductions.json), so a program that measures the returned value of ABS is refused below it, at 85, by name and by
      *> the true edition (COBOL1502 "introduced by ISO/IEC 1989:2002") however LENGTH measures the temporary; the same measurement of
      *> SQRT, PI, ANNUITY and NUMVAL is accepted at 2002 by pb3951_length_of_numeric_function_result_not_copied.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3951C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC S9(5)V99 VALUE -12.5.
       PROCEDURE DIVISION.
           DISPLAY FUNCTION LENGTH(FUNCTION ABS(S))
           STOP RUN.
