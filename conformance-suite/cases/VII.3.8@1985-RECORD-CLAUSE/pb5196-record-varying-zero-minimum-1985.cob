      *>--- conformance-case
      *> id: pb5196-record-varying-zero-minimum-1985
      *> rules: [1985-FMT-VII.3.8.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5196 - §1985:III.2 (cite.py --check OK): an integer "must be
      *> neither signed nor zero unless explicitly allowed by the rules for that
      *> format". §1985:VII.3.8.3 allows zero for no integer of the RECORD
      *> clause, so RECORD IS VARYING IN SIZE FROM 0 TO 5 CHARACTERS is refused at COBOL-85 (COBOL2386). Zero is
      *> allowed from 2002 (§2002:13.16.41.2 SR6, SR7; golden
      *> pb5196_record_contains_zero_minimum_2002).
      *> Fails if: the program compiles at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5196ZV85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5196zv85.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           RECORD IS VARYING IN SIZE FROM 0 TO 5 CHARACTERS.
       01  R1 PIC X(5).
       WORKING-STORAGE SECTION.
       01  N PIC 9.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
