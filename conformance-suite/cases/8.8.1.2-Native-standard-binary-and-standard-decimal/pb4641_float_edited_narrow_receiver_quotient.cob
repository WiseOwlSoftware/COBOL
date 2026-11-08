      *>--- conformance-case
      *> id: pb4641_float_edited_narrow_receiver_quotient
      *> rules: [DOC-A.1-123, GR-8.8.1.2-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4641 review R2-1: a floating-point edited receiver
      *> whose significand holds 32 digits or fewer (13.18.40.3 SR15)
      *> still takes the final-transfer quotient rounded ONCE. The
      *> exact quotient of 9.99...9E-1 (33 nines) by 2.0E0 is
      *> .49999999999999999999999999999999995 (35 digits, a tie at the
      *> 35th); truncated to 30 digits it is .4999... (30 nines); ROUNDED
      *> it is .5 (the 31st digit is 9). A double rounding through 34
      *> digits would store .5000... in the truncating receiver.
      *> EXPECTED, DERIVED (Python Decimal): see above.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4641FLOATEDNARROW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1  PIC +.9(30)E+99.
       01 F2  PIC +.9(30)E+99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE F1 = 9.999999999999999999999999999999999E-1 / 2.0E0
           DISPLAY "F1 " F1
           COMPUTE F2 ROUNDED =
               9.999999999999999999999999999999999E-1 / 2.0E0
           DISPLAY "F2 " F2
           STOP RUN.
