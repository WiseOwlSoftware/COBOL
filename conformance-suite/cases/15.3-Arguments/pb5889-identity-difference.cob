      *>--- conformance-case
      *> id: pb5889-identity-difference
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5889 (a recorded decision) - an integer argument
      *> that is an integer only by an algebraic identity between
      *> terms that are not integer terms is refused (COBOL1627) at
      *> every edition. It replaces pb4795_identity_difference_legal.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified"; §1989:A.2.2
      *> AR4 the same. SQRT (N) - SQRT (N) + 5 is 5 for every N,
      *> but SQRT is a numeric function and the screen decides by
      *> the types of the terms and the values of the literals,
      *> never by algebra, so the program is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5889A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 9 VALUE 2.
       01  R  PIC S9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (FUNCTION SQRT (N)
               - FUNCTION SQRT (N) + 5, 7).
           STOP RUN.
