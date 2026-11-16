      *>--- conformance-case
      *> id: g2-concat-boolean-161
      *> rules: [2002-SR-8.8.3.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.3.2 SR3 - "For operands of class boolean, the length
      *> of the value resulting from concatenation shall be less than or
      *> equal to 160 boolean character positions." Four 40-position
      *> literals and a one-position literal make 161 positions, one
      *> more than the bound, so the entry is refused at 2002 (the
      *> 160-position value is accepted by g2-concat-160-bound).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAT03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC 1(170) VALUE
           B"1111111111111111111111111111111111111111"
           & B"1111111111111111111111111111111111111111"
           & B"1111111111111111111111111111111111111111"
           & B"0000000000000000000000000000000000000001"
           & B"1".
       PROCEDURE DIVISION.
           STOP RUN.
