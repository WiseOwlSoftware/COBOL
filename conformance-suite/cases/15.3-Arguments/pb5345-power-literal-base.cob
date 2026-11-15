      *>--- conformance-case
      *> id: pb5345-power-literal-base
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5345 - an integer argument written as a power
      *> whose base is an integer literal of 2 or more, or the same
      *> item as its exponent, or which sits under a negation.
      *>
      *> §2023:15.3 type 6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value or
      *> an integer data item shall be specified"; §1989:A.2.2 AR4
      *> says the same at COBOL-85. M is signed: 2 ** M is 1/2 at
      *> M = -1, M ** M is 1/4 at M = -2, and - (N ** -1) is
      *> -1/3 at N = 3. Each is refused (COBOL1627) at every
      *> edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5345NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 3.
       01  M PIC S9 VALUE -1.
       01  R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (2 ** M, 7).
           COMPUTE R = FUNCTION MOD (M ** M, 7).
           COMPUTE R = FUNCTION MOD (- (N ** -1), 7).
           STOP RUN.
