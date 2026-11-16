      *>--- conformance-case
      *> id: g2rnd03-random-alphanumeric-seed
      *> rules: [2002-AR-15.62.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.62.2 AR1 - "Argument-1 shall be of class numeric."
      *> A is PIC X(3), class alphanumeric (§2002:8.5.2 Table 3), so
      *> the reference must be refused at compile time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RND03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A       PIC X(3) VALUE "100".
       01  R       PIC 9V9(9).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R = FUNCTION RANDOM(A)
           STOP RUN.
