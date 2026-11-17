      *>--- conformance-case
      *> id: pb5886-power-shapes
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
      *> The screen fails closed and decides exactly (kb/Work
      *> PB5886, PB5891): the argument's value is a polynomial in
      *> its terms (a quotient by a nonzero literal is the product
      *> by its reciprocal; a function is a term, of the type its
      *> definition gives for its arguments), and it is admitted
      *> only when every intermediate is exact and the polynomial is
      *> an integer for every value of its terms.
      *> a power is an integer term only for an integer base to a
      *> non-negative integer literal or an unsigned integer item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5886J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  K  PIC 9V9 VALUE 0.5.
       01  R  PIC S9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (1024 ** K, 7).
           STOP RUN.
