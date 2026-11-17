      *>--- conformance-case
      *> id: pb5892-concat-p-scaled-fraction
      *> rules: [AR-15.18.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5892 - the value test admits a P-scaled item only
      *> when its value is an integer: V PIC P9 holds .05 (the P is an
      *> assumed zero to the LEFT, scale 2), so it is not an integer
      *> and stays refused (COBOL1627), as a V-scaled item is
      *> (pb58-concat-frac).
      *>
      *> §2023:15.18.3 AR3 (cite.py --check): "If argument-1 or
      *> argument-2 is numeric, it shall be usage display or national
      *> and shall be an unsigned integer."
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5892B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  V   PIC P9 VALUE .05.
       01  S   PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION CONCAT ("A" V) TO S.
           DISPLAY "[" S "]".
           STOP RUN.
