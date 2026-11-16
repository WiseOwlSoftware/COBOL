      *>--- conformance-case
      *> id: pb5832-product-of-items
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5832 (review R1-1) - a product of two items
      *> under a literal factor.
      *>
      *> §2023:15.3 AR6: "An arithmetic expression that will
      *> always result in an integer value or an integer data
      *> item shall be specified"; §1989:A.2.2 AR4 says the same
      *> at COBOL-85, where no run-time check catches the value.
      *> With A = 1 and B = 1, A * B * 0.5 is 0.5, so the argument
      *> is no integer. Refused (COBOL1627) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5832P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A  PIC 9 VALUE 1.
       01  B  PIC 9 VALUE 3.
       01  R  PIC 9(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION FACTORIAL (A * B * 0.5).
           STOP RUN.
