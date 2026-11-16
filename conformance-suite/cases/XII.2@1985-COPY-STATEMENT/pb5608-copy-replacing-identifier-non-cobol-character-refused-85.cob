      *>--- conformance-case
      *> id: pb5608-copy-replacing-identifier-non-cobol-character-refused-85
      *> rules: [1985-SR-XII.2.3-6]
      *> files: [PB5608J.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Identifier-1 below is X<U+20AC> OF G. Its data-name is a
      *> user-defined word, and §1985:IV.4.2.2.1.1 (cite.py --check: OK)
      *> says "Each character of a user-defined word is selected from
      *> the set of characters" A-Z, 0-9 and the hyphen. U+20AC EURO
      *> SIGN is none of them, so the program is refused at 85
      *> (COBOL2773), as the word-1 form is
      *> (pb5608-copy-replacing-word-non-cobol-character-refused-85).
      *> CAN FAIL: identifier-1 MATCHES the library's X<U+20AC> OF G,
      *> which is replaced by Y, so no library word reaches the lexer
      *> and the operand screen is the only source of COBOL2773
      *> (without it the program compiles and prints Y).
      *> The library text PB5608J holds DISPLAY X<U+20AC> OF G.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5608J85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          02 Y PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-P.
           COPY PB5608J REPLACING X€ OF G BY Y.
           STOP RUN.
