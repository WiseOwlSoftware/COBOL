      *>--- conformance-case
      *> id: pb4750_exact_past_the_wide_form_variance_present_value_85
      *> rules: [RV-15.4.1-1]
      *> source-format: fixed
      *>---
      *> kb/Work PB4750: the exact variance of
      *> pb4750_exact_past_the_wide_form_variance_present_value is
      *> witnessed with FUNCTION ABS, which COBOL-2002
      *> introduced (ISO/IEC 1989:2002 Clause 15, COBOL1502), so under --std 85
      *> the program is refused at ABS and no lane is reached.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4750E85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R   PIC S9(9)V9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION ABS (-1).
           STOP RUN.
       END PROGRAM PB4750E85.
