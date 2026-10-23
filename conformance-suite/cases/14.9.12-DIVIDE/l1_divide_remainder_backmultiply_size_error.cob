*>--- conformance-case
*> id: l1_divide_remainder_backmultiply_size_error
*> rules: [GR-14.9.12.4-7]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.12.4 GR7 — "When native arithmetic is in effect, the remainder is the result of
      *> multiplying the subsidiary quotient and the divisor and subtracting the product from the
      *> dividend."
      *> (cite.py --check 14.9.12.4 "the remainder is the result of multiplying the subsidiary quotient and
      *> the divisor and subtracting the product from the dividend" -> OK, §14.9.12.4 General rules 7.)
      *>
      *> THE PRODUCT GR7 NAMES IS AN INTERMEDIATE, AND §14.7.5 GOVERNS IT. Case 5 of the SIZE ERROR
      *> phrase's list raises the size error condition "if native arithmetic is in effect and the
      *> implementor defines that the range of values allowed for the intermediate data item is to be
      *> checked" - and the range is the VALUE's (CONFORMANCE.md DOC-A.1-179, kb/Work PB3538). The
      *> operands satisfy §14.7.7 rule 2's composite of 12 + 14 = 26 digits; the product GR7 forms is
      *> (subsidiary quotient) x (divisor), about 10**12, carried at 14 + 14 = 28 fraction digits. Its
      *> UNSCALED form is of the order 10**40, past any 128-bit integer, but its value is not: the
      *> back-multiply is formed exactly on the 256-bit lane, so no size error is raised.
      *> DERIVED BEFORE MEASURING. BQ = 999999999999.999999999 / 1.00000000000001 truncated at 14
      *> places = 999999999999.98999999900000 (the exact quotient is 999999999999.98999999900000010...).
      *> BR = BD - BQ x BV = 999999999999.999999999 - 999999999999.99999999899999989999999
      *>    = 0.0000000000000001000000100000, truncated into 9(12)V9(9): 0.
      *>   OVF=NONE BQ=999999999999.98999999900000 BR=000000000000.000000000
      *> The failures this discriminates: a silent wrap of the back-multiply (a wrong BR), and the
      *> former checked Int128 multiply, which raised the size error for the product's SCALE although
      *> its value is in range (OVF=SIZEERR with BR unchanged at 7).
      *>
      *> EDITION. The 26-digit receiver needs the 2014 arithmetic range; the version matrix owns the gate
      *> below it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DIVOVF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BD PIC 9(12)V9(9) VALUE 999999999999.999999999.
       01 BV PIC 9V9(14)    VALUE 1.00000000000001.
       01 BQ PIC 9(12)V9(14).
       01 BR PIC 9(12)V9(9) VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           DIVIDE BV INTO BD GIVING BQ REMAINDER BR
               ON SIZE ERROR DISPLAY "OVF=SIZEERR BR=" BR
               NOT ON SIZE ERROR DISPLAY "OVF=NONE BQ=" BQ " BR=" BR
           END-DIVIDE
           STOP RUN.
