      *>--- conformance-case
      *> id: pb5613-floating-literal-wide-limits-2014
      *> rules: [SR-8.3.3.3.3-2, SR-8.3.3.3.3-3]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5613, PB6039 - the floating-point literal's digit limits are the edition's. §2014:8.3.1.2.2.2 (cite.py
      *> --check: OK) rule 2: "The significand shall be from 1 to 36 digits in length" and rule 3: the exponent "shall have a
      *> maximum of four digits and no decimal point"; §2023:8.3.3.3.3 SR2 and SR3 say the same, and §2014:E.3 item 20
      *> records the increase "from 31 to 36" and "from 3 to 4". The literal below has a 36-digit significand and a
      *> four-digit exponent: its value is 1.23456789012345678901234567890123456 x 10**3 = 1234.5678..., stored by
      *> truncation in PIC 9(4).999 as 1234.567. At COBOL-2002 the same source is
      *> refused (pb5613-floating-literal-wide-limits-2002).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5613W.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4).999.
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE WS-N = 1.23456789012345678901234567890123456E+0003
           DISPLAY WS-N
           STOP RUN.
