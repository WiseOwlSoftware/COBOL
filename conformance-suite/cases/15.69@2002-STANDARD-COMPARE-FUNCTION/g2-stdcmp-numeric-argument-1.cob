      *>--- conformance-case
      *> id: g2-stdcmp-numeric-argument-1
      *> rules: [2002-AR-15.69.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.69.2 AR1 - "Argument-1 shall be of class alphabetic,
      *>   alphanumeric, or national." W-D PIC 9(3) is class numeric.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SCM02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-A   PIC A(3) VALUE "ABC".
       01  W-X   PIC X(3) VALUE "ABD".
       01  W-N   PIC N(3) VALUE N"ABC".
       01  W-E   PIC 9.99 VALUE "1.25".
       01  W-EN  PIC 9.99 USAGE NATIONAL VALUE N"1.25".
       01  W-D   PIC 9(3) VALUE 123.
       01  W-NN  PIC S9(3) USAGE NATIONAL VALUE 123.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY FUNCTION STANDARD-COMPARE(W-D "123")
           STOP RUN.
