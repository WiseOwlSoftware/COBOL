      *>--- conformance-case
      *> id: pb5613-floating-literal-wide-limits-2002
      *> rules: [2002-SR-14.8.25.2-3, SR-8.3.3.3.3-2, SR-8.3.3.3.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5613, PB6039 - §2002:8.3.1.2.2.2 (cite.py --check: OK) rule 2: "The significand shall be 1 through 31
      *> digits in length" and rule 3: the exponent "shall have a maximum of three digits and no decimal point". The
      *> literal below has a 36-digit significand and a four-digit exponent, so it is no COBOL-2002 floating-point
      *> literal and is refused (COBOL1661) at 2002; from 2014 the limits are 36 and four
      *> (pb5613-floating-literal-wide-limits-2014).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5613N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4)V999.
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE WS-N = 1.23456789012345678901234567890123456E+0003
           DISPLAY WS-N
           STOP RUN.
