      *>--- conformance-case
      *> id: pb4569_native_integer_power_exact
      *> rules: [DOC-A.1-123, DOC-A.1-179, GR-8.8.1.2-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4569 (docs/CONFORMANCE.md DOC-A.1-123): under NATIVE
      *> arithmetic an integer base raised to an integer exponent is
      *> EXACT while the power's value fits the wide form's 77 digits,
      *> whatever the digit bound the compiler can prove for it. ISO
      *> 8.8.1.3: "Native arithmetic is an implementor-defined method
      *> of evaluating an arithmetic expression" and "The implementor
      *> shall specify techniques used for native arithmetic" (cite.py:
      *> OK 8.8.1.3), so CLAUDE.md rule 1 sends the choice to GnuCOBOL,
      *> whose run-time power is an exact mpz_pow_ui. The integer base
      *> used to fall back to binary64 past the Int128 carrier and keep
      *> 16 or 17 digits: 2 ** 130 showed 1361129467683754 followed by
      *> 24 zeros, and every consumer inherited the wrong operand.
      *> 15.4.1: "the characteristics and representation of the
      *> returned value are defined by the implementor" (cite.py: OK
      *> 15.4.1); ABS returns its argument's value (15.7.1).
      *> 8.8.1.2 6) a): a zero base needs an exponent above zero (kept).
      *>
      *> EXPECTED VALUES, DERIVED (Python integers): 2**130 =
      *> 1361129467683753853853498429727072845824 (40 digits);
      *> 2**100 = 1267650600228229401496703205376. 2**130 mod 7 = 2
      *> (2**3 = 1 mod 7, 130 = 3 * 43 + 1); 2**130 mod 1000 = 824;
      *> (-2)**131 mod 1000 = 352. B65 = 2**65,
      *> B64 = 2**64, B63 = 2**63, so B65 * B65 = 2**130 and
      *> B64 * B63 = 2**127.
      *>   A1/A2  ABS (2 ** 130) and ABS (X1 ** N): all 40 digits.
      *>   M4     MOD ((-2) ** 131, 1000) = 352 (a negative base).
      *>   M1/M2  MOD (2 ** 130, 7) = 2; MOD (2 ** 130, 1000) = 824.
      *>   M3     REM (X1 ** N, 1000) = 824.
      *>   I1     INTEGER (2 ** 130): the power itself.
      *>   E1     X1 ** N = B65 * B65 is TRUE.
      *>   C1     X1 ** N127 - B64 * B63 + 5 = 5 (2**127 cancels).
      *>   C2     (X1 ** N - B65 * B65) * 1000 + 42 = 42.
      *>   Q1     X1 ** N / X1 ** 30 = 2**100.
      *>   R1/R2  X3 ** NM1 = 1/3 = 0.333... to the 30 places of RQ,
      *>          for an integer base and for the 9V9 base alike.
      *>   P1/P2  -1 and 1 raised to 999999999999999999 return at once.
      *>   Z1     0 ** 0 is EC-SIZE-EXPONENTIATION (8.8.1.2 6 a).
      *> Why each leg can fail: a binary64 landing at any step keeps 16
      *> or 17 digits, so ABS ends in zeros, MOD answers 0 for 824 and
      *> 6 for 2, E1 is FALSE and C2 stores 146146501570272927154176042.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4569POWEXACT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X1    PIC 9 VALUE 2.
       01 X3    PIC 9 VALUE 3.
       01 X3S   PIC 9V9 VALUE 3.
       01 N     PIC 999 VALUE 130.
       01 N127  PIC 999 VALUE 127.
       01 NM1   PIC S9 VALUE -1.
       01 ONE   PIC 9 VALUE 1.
       01 MONE  PIC S9 VALUE -1.
       01 BIGN  PIC 9(18) VALUE 999999999999999999.
       01 B65   PIC 9(20) VALUE 36893488147419103232.
       01 B64   PIC 9(20) VALUE 18446744073709551616.
       01 B63   PIC 9(19) VALUE 9223372036854775808.
       01 ZERO0 PIC 9 VALUE 0.
       01 RM    PIC S9(5).
       01 I1    PIC S9(5).
       01 R31   PIC 9(31).
       01 S31   PIC S9(31).
       01 RQ    PIC 9V9(30).
       PROCEDURE DIVISION.
           DISPLAY "A1 " FUNCTION ABS (2 ** 130).
           DISPLAY "A2 " FUNCTION ABS (X1 ** N).
           DISPLAY "M4 " FUNCTION MOD ((-2) ** 131, 1000).
           DISPLAY "M1 " FUNCTION MOD (2 ** 130, 7).
           DISPLAY "M2 " FUNCTION MOD (2 ** 130, 1000).
           DISPLAY "M3 " FUNCTION REM (X1 ** N, 1000).
           DISPLAY "I1 " FUNCTION INTEGER (2 ** 130).
           IF X1 ** N = B65 * B65
               DISPLAY "E1 TRUE"
           ELSE
               DISPLAY "E1 FALSE".
           COMPUTE RM = X1 ** N127 - B64 * B63 + 5
               ON SIZE ERROR DISPLAY "C1 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "C1 " RM.
           COMPUTE S31 = (X1 ** N - B65 * B65) * 1000 + 42
               ON SIZE ERROR DISPLAY "C2 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "C2 " S31.
           COMPUTE R31 = X1 ** N / X1 ** 30
               ON SIZE ERROR DISPLAY "Q1 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "Q1 " R31.
           COMPUTE RQ = X3 ** NM1.
           DISPLAY "R1 " RQ.
           COMPUTE RQ = X3S ** NM1.
           DISPLAY "R2 " RQ.
           COMPUTE I1 = MONE ** BIGN.
           DISPLAY "P1 " I1.
           COMPUTE I1 = ONE ** BIGN.
           DISPLAY "P2 " I1.
           MOVE 7 TO I1.
           COMPUTE I1 = ZERO0 ** ZERO0
               ON SIZE ERROR DISPLAY "Z1 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "Z1 " I1.
           STOP RUN.
