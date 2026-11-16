      *>--- conformance-case
      *> id: g2-sign-alphanumeric-argument
      *> rules: [2002-AR-15.66.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.66.2 AR1 - "Argument-1 shall be of class numeric."
      *> A is PIC X(3): class alphanumeric (§2002:8.5.2 Table 3), not
      *> numeric, so the reference must be refused at compile time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FSG01N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A       PIC X(3) VALUE "100".
       01  R       PIC S9(6)V99.
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R = FUNCTION SIGN(A)
           STOP RUN.
