      *>--- conformance-case
      *> id: g2-testnumvalf-national-numeric-item
      *> rules: [2002-AR-15.77.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.77.2 AR1 - "Argument-1 shall be an alphanumeric or
      *>   national literal or a data item of class alphanumeric or
      *>   national."
      *> W-Z PIC S9(3) USAGE NATIONAL is class numeric (PB5625 B),
      *>   neither class AR1 names, so the reference is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TNF04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-Z   PIC S9(3) USAGE NATIONAL VALUE 123.
       01  R     PIC 9(3).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R = FUNCTION TEST-NUMVAL-F(W-Z)
           STOP RUN.
