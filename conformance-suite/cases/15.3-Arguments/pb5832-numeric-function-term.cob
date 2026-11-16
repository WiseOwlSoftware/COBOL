      *>--- conformance-case
      *> id: pb5832-numeric-function-term
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5832 - an integer argument written as a sum with
      *> a numeric function, which does not always result in an
      *> integer value.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified"; §1989:A.2.2
      *> AR4 says the same at COBOL-85, and §1989:A.2.3 GR2 says
      *> "A numeric function may not be referenced where an integer
      *> operand is required". With N = 2, SQRT (N) is irrational,
      *> so SQRT (N) + 64 is no integer and the program is refused
      *> (COBOL1627) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5832F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 9 VALUE 2.
       01  C  PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION CHAR (FUNCTION SQRT (N) + 64) TO C.
           STOP RUN.
