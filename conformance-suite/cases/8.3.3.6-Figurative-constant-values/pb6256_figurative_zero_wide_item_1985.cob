      *>--- conformance-case
      *> id: pb6256_figurative_zero_wide_item_1985
      *> rules: [GR-8.3.3.6.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6256 negative: the wide-item relation against
      *> ZERO is a 2002 program. COBOL-85 limits a fixed-point item
      *> to 18 places (COBOL0802), so the 30-place item the golden
      *> pb6256_figurative_zero_numeric_relation compares to ZERO
      *> is refused at --std 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6256ZNEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(30) VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF A * A = ZERO
               DISPLAY "ZERO"
           END-IF
           STOP RUN.
