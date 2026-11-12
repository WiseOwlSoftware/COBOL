*>--- conformance-case
*> id: pb69_native_power_past_the_carrier
*> rules: [DOC-A.1-179]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB69 / PB4569 - a native integer power consumed as a VALUE (an argument, a comparison operand, an
      *> arithmetic operand). 8.8.1.3 makes native arithmetic implementor-defined and requires the technique to
      *> be specified (cite.py: OK 8.8.1.3); CLAUDE.md rule 1 sends the choice to GnuCOBOL, whose run-time power
      *> is an exact mpz_pow_ui, and the owner decided on 2026-11-04 that the integer base is exact too
      *> (docs/CONFORMANCE.md DOC-A.1-123): an integer power is EXACT while its value fits the 256-bit wide form
      *> (77 digits), whichever of the Int128 carrier it leaves, and past it the SDIDI's 34-digit round-to-odd
      *> value. The former determination (owner decision 2026-08-03) kept a binary64 approximation past the
      *> Int128 carrier. PB69: one arm for a literal and a data-item exponent, no saturated sentinel anywhere.
      *> 15.4.1: under native arithmetic a function's returned value is "an implementor-defined approximation of
      *> the value of that expression" - a function of the OPERAND, which the Int128.MaxValue sentinel was not
      *> (A**3 and A**4 answered the SAME MOD).
      *> A = 999999999999999 (15 digits), B = 1000000007; the expected values are Python integers.
      *> T2/T4/T11: A**2 (30 digits) fits: MOD = 13657001 for the literal AND the data-item exponent,
      *>   MOD(A**2 * 3, B) = 40971003.
      *> T5/T6: A**4 > A**3 is TRUE and A**3 = A**4 is FALSE - two powers of different magnitude (before PB69
      *>   both saturated to the same sentinel: FALSE / TRUE).
      *> T7: COMPUTE R31 = A**2 exact; T8: A**3 (45 digits) into a 31-digit receiver is a SIZE ERROR (the checked
      *>   store); T9: A**3 - A**3 = 0; T10: A**3 / A**2 * 1 = A = 999999999999999, the NESTED quotient
      *>   truncated at the working scale on the exact wide form (DOC-A.1-123) - before PB69 the sentinel gave
      *>   170141183, before PB4569 the binary64 power gave 999999999999998.
      *> T1/T3: MOD(A**3, B) - the SAME value for both spellings, and the TRUE one: A**3 =
      *>   999999999999997000000000000002999999999999999 (45 digits) is exact on the wide form, and 15.64.4 1)
      *>   over it is 980012199. Before PB4569 the argument was the binary64 power's shortest decimal,
      *>   9.99999999999997E44, whose residue is 1012193 (before PB69: 639816141 from the sentinel and
      *>   966729007 from the Dec arm's modular landing).
      *> X and Y are UNSIGNED: A ** X is MOD's argument-1, which §2023:15.3 type 6 requires to ALWAYS result in
      *> an integer, and a signed exponent item can be negative (kb/Work PB5208 refuses A ** X over a signed X).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB69NATIVEPOW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(18) VALUE 999999999999999.
       01 B PIC S9(18) VALUE 1000000007.
       01 X PIC 9(4) VALUE 3.
       01 Y PIC 9(4) VALUE 2.
       01 R18 PIC S9(18).
       01 R31 PIC S9(31).
       PROCEDURE DIVISION.
           COMPUTE R18 = FUNCTION MOD(A ** 3, B)  DISPLAY "T1 MOD(A**3,B)=" R18
           COMPUTE R18 = FUNCTION MOD(A ** 2, B)  DISPLAY "T2 MOD(A**2,B)=" R18
           COMPUTE R18 = FUNCTION MOD(A ** X, B)  DISPLAY "T3 MOD(A**X,B)=" R18
           COMPUTE R18 = FUNCTION MOD(A ** Y, B)  DISPLAY "T4 MOD(A**Y,B)=" R18
           IF A ** 4 > A ** 3 DISPLAY "T5 A**4>A**3: TRUE" ELSE DISPLAY "T5 A**4>A**3: FALSE" END-IF
           IF A ** 3 = A ** 4 DISPLAY "T6 A**3=A**4: TRUE" ELSE DISPLAY "T6 A**3=A**4: FALSE" END-IF
           COMPUTE R31 = A ** 2 DISPLAY "T7 A**2=" R31
           COMPUTE R31 = A ** 3 ON SIZE ERROR DISPLAY "T8 A**3 SIZE ERROR" NOT ON SIZE ERROR DISPLAY "T8 A**3=" R31 END-COMPUTE
           COMPUTE R31 = A ** 3 - A ** 3 ON SIZE ERROR DISPLAY "T9 SIZE ERROR" NOT ON SIZE ERROR DISPLAY "T9 A**3-A**3=" R31 END-COMPUTE
           COMPUTE R31 = A ** 3 / A ** 2 * 1 ON SIZE ERROR DISPLAY "T10 SIZE ERROR" NOT ON SIZE ERROR DISPLAY "T10 A**3/A**2*1=" R31 END-COMPUTE
           COMPUTE R18 = FUNCTION MOD(A ** 2 * 3, B) DISPLAY "T11 MOD(A**2*3,B)=" R18
           STOP RUN.
       END PROGRAM PB69NATIVEPOW.
