      *>--- conformance-case
      *> id: pb5595-currency-hexadecimal-literal-8-2002
      *> rules: [SR-12.3.7.3-26]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5595 - the PICTURE SYMBOL literal-8 written in hexadecimal format. §2014:12.3.6.2 SR26: "It shall be
      *> neither a figurative constant nor a hexadecimal literal" (cite.py --check "§2014:12.3.6.2 SR26" with that
      *> quotation: OK) first appears in the 2014 text. §2002:12.2.6.2 SR22: "Literal-8 shall be an alphanumeric literal
      *> consisting of a single character. It shall not be a figurative constant." names no hexadecimal form, so
      *> X"40", the one character '@' (an ASCII compile-time set), is a legal literal-8 at 2002. The refusal at 2014
      *> and 2023 is pb791-currency-hexadecimal-literal-8.
      *> Expected, derived from the picture: PIC @@9.99 is a floating insertion string of two symbols; the digit
      *> positions are the 2nd '@' and the 9, so 12.5 is 12.50, and the currency STRING "EUR" is placed immediately left
      *> of the first nonzero digit: EUR12.50.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5595Q.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN "EUR" WITH PICTURE SYMBOL X"40".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC @@9.99.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 12.5 TO E1
           DISPLAY "[" E1 "]"
           STOP RUN.
