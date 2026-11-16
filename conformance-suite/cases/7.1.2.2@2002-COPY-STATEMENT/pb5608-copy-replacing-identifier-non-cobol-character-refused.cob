      *>--- conformance-case
      *> id: pb5608-copy-replacing-identifier-non-cobol-character-refused
      *> rules: [2002-SR-7.1.2.2-10]
      *> files: [PB5608I.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Identifier-1 below is X<U+20AC> OF G. Its data-name is a COBOL
      *> word, whose characters §2002:8.3.1.1 (cite.py --check: OK)
      *> limits to "basic letters, basic digits, extended letters, and
      *> the basic special characters hyphen and underscore"; U+20AC
      *> EURO SIGN is in no Annex B, so the operand is refused
      *> (COBOL2773) at 2002 and at 2014 (§2014:8.3.1.1).
      *> CAN FAIL: identifier-1 MATCHES the library's X<U+20AC> OF G,
      *> which is replaced by Y, so no library word reaches the lexer
      *> and the operand screen is the only source of COBOL2773
      *> (without it the program compiles and prints Y).
      *> The library text PB5608I holds DISPLAY X<U+20AC> OF G.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5608I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          02 Y PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-P.
           COPY PB5608I REPLACING X€ OF G BY Y.
           STOP RUN.
