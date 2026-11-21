      *>--- conformance-case
      *> id: pb5889_run_time_check
      *> rules: [AR-15.3-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5889 - from COBOL-2002 an integer argument the
      *> screen neither proves nor refutes is accepted, and a value
      *> that is not an integer at run time is EC-ARGUMENT-FUNCTION.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value or
      *> an integer data item shall be specified". FUNCTION ORD (A)
      *> / 2 is an integer for some values of A and not for others;
      *> the screen does not know ORD's values, so it names no
      *> counterexample and the program is accepted (§2023:4.2.2 "it
      *> is left to the implementor's discretion to determine what
      *> is syntactically distinguishable"). At run time an
      *> incorrect value is §2023:15.3 AR14 (cite.py --check): "the
      *> EC-ARGUMENT-FUNCTION exception condition is set to exist".
      *> Checking is on, so the declarative runs and RESUME goes on
      *> with the next statement.
      *> EXPECTED: ORD ("A") is 66 in the native collating sequence,
      *> so MOD (33, 7) = 5 (INTEGER 0005); ORD ("B") / 2 is 33.5,
      *> so RAISED, then DONE.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5889C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A  PIC X VALUE "A".
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
       P1.
           COMPUTE R = FUNCTION MOD (FUNCTION ORD (A) / 2, 7).
           DISPLAY "INTEGER " R.
           MOVE "B" TO A.
           COMPUTE R = FUNCTION MOD (FUNCTION ORD (A) / 2, 7).
           DISPLAY "DONE".
           STOP RUN.
