      *>--- conformance-case
      *> id: pb5122-numeric-comp-item-at-85
      *> rules: [SR-8.8.4.4.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5122 - the edition half of pb5122_numeric_test_operands_2002. §1985:VI.6.3.1.2: "Identifier-1 must reference a data item whose
      *> usage is explicitly or implicitly DISPLAY".
      *> Expected: COBOL6918 at 85; from 2002 the program compiles (§2023:8.8.4.4.3 SR8).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5122NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CP PIC 999 COMP VALUE 123.
       PROCEDURE DIVISION.
       P1.
           IF CP IS NUMERIC DISPLAY "T" ELSE DISPLAY "F".
           STOP RUN.
