      *>--- conformance-case
      *> id: pb5891-table-two-subscripts
      *> rules: [AR-15.3-6]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 refused T (I) * (T (J) + 1) / 2.
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
      *> With T (1) = 1, T (2) = 0, I = 1 and J = 2 the argument is
      *> 0.5. T (I) and T (J) are one element when I = J, so the
      *> screen does not treat them as independent operands and names
      *> no counterexample; the value is checked at run time.
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
       PROGRAM-ID. PB5891E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TB.
           05  T  PIC 99 OCCURS 3 TIMES.
       01  I  PIC 9 VALUE 1.
       01  J  PIC 9 VALUE 2.
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
           MOVE 1 TO T (1).
           MOVE 0 TO T (2).
           COMPUTE R = FUNCTION FACTORIAL (T (I) * (T (J) + 1) / 2).
           DISPLAY "DONE".
           STOP RUN.
