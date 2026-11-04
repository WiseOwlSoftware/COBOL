      *>--- conformance-case
      *> id: pb4569_standard_decimal_power_is_34_digits
      *> rules: [GR-8.8.1.5.4-2]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4569: the STANDARD-DECIMAL twin of
      *> pb4569_native_integer_power_exact, which pins what the exact
      *> NATIVE power must NOT change. Under ARITHMETIC IS
      *> STANDARD-DECIMAL an exponentiation whose exponent is an integer
      *> larger than the cases 8.8.1.5.4 2) a) to d) list is "defined by
      *> the implementor. Operands used in the development of that
      *> value shall be in SDIDI form" (cite.py: OK 8.8.1.5.4 2) e)),
      *> and an SDIDI holds "a maximum precision of 34 decimal digits"
      *> (cite.py: OK 8.8.1.5.2). So 2 ** 130 =
      *> 1361129467683753853853498429727072845824 (40 digits) is the
      *> 34-digit 1.361129467683753853853498429727073E+39, which
      *> ABS returns unchanged (15.7.1): 1361129467683753853853498429
      *> 727073000000. The same image enters X1 ** N - B65 * B65 + 5,
      *> whose product B65 * B65 (= 2**130, 40 digits) is rounded to
      *> the same 34 digits, so the difference is 0 and the sum 5.
      *> Why each leg can fail: a power that took the native exact
      *> lane here would show ...845824 in S1 and S2; a binary64 one
      *> would keep 16 digits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4569SDPOW.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X1    PIC 9 VALUE 2.
       01 N     PIC 999 VALUE 130.
       01 B65   PIC 9(20) VALUE 36893488147419103232.
       01 RM    PIC S9(5).
       PROCEDURE DIVISION.
           DISPLAY "S1 " FUNCTION ABS (2 ** 130).
           DISPLAY "S2 " FUNCTION ABS (X1 ** N).
           COMPUTE RM = X1 ** N - B65 * B65 + 5
               ON SIZE ERROR DISPLAY "S3 SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "S3 " RM.
           STOP RUN.
