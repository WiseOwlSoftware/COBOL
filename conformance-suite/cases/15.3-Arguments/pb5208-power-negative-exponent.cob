      *>--- conformance-case
      *> id: pb5208-power-negative-exponent
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5208 - an integer argument written as a power that
      *> does not always result in an integer value.
      *>
      *> §2023:15.3 type 6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value or
      *> an integer data item shall be specified"; §1989:A.2.2 AR4
      *> says the same at COBOL-85. With N = 3, N ** -1 is 1/3, so
      *> the program is refused (COBOL1627) at every edition, as
      *> FUNCTION MOD (N / 2, 7) is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5208NA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 3.
       01  R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (N ** -1, 7).
           STOP RUN.
