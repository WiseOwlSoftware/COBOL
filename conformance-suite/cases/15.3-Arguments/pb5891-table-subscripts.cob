      *>--- conformance-case
      *> id: pb5891-table-subscripts
      *> rules: [AR-15.3-6, AR-15.36.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 - two elements of a table are two terms: T (1)
      *> - T (2) over a scaled element is not an integer (COBOL1627).
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
      *> T (1) - T (2) is 0.1 at T (1) = 0.1 and T (2) = 0. The
      *> screen before kb/Work PB5891 keyed an item by its
      *> description and cancelled the two elements.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891A4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TB.
           05  T  PIC 9V9 OCCURS 2 TIMES.
       01  R  PIC S9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 0.5 TO T (1).
           MOVE 0.5 TO T (2).
           COMPUTE R = FUNCTION FACTORIAL (T (1) - T (2) + 1).
           STOP RUN.
