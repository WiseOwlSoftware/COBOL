      *>--- conformance-case
      *> id: pb5707-currency-picture-symbol-national-literal-8
      *> rules: [SR-12.3.7.3-26]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5707. §2014:12.3.6.2 SR26: "Literal-8 shall be an alphanumeric or national literal consisting of a
      *> single character" (§2023:12.3.7.3 SR26 the same; cite.py --check: OK), so N"U" is a legal PICTURE SYMBOL.
      *> COBOL-2002 asks an alphanumeric one (§2002:12.2.6.2 SR22: "Literal-8 shall be an alphanumeric literal
      *> consisting of a single character"; cite.py --check: OK) and refuses it (the diagnostics file of this id).
      *> Expected, derived from the picture: PIC UU99.99 floats U; its first U is the symbol only, so the integer
      *> digits are the second U and the two 9s: 12.5 is 012.50, the zero is suppressed, and the currency string USD
      *> stands immediately left of the first digit shown, in the symbol's place: " USD12.50".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5707N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "USD" WITH PICTURE SYMBOL N"U".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E PIC UU99.99.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 12.5 TO E
           DISPLAY "[" E "]"
           STOP RUN.
