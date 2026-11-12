      *>--- conformance-case
      *> id: pb5122-numeric-alphabetic-item-at-85
      *> rules: [SR-8.8.4.4.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5122 - the edition half of pb5122_numeric_test_operands_2002. §1985:VI.6.3.1.2: "The NUMERIC test cannot be used with an item
      *> whose data description describes the item as alphabetic".
      *> Expected: COBOL6917 at 85; from 2002 the program compiles (§2023:8.8.4.4.3 SR8).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5122NA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  AL PIC AAA VALUE "ABC".
       PROCEDURE DIVISION.
       P1.
           IF AL IS NUMERIC DISPLAY "T" ELSE DISPLAY "F".
           STOP RUN.
