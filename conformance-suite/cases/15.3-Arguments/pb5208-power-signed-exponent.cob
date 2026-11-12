      *>--- conformance-case
      *> id: pb5208-power-signed-exponent
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
      *> says the same at COBOL-85. M is signed, so with N = 3 and
      *> M = -1 N ** M is 1/3, and the program is refused
      *> (COBOL1627) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5208NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 3.
       01  M PIC S9 VALUE -1.
       01  R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (N ** M, 7).
           STOP RUN.
