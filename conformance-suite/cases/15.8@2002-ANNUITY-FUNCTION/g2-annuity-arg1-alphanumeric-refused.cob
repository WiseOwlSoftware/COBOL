      *>--- conformance-case
      *> id: g2-annuity-arg1-alphanumeric-refused
      *> rules: [2002-AR-15.8.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.8.2 AR1 - "Argument-1 shall be of class numeric."
      *> W-AN is class alphanumeric, so it is no numeric argument
      *> (§2002:15.2 AR9 "An arithmetic expression or a numeric data
      *> item shall be specified"); the source is refused at the
      *> COMPUTE. Beside the accepted case g2-annuity-argument-classes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ANN02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-AN    PIC X(4) VALUE "0.10".
       01  R       PIC +9.9(4).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R ROUNDED = FUNCTION ANNUITY(W-AN 3)
           DISPLAY "R=" R
           STOP RUN.
