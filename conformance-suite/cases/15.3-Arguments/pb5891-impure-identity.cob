      *>--- conformance-case
      *> id: pb5891-impure-identity
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 - two references of a function whose value its
      *> arguments do not fix are two terms, so they do not cancel
      *> (COBOL1627).
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
      *> RANDOM returns the next value of its sequence at each
      *> reference, so RANDOM - RANDOM + 1 is not the constant 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891A2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R  PIC S9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD
               (FUNCTION RANDOM - FUNCTION RANDOM + 1, 7).
           STOP RUN.
