      *>--- conformance-case
      *> id: pb4599_final_transfer_quotient_over_exact_operands_85
      *> rules: [DOC-A.1-123, GR-8.8.1.2-1]
      *> source-format: fixed
      *>---
      *> kb/Work PB4599: the lane of
      *> pb4599_final_transfer_quotient_over_exact_operands needs a
      *> resultant past 18 digits for its A ** 3 / A witness, which
      *> COBOL-2002 introduced (ISO 13.18.40.3; the 2002 limit is 31
      *> digits). Under --std 85 the program is refused at the item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4599QUOT85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(18) VALUE 999999999999999.
       01 R31  PIC S9(31).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R31 = A ** 3 / A
           STOP RUN.
