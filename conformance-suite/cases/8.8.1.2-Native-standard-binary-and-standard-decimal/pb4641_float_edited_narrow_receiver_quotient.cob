      *>--- conformance-case
      *> id: pb4641_float_edited_narrow_receiver_quotient
      *> rules: [DOC-A.1-123, GR-8.8.1.2-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4641 review R2-1: a floating-point edited receiver
      *> whose significand holds 29 digits or fewer (PICTURE, §2023:13.18.40.3 SR15;
      *> at 2002 the exponent counts too, §2002:13.16.38.2 SR6)
      *> still takes the final-transfer quotient rounded ONCE. The
      *> exact quotient of 9.99...9E-1 (33 nines) by 2.0E0 is
      *> .49999999999999999999999999999999995 (35 digits, a tie at the
      *> 35th); truncated to 29 digits it is .4999... (29 nines); ROUNDED
      *> it is .5 (the 30th digit is 9). A double rounding through 34
      *> digits would store .5000... in the truncating receiver.
      *> EXPECTED, DERIVED (Python Decimal): see above.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4641FLOATEDNARROW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F1  PIC +.9(29)E+9.
       01 F2  PIC +.9(29)E+9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE F1 = 9.999999999999999999999999999999999E-1 / 2.0E0
           DISPLAY "F1 " F1
           COMPUTE F2 ROUNDED =
               9.999999999999999999999999999999999E-1 / 2.0E0
           DISPLAY "F2 " F2
           STOP RUN.
