      *>--- conformance-case
      *> id: pb4641_final_transfer_quotient_of_sdidi_operands_85
      *> rules: [DOC-A.1-123, GR-8.8.1.2-1]
      *> source-format: fixed
      *>---
      *> kb/Work PB4641: the lane of
      *> pb4641_final_transfer_quotient_of_sdidi_operands needs
      *> floating-point literals and items past 18 digits, which
      *> COBOL-2002 introduced. Under --std 85 the program is refused
      *> at the item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4641QUOT85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R    PIC 9.
       01 RT   PIC V9(30).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R ROUNDED
               = 9.999999999999999999999999999999999E-1 / 2.0E0
           COMPUTE RT
               = 9.999999999999999999999999999999999E-1 / 2.0E0
           STOP RUN.
