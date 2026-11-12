      *>--- conformance-case
      *> id: pb5196-record-contains-equal-bounds-2002
      *> rules: [SR-13.18.43.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5196 - §2002:13.16.41.2 SR8 (cite.py --check OK): "Integer-5
      *> shall be greater than integer-4." (§2023:13.18.43.3 SR9 is the same
      *> words.) The clause states 5 TO 5, so integer-5 is not greater: refused
      *> from 2002. At COBOL-85 the same clause is legal (§1985:VII.3.8 prints
      *> no such rule; golden pb5196_record_contains_equal_bounds).
      *> Fails if: the program compiles at 2002, 2014 or 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5196EQR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5196eqr.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           RECORD CONTAINS 5 TO 5 CHARACTERS.
       01  R1 PIC X(5).
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
