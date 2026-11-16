      *>--- conformance-case
      *> id: g2-concat-national-161
      *> rules: [2002-SR-8.8.3.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.3.2 SR4 - "For operands of class national, the length
      *> of the value resulting from concatenation shall be less than or
      *> equal to 160 national character positions." Four 40-position
      *> literals and a one-position literal make 161 positions, one
      *> more than the bound, so the entry is refused at 2002 (the
      *> 160-position value is accepted by g2-concat-160-bound).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAT04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC N(170) VALUE
           N"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
           & N"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
           & N"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
           & N"BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBC"
           & N"D".
       PROCEDURE DIVISION.
           STOP RUN.
