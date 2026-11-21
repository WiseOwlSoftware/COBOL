      *>--- conformance-case
      *> id: pb5891-wide-exponent-power
      *> rules: [AR-15.3-6]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 refused 1 ** (X * X), a power with a wide
      *> exponent, in a sum on the SDIDI.
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
      *> The sum above the power is on the SDIDI, where X * 10.25
      *> needs 35 significant digits (§2023:8.8.1.5.2) and is rounded
      *> to one fraction digit, so the value is no integer; the exact
      *> value has no counterexample, so the computed value is
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
       PROGRAM-ID. PB5891H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X  PIC 9(31) VALUE 9999999999999999999999999999999.
       01  Y  PIC 9(31) VALUE 2050000000000000000000000000000.
       01  R  PIC 9(4).
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
           COMPUTE R = FUNCTION MOD ((Y * 100 - (1 ** (X * X) - 1
               + X * 10.25)) * 4, 1000).
           DISPLAY "DONE".
           STOP RUN.
