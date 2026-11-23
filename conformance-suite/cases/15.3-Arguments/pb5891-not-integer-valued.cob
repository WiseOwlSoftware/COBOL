      *>--- conformance-case
      *> id: pb5891-not-integer-valued
      *> rules: [AR-15.3-6, AR-15.36.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 - a polynomial argument that is not an integer
      *> for every value of its term is refused (COBOL1627) at every
      *> edition.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified"; §1989:A.2.2
      *> AR4 (cite.py --check): "An arithmetic expression which
      *> will always result in an integer value must be specified".
      *> The screen decides exactly (kb/Work PB5891): the argument's
      *> value is a polynomial in its terms, admitted only when every
      *> intermediate is exact and the polynomial is an integer for
      *> every value of its terms.
      *> N * (N + 1) / 3 is 2/3 at N = 1, a value with no finite
      *> decimal, so the arithmetic does not compute the quotient
      *> exactly and it is not always an integer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891A1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 99 VALUE 2.
       01  R  PIC S9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION FACTORIAL (N * (N + 1) / 3).
           STOP RUN.
