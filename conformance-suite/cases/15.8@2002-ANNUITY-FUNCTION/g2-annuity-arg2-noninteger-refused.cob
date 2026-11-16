      *>--- conformance-case
      *> id: g2-annuity-arg2-noninteger-refused
      *> rules: [2002-AR-15.8.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.8.2 AR3 - "Argument-2 shall be a positive integer."
      *> W-NV is PIC 9(3)V99, not an integer data item (§2002:15.2 AR5
      *> "an integer data item shall be specified"), so the source is
      *> refused at the COMPUTE. Beside the accepted case
      *> g2-annuity-argument-classes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ANN03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-NV    PIC 9(3)V99 VALUE 5.50.
       01  R       PIC +9.9(4).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R ROUNDED = FUNCTION ANNUITY(0.05 W-NV)
           DISPLAY "R=" R
           STOP RUN.
