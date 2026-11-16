      *>--- conformance-case
      *> id: pb5832-mean-term
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5832 (review R1-1) - FUNCTION MEAN over an item
      *> and a literal as a term of an integer argument.
      *>
      *> §2023:15.3 AR6: "An arithmetic expression that will
      *> always result in an integer value or an integer data
      *> item shall be specified"; §1989:A.2.2 AR4 says the same
      *> at COBOL-85. FUNCTION MEAN (A, 0) is (A + 0) / 2
      *> (§2023:15.60.4 RV1), 0.5 at A = 1, so MEAN (A, 0) + 1 is
      *> no integer there. Refused (COBOL1627) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5832M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A  PIC 9 VALUE 1.
       01  R  PIC 9(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD (FUNCTION MEAN (A, 0) + 1, 3).
           STOP RUN.
