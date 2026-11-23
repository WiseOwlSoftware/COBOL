      *>--- conformance-case
      *> id: pb5889_run_time_check_85
      *> rules: [AR-15.3-6, AR-15.64.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5889 - at COBOL-85 an integer argument the screen
      *> neither proves nor refutes is accepted, and a value that is
      *> not an integer at run time terminates the run unit.
      *>
      *> §1989:A.2.2 AR4 (cite.py --check): "An arithmetic
      *> expression which will always result in an integer value
      *> must be specified". FUNCTION ORD (A) / 2 is an integer for
      *> some values of A and not for others; the screen does not
      *> know ORD's values, so it names no counterexample and the
      *> program is accepted. The 1985 text has no exception
      *> conditions: a program that violates a rule has undefined
      *> results (§1985:I.1.6 "The results of violating the formats
      *> or rules of Standard COBOL are undefined"), and the
      *> Correction Amendment's clarification "allows an
      *> implementation of amended third Standard COBOL to terminate
      *> processing" (§1993:p.B-46). The run unit is terminated with
      *> a message naming the function, the argument and the value
      *> (docs/conformance D-INTEGER-ARGUMENT-SCREEN).
      *> EXPECTED: ORD ("A") is 66 in the native collating sequence,
      *> so the first argument is 33 and MOD (33, 7) = 5 (INTEGER
      *> 0005); then ORD ("B") / 2 is 33.5, the run ends after BEFORE
      *> and AFTER is never shown.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5889T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A  PIC X VALUE "A".
       01  R  PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           COMPUTE R = FUNCTION MOD (FUNCTION ORD (A) / 2, 7).
           DISPLAY "INTEGER " R.
           MOVE "B" TO A.
           DISPLAY "BEFORE".
           COMPUTE R = FUNCTION MOD (FUNCTION ORD (A) / 2, 7).
           DISPLAY "AFTER " R.
           STOP RUN.
