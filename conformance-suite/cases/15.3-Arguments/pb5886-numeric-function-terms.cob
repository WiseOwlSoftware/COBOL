      *>--- conformance-case
      *> id: pb5886-numeric-function-terms
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5886 - an integer argument the type 6 screen
      *> does not show to be always an integer is refused
      *> (COBOL1627) at every edition.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified"; §1989:A.2.2
      *> AR4 (cite.py --check): "An arithmetic expression which
      *> will always result in an integer value must be specified".
      *> The screen fails closed (kb/Work PB5886): a quotient is an
      *> integer term only by a literal whose reciprocal terminates
      *> (it is the product by that reciprocal), a function term is
      *> judged by the type its definition gives for its arguments,
      *> and a term it cannot show to be an integer is not one.
      *> MAX of a scaled item is a numeric function, which is no
      *> integer term by its type (§2023:8.4.3.2.3 SR11 judges a
      *> numeric function where an integer is required by type).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5886I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S2 PIC 9V9 VALUE 0.2.
       01  R  PIC S9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION FACTORIAL (S2 - FUNCTION MAX (S2)).
           STOP RUN.
