      *>--- conformance-case
      *> id: pb4975_float_edited_wide_significand_quotient_85
      *> rules: [DOC-A.1-123, GR-8.8.1.2-1]
      *> source-format: fixed
      *>---
      *> kb/Work PB4975: the floating-point numeric-edited PICTURE of
      *> pb4975_float_edited_wide_significand_quotient was
      *> introduced by COBOL-2002 (13.18.40.4 GR13 b). Under --std 85
      *> the program is refused at the item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4975WIDEQUOT85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F34  PIC +.9(34)E+99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE F34 = 1 / 3
           STOP RUN.
