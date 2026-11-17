      *>--- conformance-case
      *> id: pb5345_power_failopen_shapes
      *> rules: [AR-15.3-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5345, PB5886 - the complement of the literal-base
      *> arm of the §2023:15.3 type 6 power screen: a literal integer
      *> base to an unsigned integer item is an integer argument.
      *>
      *> §2023:15.3 type 6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value or
      *> an integer data item shall be specified"; §1989:A.2.2 AR4
      *> the same. A literal base to an unsigned integer item is an
      *> integer (2 ** 2 is 4, MOD (4, 7) is 4). Since kb/Work
      *> PB5886 the screen fails closed, so 1024 ** K over a scaled K
      *> and 1 ** M over a signed M (integers at every valuation, but
      *> not by the shapes the screen admits) are refused
      *> (pb5886-power-shapes). A screen that refused a literal base
      *> for its shape alone fails here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5345A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  U PIC 9 VALUE 2.
       01  R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (2 ** U, 7).
           DISPLAY R.
           STOP RUN.
