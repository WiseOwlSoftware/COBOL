      *>--- conformance-case
      *> id: pb5587-sort-merge-assign-numeric-2002
      *> rules: [1985-FMT-XI.2.3.2]
      *> source-format: fixed
      *>---
      *> kb/Work PB5587. From COBOL-2002 literal-1 of the ASSIGN
      *> clause "shall be an alphanumeric literal" (§2002:12.3.4.2
      *> SR4), the sort-merge entry included; a numeric literal is
      *> refused with COBOL2645 at 2002 and later, where 85 sorts
      *> (pb5587-sort-merge-assign-figurative-and-list).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5587NUM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF1 ASSIGN TO 123.
       DATA DIVISION.
       FILE SECTION.
       SD  SF1.
       01  SREC1 PIC X(3).
       PROCEDURE DIVISION.
       M-1.
           STOP RUN.
