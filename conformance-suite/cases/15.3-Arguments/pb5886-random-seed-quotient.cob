      *>--- conformance-case
      *> id: pb5886-random-seed-quotient
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
      *> ORD (A) / 2 is 33.5 for A = "B"; a seed is an integer
      *> argument.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5886G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A  PIC X VALUE "B".
       01  R  PIC S9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION RANDOM (FUNCTION ORD (A) / 2) * 1000.
           STOP RUN.
