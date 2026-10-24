*>--- conformance-case
*> id: pb637_annuity_total_periods
*> rules: [AR-15.9.3-3, RV-15.9.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB637 — ISO §15.9 ANNUITY's argument-2 is a TOTAL integer argument, witnessed under
      *> ARITHMETIC IS STANDARD-DECIMAL with EC-ARGUMENT-FUNCTION CHECKING ON, for a count past the 64-bit window.
      *>
      *> THE RULES. §15.9.3 r3 is one sentence — "Argument-2 shall be a positive integer." — a SIGN and integer-ness
      *> rule that places no constraint on the MAGNITUDE, so §15.3's closing paragraph (EC-ARGUMENT-FUNCTION when an
      *> argument "results in an incorrect value for that argument or for the returned value according to the rules
      *> specified in the function definition") has nothing to fire on for a large n. The value is the returned-value
      *> rule's: §15.9.4 r1a, "(1 / (argument-2))" at rate zero, and r1b, "(argument-1 / (1 - (1 + argument-1) **
      *> (- (argument-2))))" otherwise; and §15.4.1 makes the standard-decimal returned value EQUAL the value of that
      *> equivalent expression, so "function-identifier = equivalent-arithmetic-expression" is true (its NOTE 2).
      *> The defect: the SDIDI body took argument-2 through the PARTIAL-function narrowing, so a count past
      *> 9 223 372 036 854 775 807 terminated the run unit under checking and was replaced by 0 with checking off.
      *>
      *> WHAT EACH LEG OWES.
      *>   T1  rate 0, n = 10^20 (past the 64-bit window): 1 / 10^20 = 1E-20 — exactly representable.
      *>   T2  rate 0, n = 10^30 (a 31-digit item, the widest PICTURE allows): 1E-30.
      *>   T3  §15.4.1 NOTE 2 itself: FUNCTION ANNUITY (0 N20) = 1 / N20, the two sides evaluated as SDIDI values.
      *>   T4  a 19-digit n above the 64-bit window, 9 999 999 999 999 999 999: 1 / (10^19 - 1) is 1E-19 plus 1E-38,
      *>       so a 31-place receiver holds 1E-19 exactly.
      *>   T5  rate 1E-30, n = 10^20: with x = n * ln(1 + rate) = 1E-10 - 5E-41, the denominator of r1b is
      *>       1 - e^(-x) = x - x**2/2 + ..., and the value rate / (x - x**2/2 + ...) = 1E-20 * (1 + 5E-11 + ...)
      *>       = 0.0000000000000000000100000000005 to 31 places. The cancellation costs the written expression ten of
      *>       the SDIDI's 34 digits, which still leaves the 31st place exact.
      *>   T6/T7  rate 0.05: (1 + rate) ** n leaves the decimal128 range (§8.8.1.5.2 r2) at n = 10^6 and at n = 10^20
      *>       alike, so the equivalent expression has no value at either and the function raises ITS size condition
      *>       there - the same one on both sides of the 64-bit window, never an EC-ARGUMENT-FUNCTION that depends on
      *>       the carrier's width. The ON SIZE ERROR phrase observes it; the declarative would report any
      *>       EC-ARGUMENT-FUNCTION and does not here.
      *>
      *> ARMED, NOT ASSUMED. A checking-ON leg proves nothing if the directive is inert, so A0 raises the one
      *> condition §15.9.3 does define for argument-2 - r3's sign, a non-positive count - and the declarative reports
      *> it; the aborted COMPUTE stores nothing and GUARD keeps its preset 9. Every later leg runs under the same
      *> armed directive.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB637ANN.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NEG   PIC S9(4) VALUE -3.
       01 N19   PIC 9(19) VALUE 9999999999999999999.
       01 N20   PIC 9(25) VALUE 100000000000000000000.
       01 N30   PIC 9(31) VALUE 1000000000000000000000000000000.
       01 N6    PIC 9(7) VALUE 1000000.
       01 GUARD PIC 9 VALUE 9.
       01 R1    PIC 9V9(29).
       01 R2    PIC V9(31).
       01 R3    PIC V9(31).
       01 R4    PIC V9(31).
       01 R5    PIC V9(31).
       01 ES    PIC X(20).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           MOVE FUNCTION EXCEPTION-STATUS TO ES
           DISPLAY "ARMED=" ES.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 9 TO GUARD
           COMPUTE GUARD = FUNCTION ANNUITY(0, NEG)
           DISPLAY "A0=" GUARD
           COMPUTE R1 = FUNCTION ANNUITY(0, N20)
           IF R1 = 0.00000000000000000001
               DISPLAY "T1=OK"
           ELSE
               DISPLAY "T1=BAD"
           END-IF
           COMPUTE R2 = FUNCTION ANNUITY(0, N30)
           IF R2 = 0.000000000000000000000000000001
               DISPLAY "T2=OK"
           ELSE
               DISPLAY "T2=BAD"
           END-IF
           IF FUNCTION ANNUITY(0, N20) = 1 / N20
               DISPLAY "T3=OK"
           ELSE
               DISPLAY "T3=BAD"
           END-IF
           COMPUTE R3 = FUNCTION ANNUITY(0, N19)
           IF R3 = 0.0000000000000000001
               DISPLAY "T4=OK"
           ELSE
               DISPLAY "T4=BAD"
           END-IF
           COMPUTE R4 = FUNCTION ANNUITY(0.000000000000000000000000000001, N20)
           IF R4 = .0000000000000000000100000000005
               DISPLAY "T5=OK"
           ELSE
               DISPLAY "T5=BAD"
           END-IF
           COMPUTE R5 = FUNCTION ANNUITY(0.05, N6)
               ON SIZE ERROR
                   DISPLAY "T6=SIZE"
               NOT ON SIZE ERROR
                   DISPLAY "T6=VALUE"
           END-COMPUTE
           COMPUTE R5 = FUNCTION ANNUITY(0.05, N20)
               ON SIZE ERROR
                   DISPLAY "T7=SIZE"
               NOT ON SIZE ERROR
                   DISPLAY "T7=VALUE"
           END-COMPUTE
           STOP RUN.
       END PROGRAM PB637ANN.
