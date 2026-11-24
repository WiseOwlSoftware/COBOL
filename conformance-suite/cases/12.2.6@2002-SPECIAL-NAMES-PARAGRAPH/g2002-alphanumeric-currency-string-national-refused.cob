      *>--- conformance-case
      *> id: g2002-alphanumeric-currency-string-national-refused
      *> rules: [2002-SR-12.2.6.2-24]
      *> source-format: fixed
      *>---
      *> §2002:12.2.6.2 SR24 - "If literal-7 is of class alphanumeric,
      *>   the associated currency symbol may be used only to define a
      *>   numeric-edited item with usage display."  The CURRENCY SIGN
      *>   clause writes the alphanumeric literal "USD", so the symbol
      *>   U may not define the national item NC2; a COBOL-2002
      *>   compiler must refuse it, and accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DCS02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "USD" WITH PICTURE SYMBOL "U".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NC2 PIC U9.99 USAGE NATIONAL.
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED"
           STOP RUN.
