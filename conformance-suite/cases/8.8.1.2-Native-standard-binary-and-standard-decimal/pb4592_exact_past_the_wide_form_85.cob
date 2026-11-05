      *>--- conformance-case
      *> id: pb4592_exact_past_the_wide_form_85
      *> rules: [GR-8.8.1.2-6]
      *> source-format: fixed
      *>---
      *> kb/Work PB4592: the exact intermediate of
      *> pb4592_exact_past_the_wide_form is witnessed with
      *> floating-point numeric literals (1.0E0, 1.0E-50), which
      *> COBOL-2002 introduced (ISO 8.3.3.3.3 1)), so under --std 85 the
      *> program is refused at the literal and the lane is never reached.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4592E85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIG PIC 9(18) VALUE 999999999999999999.
       01 R   PIC S9(9).
       PROCEDURE DIVISION.
           COMPUTE R = BIG * BIG * 1.0E0 - BIG * BIG.
           STOP RUN.
       END PROGRAM PB4592E85.
