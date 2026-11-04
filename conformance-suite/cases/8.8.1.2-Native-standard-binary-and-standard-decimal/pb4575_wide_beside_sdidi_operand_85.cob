      *>--- conformance-case
      *> id: pb4575_wide_beside_sdidi_operand_85
      *> rules: [DOC-A.1-123, GR-8.8.1.2-1]
      *> source-format: fixed
      *>---
      *> kb/Work PB4575: the expression lane of
      *> pb4575_wide_beside_sdidi_operand needs an operand the SDIDI
      *> carries beside the wide product, and its witness is a
      *> floating-point literal, which COBOL-2002 introduced (ISO
      *> 8.3.3.3.3; VCR row 7.37). Under --std 85 the program is
      *> refused at the literal, so the lane is never reached there.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4575WIDESDIDI85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC 9(18) VALUE 999999999999999999.
       01 K3  PIC 999 VALUE 999.
       01 R   PIC -(9)9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = (BIG * BIG * K3 + 2.0E0) - BIG * BIG * K3
           DISPLAY R
           STOP RUN.
