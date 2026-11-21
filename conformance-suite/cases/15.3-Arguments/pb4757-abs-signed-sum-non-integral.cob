*>--- conformance-case
*> id: pb4757-abs-signed-sum-non-integral
*> rules: [AR-15.3-6]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4757 refused S2 + FUNCTION ABS (S2) over a signed
      *> S2.
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
      *> ABS (S2) is S2 where S2 is not negative (§2023:15.7.4), so
      *> the argument is 0.4 at S2 = 0.2; ABS of a scaled item is a
      *> numeric function, so the screen names no counterexample and
      *> the value is checked at run time.
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
       PROGRAM-ID. NEGPB4757ABSSGN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S2   PIC S9V9 VALUE 0.2.
       01 T    PIC 9(9).
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
           COMPUTE T = FUNCTION FACTORIAL (S2 + FUNCTION ABS (S2))
           DISPLAY "DONE".
           STOP RUN.
