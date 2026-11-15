      *>--- conformance-case
      *> id: pb5595-currency-hexadecimal-string-digits-2002
      *> rules: [SR-12.3.7.3-23]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5595 - the currency string X"3132" (the digits 12) in hexadecimal format. §2002:12.2.6.2 SR20 b) (cite.py
      *> --check "§2002:12.2.6.2 SR20" with the quotation "may consist of any characters from the computer's coded
      *> character set except for the digits 0 through 9": OK) refuses a digit in literal-7 whatever its form; the
      *> hexadecimal exemption is the NOTE of §2014:12.3.6.2 SR19, absent from the 2002 text.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5595G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN X"3132" WITH PICTURE SYMBOL "#".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC #9.99.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 2.5 TO E1
           DISPLAY "[" E1 "]"
           STOP RUN.
