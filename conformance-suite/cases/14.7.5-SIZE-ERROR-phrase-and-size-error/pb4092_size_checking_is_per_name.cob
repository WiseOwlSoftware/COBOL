      *>--- conformance-case
      *> id: pb4092_size_checking_is_per_name
      *> rules: [GR-14.7.5-3, GR-14.7.5-4, GR-7.3.25.4-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB4092 and PB4093 - checking for ONE EC-SIZE name is checking
      *> for that name, never for the family.
      *>
      *> ISO 14.6.13.1.1: "If checking for an exception condition is
      *> enabled and an exception status indicator is set as a result
      *> of an exception detected during the execution of a statement,
      *> the associated exception condition is raised", and its NOTE 3:
      *> "An exception condition is set to exist only if checking for
      *> that condition is enabled". 14.7.5 no-phrase rule 3 names
      *> EC-SIZE-OVERFLOW and EC-SIZE-UNDERFLOW for an intermediate
      *> past its range, rule 4 EC-SIZE-TRUNCATION for a result further
      *> from zero than its resultant identifier permits, and rule 2
      *> EC-SIZE-ZERO-DIVIDE. A condition whose checking is not enabled
      *> takes 14.6.13.1.3 item 8's not-enabled disposition, which
      *> CONFORMANCE.md DOC-A.1-70 documents: execution continues, the
      *> resultant identifier receives the LOW-ORDER digits of a result
      *> it cannot hold, and a binary64 intermediate outside its range
      *> is the IEEE value (DOC-A.1-179).
      *>
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THAT RULE. WS-D = 1.0E200
      *> so WS-D x WS-D leaves the binary64 intermediate upward (IEEE
      *> +Infinity, which is > 0); WS-T = 1.0E-200 so WS-T x WS-T is a
      *> nonzero value that rounds to zero (IEEE 0, not > 0). WS-A x WS-A
      *> = 10000 does not fit WS-C PIC 999: its low-order digits are 000.
      *> WS-C starts every step at 007 so an unchanged receiver shows.
      *>   STEP 1 TRUNCATION on:  IF WS-D * WS-D > 0 raises nothing (the
      *>          overflow is not enabled): T
      *>   STEP 2 TRUNCATION on:  COMPUTE raises EC-SIZE-TRUNCATION, the
      *>          receiver is not stored: 007
      *>   STEP 3 OVERFLOW on:    the same COMPUTE raises nothing and
      *>          stores the low-order digits: 000
      *>   STEP 4 OVERFLOW on:    IF WS-D * WS-D > 0 raises
      *>          EC-SIZE-OVERFLOW, the declarative resumes after the IF
      *>   STEP 5 UNDERFLOW on:   IF WS-D * WS-D > 0: T, nothing raised
      *>   STEP 6 UNDERFLOW on:   IF WS-T * WS-T > 0 raises
      *>          EC-SIZE-UNDERFLOW
      *>   STEP 7 OVERFLOW on:    IF WS-T * WS-T > 0 raises nothing: F
      *>   STEP 8 ZERO-DIVIDE on: the truncating COMPUTE: 000, no raise
      *>   STEP 9 ZERO-DIVIDE on: COMPUTE WS-C = WS-A / WS-Z raises
      *>          EC-SIZE-ZERO-DIVIDE, the receiver is not stored: 007
      *>   STEP 10 EXPONENTIATION on: the truncating COMPUTE: 000
      *>   STEP 11 the EC-SIZE family on: the COMPUTE raises
      *>          EC-SIZE-TRUNCATION: 007
      *>   STEP 12 the EC-SIZE family on: IF WS-D * WS-D > 0 raises
      *>          EC-SIZE-OVERFLOW
      *> The leg fails if a raise appears at a step whose name is not
      *> enabled (the family-wide checked kernels raised OVERFLOW at
      *> step 1), or if a receiver at steps 3, 8 and 10 keeps 007 (the
      *> family-wide Try store left it unchanged). The declarative
      *> RESUMEs at the next statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4092PN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-D      COMP-2    VALUE 1.0E200.
       01  WS-T      COMP-2    VALUE 1.0E-200.
       01  WS-A      PIC 999   VALUE 100.
       01  WS-Z      PIC 999   VALUE 0.
       01  WS-C      PIC 999   VALUE 7.
       01  WS-STEP   PIC 99    VALUE 0.
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
       >>TURN EC-SIZE-TRUNCATION CHECKING ON
           MOVE 1 TO WS-STEP.
           IF WS-D * WS-D > 0 DISPLAY "T" ELSE DISPLAY "F".
           MOVE 2 TO WS-STEP.
           MOVE 7 TO WS-C.
           COMPUTE WS-C = WS-A * WS-A.
           DISPLAY "C=" WS-C.
       >>TURN EC-SIZE-TRUNCATION CHECKING OFF
       >>TURN EC-SIZE-OVERFLOW CHECKING ON
           MOVE 3 TO WS-STEP.
           MOVE 7 TO WS-C.
           COMPUTE WS-C = WS-A * WS-A.
           DISPLAY "C=" WS-C.
           MOVE 4 TO WS-STEP.
           IF WS-D * WS-D > 0 DISPLAY "T" ELSE DISPLAY "F".
           MOVE 7 TO WS-STEP.
           IF WS-T * WS-T > 0 DISPLAY "T" ELSE DISPLAY "F".
       >>TURN EC-SIZE-OVERFLOW CHECKING OFF
       >>TURN EC-SIZE-UNDERFLOW CHECKING ON
           MOVE 5 TO WS-STEP.
           IF WS-D * WS-D > 0 DISPLAY "T" ELSE DISPLAY "F".
           MOVE 6 TO WS-STEP.
           IF WS-T * WS-T > 0 DISPLAY "T" ELSE DISPLAY "F".
       >>TURN EC-SIZE-UNDERFLOW CHECKING OFF
       >>TURN EC-SIZE-ZERO-DIVIDE CHECKING ON
           MOVE 8 TO WS-STEP.
           MOVE 7 TO WS-C.
           COMPUTE WS-C = WS-A * WS-A.
           DISPLAY "C=" WS-C.
           MOVE 9 TO WS-STEP.
           MOVE 7 TO WS-C.
           COMPUTE WS-C = WS-A / WS-Z.
           DISPLAY "C=" WS-C.
       >>TURN EC-SIZE-ZERO-DIVIDE CHECKING OFF
       >>TURN EC-SIZE-EXPONENTIATION CHECKING ON
           MOVE 10 TO WS-STEP.
           MOVE 7 TO WS-C.
           COMPUTE WS-C = WS-A * WS-A.
           DISPLAY "C=" WS-C.
       >>TURN EC-SIZE-EXPONENTIATION CHECKING OFF
       >>TURN EC-SIZE CHECKING ON
           MOVE 11 TO WS-STEP.
           MOVE 7 TO WS-C.
           COMPUTE WS-C = WS-A * WS-A.
           DISPLAY "C=" WS-C.
           MOVE 12 TO WS-STEP.
           IF WS-D * WS-D > 0 DISPLAY "T" ELSE DISPLAY "F".
       >>TURN EC-SIZE CHECKING OFF
           STOP RUN.
