      *>--- conformance-case
      *> id: pb5196_record_contains_zero_minimum_2002
      *> rules: [SR-13.18.43.3-8]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5196 - §2002:13.16.41.2 SR7 (cite.py --check OK): "Integer-4
      *> shall be greater than or equal to zero." (§2023:13.18.43.3 SR8, the same
      *> words.) It is the express override of the nonzero default for an integer
      *> (§2023:5.5 1)), so RECORD CONTAINS 0 TO 5 CHARACTERS is
      *> legal from 2002. At COBOL-85 no rule of §1985:VII.3.8 allows zero and
      *> §1985:III.2 says an integer "must be neither signed nor zero unless
      *> explicitly allowed by the rules for that format", so it is refused
      *> there (pb5196-record-contains-zero-minimum-1985).
      *> A translate-only test: it ends in STOP RUN with no output.
      *> Fails if: the clause is refused at 2002, 2014 or 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5196ZM2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5196zm2.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           RECORD CONTAINS 0 TO 5 CHARACTERS.
       01  R1 PIC X(5).
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
