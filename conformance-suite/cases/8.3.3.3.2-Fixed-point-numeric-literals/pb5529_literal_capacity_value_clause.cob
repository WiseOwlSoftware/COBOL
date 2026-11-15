      *>--- conformance-case
      *> id: pb5529_literal_capacity_value_clause
      *> rules: [GR-8.3.3.3.2-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5529. 8.3.3.3.2: "The implementor shall allow for
      *> fixed-point numeric literals of 1 through 31 digits in length"
      *> (cite.py --check OK). A VALUE-clause literal is a numeric
      *> literal like any other, so 19 digits (beyond COBOL-85's 18) and
      *> 31 digits are legal from COBOL-2002. The twins refused: a
      *> 19-digit literal at COBOL-85 (pb5529-value-of-19-digit-literal,
      *> COBOL0802) and a 32-digit literal at every edition
      *> (pb5529-value-over-31-digits, COBOL0801).
      *> EXPECTED OUTPUT, DERIVED: each item is an unsigned integer
      *> described with as many 9s as its literal has digits, so
      *> DISPLAY writes the literal's digits unchanged: 19, then 31.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5529A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9(19) VALUE 1234567890123456789.
       01  B PIC 9(31) VALUE 1234567890123456789012345678901.
       PROCEDURE DIVISION.
           DISPLAY A
           DISPLAY B
           STOP RUN.
