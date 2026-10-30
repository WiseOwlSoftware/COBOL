      *>--- conformance-case
      *> id: pb4093_truncation_not_enabled_stores_low_order_digits
      *> rules: [GR-14.7.5-4, GR-14.6.13.1.3-8, GR-7.3.25.4-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB4093 - a result further from zero than its resultant
      *> identifier permits, while checking for EC-SIZE-TRUNCATION is
      *> NOT enabled, is stored as its low-order digits, whatever other
      *> EC-SIZE name is enabled.
      *>
      *> ISO 14.7.5 no-phrase rule 4: "if the result of the arithmetic
      *> statement is a value further from zero than permitted for the
      *> associated resultant data item, the EC-SIZE-TRUNCATION
      *> exception condition is set to exist". 14.6.13.1.1 NOTE 3: "An
      *> exception condition is set to exist only if checking for that
      *> condition is enabled" - so with EC-SIZE-OVERFLOW (or
      *> EC-SIZE-ZERO-DIVIDE) enabled alone the statement has no phrase
      *> and no enabled condition to take, and 14.6.13.1.3 item 8's
      *> not-enabled disposition applies, which CONFORMANCE.md
      *> DOC-A.1-70 documents: execution continues and the resultant
      *> identifier receives the LOW-ORDER digits of the result aligned
      *> at its scale. With EC-SIZE-TRUNCATION enabled the condition is
      *> raised, 14.6.13.1.3 processes it, and the statement does not
      *> store (the control block).
      *>
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THAT RULE. WS-A = WS-B =
      *> 999, so WS-A x WS-B = 998001.
      *>   a  MULTIPLY GIVING PIC 999:  low three digits  001
      *>   b  ADD WS-A WS-A TO WS-C (007): 7 + 1998 = 2005 -> 005
      *>   c  COMPUTE into PIC 99 COMP-3: low two digits  01
      *>   d  COMPUTE into PIC ZZ9: low three digits 001, edited "  1"
      *>   e  COMPUTE into PIC 999 and PIC 99 (two receivers): 001 and 01
      *>   f  DIVIDE WS-F1 BY WS-F2 GIVING WS-V REMAINDER WS-R, WS-F1 the
      *>      binary64 value 2 ** 130 = 1361129467683753853853498429727
      *>      072845824 (exact in binary64; a floating-point literal
      *>      carries at most 36 digits, so it is computed), WS-F2 1:
      *>      the quotient is
      *>      that integer, past PIC 999, so its low three digits 824
      *>      (the REMAINDER statement's subsidiary quotient is formed on
      *>      the same binary64 value and meets the same not-enabled
      *>      disposition, never the checked landing)
      *> Steps 1-5 and 16 enable EC-SIZE-OVERFLOW alone, steps 6-10
      *> EC-SIZE-ZERO-DIVIDE alone: the same values, nothing raised.
      *> Steps 11-15 and 17 enable EC-SIZE-TRUNCATION: each statement
      *> raises it, the declarative resumes at the next statement, and
      *> every receiver keeps the 007 / 07 / "  7" it held.
      *> The leg fails if a receiver at steps 1-10 keeps its old value
      *> (the family-wide Try store left it unchanged as if a SIZE
      *> ERROR phrase had run) or if a raise appears at those steps.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4093LO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A      PIC 999        VALUE 999.
       01  WS-B      PIC 999        VALUE 999.
       01  WS-C      PIC 999        VALUE 7.
       01  WS-P      PIC 99 COMP-3  VALUE 7.
       01  WS-E      PIC ZZ9.
       01  WS-Q      PIC 99         VALUE 7.
       01  WS-V      PIC 999        VALUE 7.
       01  WS-R      PIC 999        VALUE 7.
       01  WS-F1     COMP-2         VALUE 0.
       01  WS-F2     COMP-2         VALUE 1.
       01  WS-F3     COMP-2         VALUE 2.
       01  WS-STEP   PIC 99         VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SZ SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE.
       SZ-1.
           DISPLAY "RAISED " FUNCTION EXCEPTION-STATUS
               " AT STEP " WS-STEP.
           RESUME NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           COMPUTE WS-F1 = WS-F3 ** 130.
       >>TURN EC-SIZE-OVERFLOW CHECKING ON
           MOVE 1 TO WS-STEP.
           MOVE 7 TO WS-C.
           MULTIPLY WS-A BY WS-B GIVING WS-C.
           DISPLAY "C=" WS-C.
           MOVE 2 TO WS-STEP.
           MOVE 7 TO WS-C.
           ADD WS-A WS-A TO WS-C.
           DISPLAY "C=" WS-C.
           MOVE 3 TO WS-STEP.
           MOVE 7 TO WS-P.
           COMPUTE WS-P = WS-A * WS-B.
           DISPLAY "P=" WS-P.
           MOVE 4 TO WS-STEP.
           MOVE 7 TO WS-E.
           COMPUTE WS-E = WS-A * WS-B.
           DISPLAY "E=[" WS-E "]".
           MOVE 5 TO WS-STEP.
           MOVE 7 TO WS-C.
           MOVE 7 TO WS-Q.
           COMPUTE WS-C WS-Q = WS-A * WS-B.
           DISPLAY "C=" WS-C " Q=" WS-Q.
           MOVE 16 TO WS-STEP.
           MOVE 7 TO WS-V.
           DIVIDE WS-F1 BY WS-F2 GIVING WS-V REMAINDER WS-R.
           DISPLAY "V=" WS-V.
       >>TURN EC-SIZE-OVERFLOW CHECKING OFF
       >>TURN EC-SIZE-ZERO-DIVIDE CHECKING ON
           MOVE 6 TO WS-STEP.
           MOVE 7 TO WS-C.
           MULTIPLY WS-A BY WS-B GIVING WS-C.
           DISPLAY "C=" WS-C.
           MOVE 7 TO WS-STEP.
           MOVE 7 TO WS-C.
           ADD WS-A WS-A TO WS-C.
           DISPLAY "C=" WS-C.
           MOVE 8 TO WS-STEP.
           MOVE 7 TO WS-P.
           COMPUTE WS-P = WS-A * WS-B.
           DISPLAY "P=" WS-P.
           MOVE 9 TO WS-STEP.
           MOVE 7 TO WS-E.
           COMPUTE WS-E = WS-A * WS-B.
           DISPLAY "E=[" WS-E "]".
           MOVE 10 TO WS-STEP.
           MOVE 7 TO WS-C.
           MOVE 7 TO WS-Q.
           COMPUTE WS-C WS-Q = WS-A * WS-B.
           DISPLAY "C=" WS-C " Q=" WS-Q.
       >>TURN EC-SIZE-ZERO-DIVIDE CHECKING OFF
       >>TURN EC-SIZE-TRUNCATION CHECKING ON
           MOVE 11 TO WS-STEP.
           MOVE 7 TO WS-C.
           MULTIPLY WS-A BY WS-B GIVING WS-C.
           DISPLAY "C=" WS-C.
           MOVE 12 TO WS-STEP.
           MOVE 7 TO WS-C.
           ADD WS-A WS-A TO WS-C.
           DISPLAY "C=" WS-C.
           MOVE 13 TO WS-STEP.
           MOVE 7 TO WS-P.
           COMPUTE WS-P = WS-A * WS-B.
           DISPLAY "P=" WS-P.
           MOVE 14 TO WS-STEP.
           MOVE 7 TO WS-E.
           COMPUTE WS-E = WS-A * WS-B.
           DISPLAY "E=[" WS-E "]".
           MOVE 15 TO WS-STEP.
           MOVE 7 TO WS-C.
           MOVE 7 TO WS-Q.
           COMPUTE WS-C WS-Q = WS-A * WS-B.
           DISPLAY "C=" WS-C " Q=" WS-Q.
           MOVE 17 TO WS-STEP.
           MOVE 7 TO WS-V.
           DIVIDE WS-F1 BY WS-F2 GIVING WS-V REMAINDER WS-R.
           DISPLAY "V=" WS-V.
       >>TURN EC-SIZE-TRUNCATION CHECKING OFF
           STOP RUN.
