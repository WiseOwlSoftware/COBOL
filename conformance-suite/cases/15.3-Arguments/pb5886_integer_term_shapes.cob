      *>--- conformance-case
      *> id: pb5886_integer_term_shapes
      *> rules: [AR-15.3-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5886 - the complement of the fail-closed type 6
      *> screen: integer terms, and sums, differences and products
      *> of them, stay integer arguments.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified"; §1989:A.2.2
      *> AR4 (cite.py --check): "An arithmetic expression which
      *> will always result in an integer value must be specified".
      *> The screen fails closed (kb/Work PB5886): a quotient is an
      *> integer term only when both operands are literals whose
      *> quotient is an integer, a function term is judged by the
      *> function's own type, and a term it cannot show to be an
      *> integer is not one.
      *> An integer function is an integer term by its type, so
      *> with A = "B" (ordinal 67 in the native collating sequence,
      *> as ORD ("A") is 66) and N = 1, MOD (x, 1000) is x for each
      *> argument below (§2023:15.64.4 RV1):
      *>   FUNCTION ORD (A)                       = 67
      *>   FUNCTION ORD (A) * 2 + 1               = 135
      *>   FUNCTION ORD (A) - FUNCTION INTEGER (N) = 66
      *>   FUNCTION ORD (A) * FUNCTION MOD (N 3)  = 67
      *>   FUNCTION ORD (A) * 0.5 * 2             = 67
      *>   7 / 7 + N (a literal quotient of 1)    = 2
      *>   N ** 2 + FUNCTION ORD (A)              = 68
      *>   - FUNCTION ORD (A) + 100               = 33
      *> A screen that refused a function term for its shape, or a
      *> literal quotient that is an integer, fails here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5886K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A  PIC X VALUE "B".
       01  N  PIC 9 VALUE 1.
       01  R  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (FUNCTION ORD (A), 1000).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (FUNCTION ORD (A) * 2 + 1, 1000).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (FUNCTION ORD (A)
               - FUNCTION INTEGER (N), 1000).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (FUNCTION ORD (A)
               * FUNCTION MOD (N 3), 1000).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (FUNCTION ORD (A) * 0.5 * 2, 1000).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (7 / 7 + N, 1000).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (N ** 2 + FUNCTION ORD (A), 1000).
           DISPLAY R.
           COMPUTE R = FUNCTION MOD (- FUNCTION ORD (A) + 100, 1000).
           DISPLAY R.
           STOP RUN.
