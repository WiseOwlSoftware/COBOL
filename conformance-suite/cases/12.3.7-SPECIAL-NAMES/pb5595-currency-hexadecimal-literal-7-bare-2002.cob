      *>--- conformance-case
      *> id: pb5595-currency-hexadecimal-literal-7-bare-2002
      *> rules: [SR-12.3.7.3-19]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5595 - the CURRENCY SIGN clause without PICTURE SYMBOL, literal-7 written in hexadecimal format. The
      *> rule that such a literal-7 needs the PICTURE SYMBOL phrase first appears in the 2014 text (§2014:12.3.6.2 SR19:
      *> "If literal-7 is an alphanumeric or national literal in hexadecimal format, the PICTURE SYMBOL phrase shall be
      *> present in the CURRENCY SIGN clause"; cite.py --check "§2014:12.3.6.2 SR19" with that quotation: OK). The
      *> 2002 text has no such rule: §2002:12.2.6.2 SR19 asks only that without PICTURE SYMBOL "literal-7 shall consist
      *> of a single character" outside the excluded set, and X"40" is the one character '@' (an ASCII compile-time set).
      *> The refusal at 2014 and 2023 is pb791-currency-hexadecimal-literal-7-without-picture-symbol.
      *> Expected, derived from the picture: PIC @@@9.99 is a floating insertion string of three symbols (the first is
      *> the sign position); the digit positions are the 2nd and 3rd '@' and the 9, so 12.5 is 012.50 and the leading
      *> zero is suppressed with '@' placed immediately left of the first nonzero digit: one space, '@', 12.50.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5595P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS X"40".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC @@@9.99.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 12.5 TO E1
           DISPLAY "[" E1 "]"
           STOP RUN.
