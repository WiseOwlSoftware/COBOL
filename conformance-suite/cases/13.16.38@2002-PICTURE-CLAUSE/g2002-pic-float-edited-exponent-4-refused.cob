      *>--- conformance-case
      *> id: g2002-pic-float-edited-exponent-4-refused
      *> rules: [2002-GR-13.16.38.3-12]
      *> source-format: fixed
      *>---
      *> §2002:13.16.38.3 GR12 - "The exponent shall be '+9', '+99',
      *>   '+999', or '+9(n)' where n = 1, 2, or 3."  +9999 is four
      *>   digits (COBOL-2014 admits it, §2014:13.18.40.3 GR12), so a
      *>   COBOL-2002 compiler must refuse the PICTURE; accepting it
      *>   fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PFB2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC +9.9E+9999.
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED"
           STOP RUN.
