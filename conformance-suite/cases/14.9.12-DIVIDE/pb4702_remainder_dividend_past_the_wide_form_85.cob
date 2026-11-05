      *>--- conformance-case
      *> id: pb4702_remainder_dividend_past_the_wide_form_85
      *> rules: [GR-14.9.12.4-6]
      *> source-format: fixed
      *>---
      *> kb/Work PB4702: the remainder of
      *> pb4702_remainder_dividend_past_the_wide_form is witnessed with
      *> FUNCTION ABS and items of 19 digits and more, which COBOL-2002
      *> introduced, so under --std 85 the program is refused at the
      *> function and the lane is never reached there.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4702REM85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(18) VALUE 999999999999999999.
       01 Q    PIC 9(3)V9(8).
       01 R2   PIC 9(3)V9(8).
       PROCEDURE DIVISION.
           DIVIDE FUNCTION ABS (A + 7) BY FUNCTION ABS (A)
               GIVING Q REMAINDER R2.
           STOP RUN.
       END PROGRAM PB4702REM85.
