      *>--- conformance-case
      *> id: pb3830-compute-all-zero-numeric
      *> rules: [SR-8.3.3.6.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3830, branch review R2-1 sibling sweep. ISO 8.3.3.6.3
      *> SR1 a): "If the literal is restricted to a numeric literal, the only
      *> figurative constant permitted is ZERO (ZEROS, ZEROES) without the
      *> ALL phrase" (cite.py --check 8.3.3.6.3 -> OK). The right side of
      *> COMPUTE X, X numeric, is an arithmetic expression (8.8.1.1), so ALL
      *> ZERO there is barred from 2002 (VCR Table 7 row 7.28, kb/Work
      *> PB1415) exactly as in ADD ALL ZERO TO X; at 85 the ALL is redundant
      *> (conformance pb1415_all_zero_numeric_85). Before the fix the COMPUTE
      *> re-route built a bare zero and compiled this clean at 2023, while
      *> ADD ALL ZERO TO X was refused COBOL0902.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3830N8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X    PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE X = ALL ZERO
           DISPLAY "X=" X
           STOP RUN.
