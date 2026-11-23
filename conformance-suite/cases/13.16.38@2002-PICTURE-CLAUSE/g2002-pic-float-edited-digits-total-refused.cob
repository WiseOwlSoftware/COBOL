      *>--- conformance-case
      *> id: g2002-pic-float-edited-digits-total-refused
      *> rules: [2002-SR-13.16.38.2-6]
      *> source-format: fixed
      *>---
      *> §2002:13.16.38.2 SR6 - "the number of digit positions described
      *>   by character-string-1 shall range from 1 through 31."  A
      *>   floating-point edited character-string-1 is both parts
      *>   (§2002:13.16.38.3 GR12 b), and the exponent's 9s are digit
      *>   positions (§2002:13.16.38.4 ER8).  +9(29)E+999 describes
      *>   29 + 3 = 32, so a COBOL-2002 compiler must refuse it even
      *>   though its significand alone is within 31.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PFB4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC +9(29)E+999.
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED"
           STOP RUN.
