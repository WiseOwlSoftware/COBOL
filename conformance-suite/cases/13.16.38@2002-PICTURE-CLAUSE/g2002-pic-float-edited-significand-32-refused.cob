      *>--- conformance-case
      *> id: g2002-pic-float-edited-significand-32-refused
      *> rules: [2002-SR-13.16.38.2-6]
      *> source-format: fixed
      *>---
      *> §2002:13.16.38.2 SR6 - "For data items of category numeric and
      *>   numeric-edited, the number of digit positions described by
      *>   character-string-1 shall range from 1 through 31."  The
      *>   significand of +9(32)E+99 describes 32 (COBOL-2014 admits
      *>   36 for a floating-point item, §2014:13.18.40.2 SR12), so a
      *>   COBOL-2002 compiler must refuse the PICTURE; accepting it
      *>   fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PFB3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC +9(32)E+99.
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED"
           STOP RUN.
