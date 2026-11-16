      *>--- conformance-case
      *> id: g2-ord-min-national-numeric-with-national
      *> rules: [2002-AR-15.59.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.59.2 AR2 - "All arguments shall be of the same class
      *>   with the exception that mixing of arguments of alphabetic
      *>   and alphanumeric classes is allowed."
      *> W-NN (PIC S9(3) USAGE NATIONAL) is class numeric in 2002
      *> (kb/Work PB5625 reading B) and W-N (PIC N) class national;
      *> the classes differ and are not the alphabetic/alphanumeric
      *> pair, so the reference must be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OMNNL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-S   PIC S9(3) VALUE -1.
       01  W-S2  PIC S9(3) VALUE -10.
       01  W-SC  PIC 9V99 VALUE 1.25.
       01  W-PK  PIC S9V999 USAGE PACKED-DECIMAL VALUE 1.299.
       01  W-D   PIC S9(3) VALUE 7.
       01  W-NN  PIC S9(3) USAGE NATIONAL VALUE -5.
       01  W-NU  PIC 9(3) USAGE NATIONAL VALUE 4.
       01  W-A   PIC A(4) VALUE "ABCD".
       01  W-X   PIC X(4) VALUE "ABCE".
       01  W-X2  PIC X(4) VALUE "AB".
       01  W-N   PIC N VALUE N"B".
       01  W-N2  PIC N VALUE N"A".
       01  I     PIC 9(3).
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE FUNCTION ORD-MIN(W-NN W-N) TO I
           STOP RUN.
