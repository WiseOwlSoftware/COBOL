      *>--- conformance-case
      *> id: pb5886-random-seed-quotient
      *> rules: [AR-15.3-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5886 refused FUNCTION ORD (A) / 2 as RANDOM's
      *> seed.
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
      *> ORD (A) / 2 is 33.5 for A = "B"; the value is checked at run
      *> time.
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
       PROGRAM-ID. PB5886G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A  PIC X VALUE "B".
       01  R  PIC S9(18).
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
           COMPUTE R = FUNCTION RANDOM (FUNCTION ORD (A) / 2) * 1000.
           DISPLAY "DONE".
           STOP RUN.
