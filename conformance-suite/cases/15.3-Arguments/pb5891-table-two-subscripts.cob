      *>--- conformance-case
      *> id: pb5891-table-two-subscripts
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 review R1-1 - two elements of a table under
      *> two subscripts are two terms (COBOL1627).
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified"; §1989:A.2.2
      *> AR4 (cite.py --check): "An arithmetic expression which
      *> will always result in an integer value must be specified".
      *> T (I) * (T (J) + 1) / 2 is 1/2 at T (I) = 1, T (J) = 0, so
      *> it is not always an integer. Its complement, T (I) * (T (I)
      *> + 1) / 2, is admitted (pb5891_polynomial_integer_arguments).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TB.
           05  T  PIC 99 OCCURS 3 TIMES.
       01  I  PIC 9 VALUE 1.
       01  J  PIC 9 VALUE 2.
       01  R  PIC S9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO T (1).
           MOVE 0 TO T (2).
           COMPUTE R = FUNCTION FACTORIAL (T (I) * (T (J) + 1) / 2).
           STOP RUN.
