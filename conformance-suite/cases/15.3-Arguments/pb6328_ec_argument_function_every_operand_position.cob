*>--- conformance-case
*> id: pb6328_ec_argument_function_every_operand_position
*> rules: [AR-15.3-14]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> §2023:15.3 AR14 (cite.py --check passed): "If the evaluation of an argument results in an incorrect value for that
      *> argument or for the returned value according to the rules specified in the function definition and no exception
      *> condition was raised during item identification or expression evaluation, the EC-ARGUMENT-FUNCTION exception
      *> condition is set to exist." The rule attaches to the FUNCTION REFERENCE: it names no statement kind and no
      *> condition kind, so one reference with one bad argument raises wherever the reference is written. The same
      *> sentence stands in §2002:15.2 and §2014:15.3, which is why this test is accepted from 2002, the edition that
      *> introduced both the condition and the >>TURN directive.
      *>
      *> The violating argument is the one the sibling test pb26_ec_argument_function_every_statement uses: FUNCTION
      *> CHAR(0) (§2023:15.15.3 AR2: "The value of argument-1 shall be greater than zero"). CHAR returns an alphanumeric item, which
      *> every position below accepts: a class condition's identifier-1 (§2023:8.8.4.4.3 SR8, NUMERIC: usage display)
      *> and a CANCEL target (§2023:14.9.5.3 SR1: "Identifier-1 shall be defined as an alphanumeric or national data item").
      *>
      *> WHAT FAILED (kb/Work PB6328, PB6223). Whether a statement is guarded for the condition was decided by a hand
      *> switch over condition kinds ending in `_ => false` (a relation was covered, a class condition, an
      *> alphabet-name class condition, a range's bounds and a window evaluated once per evaluation were not) and by a
      *> generated walk that did not enter the value tuple a CANCEL holds its dynamic name in. Each line below prints
      *> its position and then the declarative's CAUGHT; a position whose CAUGHT is missing is that defect returning.
      *> Every condition is evaluated at run time (W-Q is 0, so the OR's second operand runs; M is inside the reversed
      *> alphabet's range from Z), so a raise that fails to come is the guard missing and not a skipped evaluation.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. P6328OPSF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R.
           MOVE 1 TO R
           GOBACK.
       END FUNCTION P6328OPSF.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6328OPS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA"
           ALPHABET A7 IS STANDARD-1
           CLASS DIGS IS "0" THRU "9".
       REPOSITORY.
           FUNCTION P6328OPSF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-Z PIC 9 VALUE 0.
       01 W-Q PIC 9 VALUE 0.
       01 W-X PIC X VALUE "M".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "  CAUGHT".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "1-RELATION".
           IF FUNCTION CHAR(W-Z) = "A" CONTINUE END-IF.
           DISPLAY "2-CLASS-NUMERIC".
           IF FUNCTION CHAR(W-Z) IS NUMERIC CONTINUE END-IF.
           DISPLAY "3-USER-CLASS".
           IF FUNCTION CHAR(W-Z) IS DIGS CONTINUE END-IF.
           DISPLAY "4-ALPHABET-CLASS".
           IF FUNCTION CHAR(W-Z) IS A7 CONTINUE END-IF.
           DISPLAY "5-RANGE-SUBJECT".
           EVALUATE FUNCTION CHAR(W-Z)
               WHEN "A" THRU "Z" IN AL CONTINUE
               WHEN OTHER CONTINUE
           END-EVALUATE.
           DISPLAY "6-RANGE-LOW-BOUND".
           EVALUATE W-X
               WHEN FUNCTION CHAR(W-Z) THRU "A" IN AL CONTINUE
               WHEN OTHER CONTINUE
           END-EVALUATE.
           DISPLAY "7-RANGE-HIGH-BOUND".
           EVALUATE W-X
               WHEN "Z" THRU FUNCTION CHAR(W-Z) IN AL CONTINUE
               WHEN OTHER CONTINUE
           END-EVALUATE.
           DISPLAY "8-WINDOW-RELATION".
           IF W-Q = 1 OR FUNCTION P6328OPSF = FUNCTION LOG10(W-Z)
               CONTINUE
           END-IF.
           DISPLAY "9-WINDOW-CLASS".
           IF W-Q = 1 OR (FUNCTION CHAR(W-Z) IS NUMERIC
               AND FUNCTION P6328OPSF = 1)
               CONTINUE
           END-IF.
           DISPLAY "10-CANCEL-TARGET".
           CANCEL FUNCTION CHAR(W-Z).
           DISPLAY "DONE".
           STOP RUN.
