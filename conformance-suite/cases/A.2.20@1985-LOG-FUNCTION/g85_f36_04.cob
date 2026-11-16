      *>--- conformance-case
      *> id: g85_f36_04
      *> rules: [1989-FMT-A.2.20.2, 1989-AR-A.2.20.3-1, 1989-RV-A.2.20.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.20.2 - the format FUNCTION LOG (argument-1).
      *> §1989:A.2.20.3 AR1 - argument-1 of class numeric: a literal,
      *> a DISPLAY item with a decimal point, a BINARY item, a
      *> PACKED-DECIMAL item and an arithmetic expression
      *> (§1989:A.2.2 AR1) are each accepted.
      *> §1989:A.2.20.4 RV1 - the natural log, rounded to four places:
      *> ln 1 = 0, ln 100 = 4.60517, ln 2.5 = 0.91629,
      *> ln 1000 = 6.90776, ln 0.5 = -0.69315, ln 10 = 2.30259.
      *> A leg fails if a numeric argument is refused or a value
      *> is not the natural logarithm.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3604.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A  PIC 9V9 VALUE 2.5.
       01  B  PIC 9(4) USAGE BINARY VALUE 1000.
       01  P  PIC 9V9 USAGE PACKED-DECIMAL VALUE 0.5.
       01  R  PIC -9.9999.
       PROCEDURE DIVISION.
       P1.
           COMPUTE R ROUNDED = FUNCTION LOG (1).
           DISPLAY R.
           COMPUTE R ROUNDED = FUNCTION LOG (100).
           DISPLAY R.
           COMPUTE R ROUNDED = FUNCTION LOG (A).
           DISPLAY R.
           COMPUTE R ROUNDED = FUNCTION LOG (B).
           DISPLAY R.
           COMPUTE R ROUNDED = FUNCTION LOG (P).
           DISPLAY R.
           COMPUTE R ROUNDED = FUNCTION LOG (A * 4).
           DISPLAY R.
           STOP RUN.
