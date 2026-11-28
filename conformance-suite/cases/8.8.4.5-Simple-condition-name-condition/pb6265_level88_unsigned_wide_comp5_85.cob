      *>--- conformance-case
      *> id: pb6265_level88_unsigned_wide_comp5_85
      *> rules: [GR-8.8.4.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6265 negative: a condition-name over a 25-digit
      *> item is a 2002 program. COBOL-85 limits a fixed-point item
      *> and a numeric literal to 18 digits (COBOL0802), so the item
      *> and the VALUE the golden
      *> pb6265_level88_unsigned_wide_comp5 tests are refused at
      *> --std 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6265NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U    PIC 9(25) VALUE 1.
          88 U-BIG VALUE 1000000000000000000000.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF U-BIG
               DISPLAY "BIG"
           END-IF
           STOP RUN.
