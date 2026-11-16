      *>--- conformance-case
      *> id: g85_f34_02
      *> rules: [1989-FMT-A.2.9.2, 1989-AR-A.2.9.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.9.2 - FUNCTION CHAR ( argument-1 ) is accepted (all
      *> lines). §1989:A.2.9.3 AR1 - argument-1 is an integer: an
      *> integer literal, an integer data item and an integer arithmetic
      *> expression (§1989:A.2.2 AR4) are each accepted and each selects
      *> the ordinal position its value names (§1989:A.2.9.1).
      *> §1985:VI.4.5.4 GR4d2: the order of the ALPHABET literals gives
      *> the ordinal numbers, so under MY-ALPHA position 1 is Q, 2 is R,
      *> 3 is S. A line fails if an integer form is refused or picks the
      *> wrong position.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3402.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE IS MY-ALPHA.
       SPECIAL-NAMES.
           ALPHABET MY-ALPHA IS "Q" "R" "S".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  C  PIC X.
       01  N  PIC 9(3) VALUE 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION CHAR ( 1 ) TO C.
           DISPLAY "CHAR-LIT=" C.
           MOVE FUNCTION CHAR ( N ) TO C.
           DISPLAY "CHAR-ID=" C.
           MOVE FUNCTION CHAR ( N + 1 ) TO C.
           DISPLAY "CHAR-EXPR=" C.
           STOP RUN.
