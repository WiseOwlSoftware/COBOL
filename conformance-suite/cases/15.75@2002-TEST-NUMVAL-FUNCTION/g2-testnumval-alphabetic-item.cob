      *>--- conformance-case
      *> id: g2-testnumval-alphabetic-item
      *> rules: [2002-AR-15.75.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.75.2 AR1 - "Argument-1 shall be an alphanumeric or
      *>   national literal or a data item of class alphanumeric or
      *>   national."
      *> W-Z PIC A(4) is class alphabetic,
      *>   neither class AR1 names, so the reference is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TNV02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-Z   PIC A(4) VALUE "ABCD".
       01  R     PIC 9(3).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R = FUNCTION TEST-NUMVAL(W-Z)
           STOP RUN.
