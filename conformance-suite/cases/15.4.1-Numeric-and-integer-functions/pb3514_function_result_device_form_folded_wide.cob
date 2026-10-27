*>--- conformance-case
*> id: pb3514_function_result_device_form_folded_wide
*> rules: [GR-14.9.11.4-1, RV-15.4.1-L2.2, DOC-A.1-92, DOC-A.1-56]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3514 review round 1 (DOC-A.1-92). Two consequences of one rule: a function's returned value is a temporary
      *> data item (§15.4) and DISPLAY shows the temporary by the description the implementor gave it (§14.9.11.4 GR1).
      *> (1) A call the compiler folds to a literal that GnuCOBOL 3.2.0 does NOT fold (HIGHEST-/LOWEST-ALGEBRAIC) is still a
      *>     function's returned value, so as the argument of ABS, MAX, MIN, MEDIAN or MEAN its temporary is described by its
      *>     value, exactly as it is when DISPLAY shows the call itself (000000999).
      *> (2) ABS of a NEGATIVE arithmetic expression is a signed temporary holding the magnitude (+), however many digits the
      *>     expression has: 37 digits and 39 digits both show the sign (the runtime keeps a description for a value past the
      *>     38 digits an Int128 holds). cobc shows both with a leading zero (a GMP size estimate that exceeds the true digit
      *>     count by one), which this compiler does not copy (pb3514_function_result_device_form_not_copied), so those two
      *>     lines are derived from the rule, the other eight are identical to cobc.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3514F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC S999 VALUE -5.
       01 U PIC 999 VALUE 5.
       01 BIG PIC S9(18) VALUE -999999999999999999.
       PROCEDURE DIVISION.
           DISPLAY "HIGH=" FUNCTION HIGHEST-ALGEBRAIC(S) "|"
           DISPLAY "LOW=" FUNCTION LOWEST-ALGEBRAIC(S) "|"
           DISPLAY "ABS-HIGH=" FUNCTION ABS(FUNCTION HIGHEST-ALGEBRAIC(S)) "|"
           DISPLAY "ABS-LOW=" FUNCTION ABS(FUNCTION LOWEST-ALGEBRAIC(S)) "|"
           DISPLAY "MAX-HIGH=" FUNCTION MAX(FUNCTION HIGHEST-ALGEBRAIC(U) 1) "|"
           DISPLAY "MIN-LOW=" FUNCTION MIN(FUNCTION LOWEST-ALGEBRAIC(S) 0) "|"
           DISPLAY "MEAN-HIGH=" FUNCTION MEAN(FUNCTION HIGHEST-ALGEBRAIC(S)) "|"
           DISPLAY "MEDIAN-HIGH=" FUNCTION MEDIAN(FUNCTION HIGHEST-ALGEBRAIC(S) 1 2) "|"
           DISPLAY "ABS-37=" FUNCTION ABS(BIG * 10000000000000000000) "|"
           DISPLAY "ABS-39=" FUNCTION ABS(BIG * 1000000000000000000000) "|"
           STOP RUN.
