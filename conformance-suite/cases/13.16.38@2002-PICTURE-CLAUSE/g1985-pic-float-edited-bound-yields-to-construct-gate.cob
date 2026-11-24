      *>--- conformance-case
      *> id: g1985-pic-float-edited-bound-yields-to-construct-gate
      *> rules: [2002-SR-13.16.38.2-6]
      *> source-format: fixed
      *>---
      *> §2002:13.16.38.3 GR12 - "The exponent shall be '+9', '+99',
      *>   '+999', or '+9(n)' where n = 1, 2, or 3."  A floating-point
      *>   numeric-edited PICTURE (the symbol E) is a COBOL-2002
      *>   introduction: ANSI X3.23-1985 has no such form, so a COBOL-85
      *>   compiler must refuse the form itself (COBOL0900, the
      *>   construct gate).  +9(40)E+99 also exceeds every later
      *>   edition's digit bound, but below COBOL-2002 there is no bound
      *>   to measure: a digit-count error (COBOL1658) that named a total
      *>   under a 2002 rule would hide the gate and tell the user about
      *>   a count instead of that the form is not available.  The case
      *>   fails if the refusal is not the construct gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85PFG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC +9(40)E+99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "NOT REFUSED"
           STOP RUN.
