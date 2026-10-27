*>--- conformance-case
*> id: pb3514_function_result_device_form_not_copied
*> rules: [GR-14.9.11.4-1, RV-15.4.1-L2.2, DOC-A.1-92, DOC-A.1-56]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3514 (DOC-A.1-92): where WiseOwl COBOL's device form of a function's returned value DIFFERS from GnuCOBOL
      *> 3.2.0's, on purpose, and why. ISO leaves the description of the returned-value item to the implementor (§15.4.1), so
      *> the precedence of CLAUDE.md rule 1 follows GnuCOBOL only where GnuCOBOL's form is a DESCRIPTION. Each row here is a
      *> place where it shows a figure the value does not have or an image no description gives; expected values are derived
      *> from the determination, not copied from a run of cobc.
      *>
      *> 1. NO DIGIT IS CUT. GnuCOBOL's 4-byte temporary has 9 digit positions, so a 10-digit value (2147483647, 4294967295)
      *>    shows without its high-order digit (147483647, 294967295). The positions here are max(9, the value's digits).
      *> 2. NO DIGIT IS INVENTED. GnuCOBOL's DISPLAY temporary takes its digit count from GMP's size ESTIMATE, which exceeds
      *>    the true count by one for some values from 2**64 on and shows a leading zero (-09223372036854775808,
      *>    0999999999999999999999). The digit count here is the value's own.
      *> 3. A BINARY64 RESULT KEEPS THE IMAGE A FLOATING-POINT ITEM HAS (kb/Work PB3505 declined GnuCOBOL's float image): the
      *>    shortest round-trip text. SQRT, EXP, PI and the trigonometric family return binary64 under native arithmetic
      *>    (§15.4.1), and a function given a floating-point item (COMP-1 / COMP-2) returns binary64 too. GnuCOBOL shows
      *>    SQRT(4) as 000000002, SQRT(2) with 100 digits, and ABS of a COMP-2 holding -0.1 as 0.09999999999999999.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3514E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1 COMP-1 VALUE 1.5.
       01 F2 COMP-2 VALUE 2.5.
       01 F3 COMP-2 VALUE -0.1.
       PROCEDURE DIVISION.
      *> 1.
           DISPLAY "TEN-DIGITS=" FUNCTION NUMVAL("2147483647") "|"
           DISPLAY "TEN-DIGITS-U=" FUNCTION NUMVAL("4294967295") "|"
           DISPLAY "TEN-DIGITS-N=" FUNCTION NUMVAL("-2147483647") "|"
      *> 2.
           DISPLAY "I63=" FUNCTION NUMVAL("-9223372036854775808") "|"
           DISPLAY "I63B=" FUNCTION NUMVAL("-9223372036854775809") "|"
           DISPLAY "I70=" FUNCTION NUMVAL("999999999999999999999") "|"
           DISPLAY "I70N=" FUNCTION NUMVAL("-99999999999999999999") "|"
      *> 3.
           DISPLAY "SQRT4=" FUNCTION SQRT(4) "|"
           DISPLAY "SQRT2=" FUNCTION SQRT(2) "|"
           DISPLAY "EXP0=" FUNCTION EXP(0) "|"
           DISPLAY "SIN0=" FUNCTION SIN(0) "|"
           DISPLAY "COS0=" FUNCTION COS(0) "|"
           DISPLAY "SUM-F=" FUNCTION SUM(F2 F1) "|"
           DISPLAY "INTEGER-F=" FUNCTION INTEGER(F2) "|"
           DISPLAY "ABS-F3=" FUNCTION ABS(F3) "|"
           DISPLAY "MAX-F=" FUNCTION MAX(F1 F2) "|"
           STOP RUN.
