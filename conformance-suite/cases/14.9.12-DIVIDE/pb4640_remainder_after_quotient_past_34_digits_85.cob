      *>--- conformance-case
      *> id: pb4640_remainder_after_quotient_past_34_digits_85
      *> rules: [GR-14.9.12.4-6, GR-14.9.12.4-7]
      *> source-format: fixed
      *>---
      *> kb/Work PB4640: the program of
      *> pb4640_remainder_after_quotient_past_34_digits needs the
      *> intrinsic function ABS, which COBOL-2002 introduced (ISO
      *> 15.4.1); below it the program is refused at the function.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4640REMQUOT85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9(18) VALUE 999999999999999.
       01 Q    PIC S9(18).
       01 R    PIC S9(18).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DIVIDE FUNCTION ABS (A * A * A + 4321) BY 1000
               GIVING Q REMAINDER R
           STOP RUN.
