      *>--- conformance-case
      *> id: pb5832-quotient-in-sum
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5832 - an integer argument written as a quotient
      *> by a literal inside a sum, which does not always result in
      *> an integer value.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified"; §1989:A.2.2
      *> AR4 says the same at COBOL-85, where no run-time check
      *> catches the value. With WN = 1, WN / 2 + 1 is 1.5, so the
      *> program is refused (COBOL1627) at every edition, as
      *> FUNCTION FACTORIAL (WN / 2) is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5832Q.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WN PIC 9 VALUE 3.
       01  R  PIC 9(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION FACTORIAL (WN / 2 + 1).
           STOP RUN.
