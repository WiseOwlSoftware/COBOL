*>--- conformance-case
*> id: pb637_annuity_total_periods_unchecked
*> rules: [AR-15.9.3-3, RV-15.9.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB637 — the CHECKING-OFF half of pb637_annuity_total_periods. With EC-ARGUMENT-FUNCTION checking
      *> not enabled, §15.3 r14 leaves "the result of the function reference" to the implementor ONLY when the
      *> condition is set to exist, and §15.9.3 r3 ("Argument-2 shall be a positive integer") sets it for no count
      *> past the 64-bit window. The defect replaced the ARGUMENT with 0 silently - FUNCTION ANNUITY (0 N) then
      *> divided by nothing and returned 0 where §15.9.4 r1a, "(1 / (argument-2))", is 1E-20 at n = 10^20 and
      *> 1E-30 at n = 10^30. §15.4.1 makes the standard-decimal value equal the equivalent expression's.
      *>
      *>   T1  rate 0, n = 10^20: 1E-20.        T2  rate 0, n = 10^30 (a 31-digit item): 1E-30.
      *>   T3  rate 0, n = 9 999 999 999 999 999 999 (19 digits, past the 64-bit window): 1E-19 + 1E-38, so a
      *>       31-place receiver holds 1E-19.
      *>   T4  rate 1E-30, n = 10^20: 0.0000000000000000000100000000005 to 31 places (derived in the checked
      *>       program's header: rate / (x - x**2/2 + ...), x = n * ln(1 + rate) = 1E-10 - 5E-41).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB637UNC.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N19   PIC 9(19) VALUE 9999999999999999999.
       01 N20   PIC 9(25) VALUE 100000000000000000000.
       01 N30   PIC 9(31) VALUE 1000000000000000000000000000000.
       01 R1    PIC V9(31).
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE R1 = FUNCTION ANNUITY(0, N20)
           IF R1 = 0.00000000000000000001
               DISPLAY "T1=OK"
           ELSE
               DISPLAY "T1=BAD"
           END-IF
           COMPUTE R1 = FUNCTION ANNUITY(0, N30)
           IF R1 = 0.000000000000000000000000000001
               DISPLAY "T2=OK"
           ELSE
               DISPLAY "T2=BAD"
           END-IF
           COMPUTE R1 = FUNCTION ANNUITY(0, N19)
           IF R1 = 0.0000000000000000001
               DISPLAY "T3=OK"
           ELSE
               DISPLAY "T3=BAD"
           END-IF
           COMPUTE R1 = FUNCTION ANNUITY(0.000000000000000000000000000001, N20)
           IF R1 = .0000000000000000000100000000005
               DISPLAY "T4=OK"
           ELSE
               DISPLAY "T4=BAD"
           END-IF
           STOP RUN.
       END PROGRAM PB637UNC.
