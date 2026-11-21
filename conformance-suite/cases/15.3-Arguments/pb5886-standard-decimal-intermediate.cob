      *>--- conformance-case
      *> id: pb5886-standard-decimal-intermediate
      *> rules: [AR-15.3-6]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5886 refused B27 / 0.0001 / 16384 * 16384 * 0.0001
      *> under STANDARD-DECIMAL.
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
      *> Its exact value is B27, but a standard-decimal intermediate
      *> keeps 34 significant digits (§2023:8.8.1.5.2), and the
      *> quotient by 16384 needs 37 at B27 = 999...9, so the
      *> expression's value there is ...998.9999. The exact value has
      *> no counterexample, so the value the arithmetic computes is
      *> checked at run time.
      *>
      *> From COBOL-2002 a value that is not an integer is an
      *> incorrect argument: §2023:15.3 AR14 (cite.py --check) "the
      *> EC-ARGUMENT-FUNCTION exception condition is set to exist".
      *> Checking is turned on, so the declarative shows RAISED and
      *> RESUME continues with the next statement (EXPECTED: RAISED,
      *> DONE). At COBOL-85 the same run terminates
      *> (pb5889_run_time_check_85).
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5886L.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  B27 PIC 9(27) VALUE 999999999999999999999999999.
       01  R   PIC 9(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "RAISED".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD
               (B27 / 0.0001 / 16384 * 16384 * 0.0001, 1000).
           DISPLAY "DONE".
           STOP RUN.
