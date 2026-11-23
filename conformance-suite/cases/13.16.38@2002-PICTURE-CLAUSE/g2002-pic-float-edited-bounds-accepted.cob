      *>--- conformance-case
      *> id: g2002-pic-float-edited-bounds-accepted
      *> rules: [2002-GR-13.16.38.3-12, 2002-SR-13.16.38.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *>---
      *> §2002:13.16.38.3 GR12 - "The exponent shall be '+9', '+99',
      *>   '+999', or '+9(n)' where n = 1, 2, or 3."  B is the longest
      *>   exponent 2002 allows (+999) and C the shortest (+9).
      *> §2002:13.16.38.2 SR6 - the digit positions of a numeric-edited
      *>   item "shall range from 1 through 31": A carries 30 + 1 = 31
      *>   digit positions in its significand, the most the rule allows.
      *> Each symbol is counted in the size of the item (§2002:13.16.38.3
      *>   GR13, the symbols 'E' and the signs among them), so
      *>   A  +9(30).9E+999  is 1 + 30 + 1 + 1 + 1 + 4 = 38 positions,
      *>   B  +9.9E+999      is 1 + 1 + 1 + 1 + 1 + 4 = 9 positions,
      *>   C  +9.9E+9        is 1 + 1 + 1 + 1 + 1 + 2 = 7 positions.
      *> Fails if the compiler refuses a bound the rule allows: the
      *> later editions' four-digit exponent and 36 digits are refused
      *> only below them (g2002-pic-float-edited-exponent-4-refused and
      *> g2002-pic-float-edited-significand-32-refused).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PFB1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC +9(30).9E+999.
       01  B PIC +9.9E+999.
       01  C PIC +9.9E+9.
       01  N PIC 99.
       01  NX REDEFINES N PIC XX.
       PROCEDURE DIVISION.
           MOVE FUNCTION LENGTH(A) TO N
           DISPLAY "A=" NX
           MOVE FUNCTION LENGTH(B) TO N
           DISPLAY "B=" NX
           MOVE FUNCTION LENGTH(C) TO N
           DISPLAY "C=" NX
           STOP RUN.
