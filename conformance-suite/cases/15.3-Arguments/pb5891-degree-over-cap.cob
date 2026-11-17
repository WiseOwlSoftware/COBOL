      *>--- conformance-case
      *> id: pb5891-degree-over-cap
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 - a polynomial whose integer-valued test would
      *> need the grid past its stated cap is not shown and is
      *> refused (COBOL1627).
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
      *> (N ** 13 - N) / 13 is an integer for every N (Fermat), but
      *> its degree 13 passes the cap of 12 (docs/INTRINSICS_DESIGN.md,
      *> the type-6 intake, rule 3) and 1/13 has no finite decimal,
      *> so the quotient is not shown exact. A refusal of legal
      *> source by the stated cap.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891A3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 99 VALUE 2.
       01  R  PIC S9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD ((N ** 13 - N) / 13, 7).
           STOP RUN.
