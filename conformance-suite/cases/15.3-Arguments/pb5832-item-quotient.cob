      *>--- conformance-case
      *> id: pb5832-item-quotient
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5832 - an integer argument written as a quotient
      *> of two items, which does not always result in an integer
      *> value.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified"; §1989:A.2.2
      *> AR4 says the same at COBOL-85, where no run-time check
      *> catches the value. With A = 1 and B = 3, A / B is 1/3, so
      *> the program is refused (COBOL1627) at every edition, as
      *> FUNCTION MOD (N / 2, 7) is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5832I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A  PIC 9 VALUE 7.
       01  B  PIC 9 VALUE 2.
       01  R  PIC 9(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (A / B, 3).
           STOP RUN.
