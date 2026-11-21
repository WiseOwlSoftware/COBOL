      *>--- conformance-case
      *> id: pb5891-impure-identity
      *> rules: [AR-15.3-6]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 refused FUNCTION RANDOM - FUNCTION RANDOM +
      *> 1.
      *>
      *> kb/Work PB5889 (the ruling of 2026-11-21): the integer-
      *> argument screen refuses an argument only when it can name a
      *> value of the operands at which the expression is not an
      *> integer, and accepts the rest with a run-time check of the
      *> value. §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value or an
      *> integer data item shall be specified"; §1989:A.2.2 AR4
      *> (cite.py --check) the same at COBOL-85.
      *>
      *> Two references to RANDOM are two values, so the argument is
      *> an integer only if they are equal; the screen names no
      *> counterexample (RANDOM's values are the function's), and the
      *> value is checked at run time. The declarative shows nothing,
      *> so the run ends DONE whichever way the check decides
      *> (EXPECTED: DONE).
      *>
      *> Checking for EC-ARGUMENT-FUNCTION is on (§2023:15.3 AR14).
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891A2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R  PIC S9(18).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD
               (FUNCTION RANDOM - FUNCTION RANDOM + 1, 7).
           DISPLAY "DONE".
           STOP RUN.
