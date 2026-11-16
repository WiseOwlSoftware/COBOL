      *>--- conformance-case
      *> id: pb5608-copy-replacing-word-non-cobol-character-refused
      *> rules: [2002-SR-7.1.2.2-10]
      *> files: [PB5608V.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR10 (cite.py --check: OK): "Word-1 or word-2 may
      *> be any single COBOL word except 'COPY'"; §2002:8.3.1.1 (OK):
      *> "Each character of a COBOL word that is not a special character
      *> word shall be selected from the set of basic letters, basic
      *> digits, extended letters, and the basic special characters
      *> hyphen and underscore". Word-1 below is X, U+20AC EURO SIGN,
      *> which no Annex B holds, so it is no COBOL word and the program
      *> is refused (COBOL2773). 2014 keeps both rules (§2014:7.2.2.2
      *> SR10, §2014:8.3.1.1) and refuses it too, although a 2014
      *> text-word takes any character of the coded set (written as
      *> pseudo-text the operand is accepted there:
      *> pb5608-copy-pseudo-text-coded-set-character-accepted).
      *> CAN FAIL: word-1 MATCHES the library's only X<U+20AC>, which is
      *> replaced by "Z", so no library word reaches the lexer and the
      *> operand screen is the only source of COBOL2773 (without it the
      *> program compiles and prints Z).
      *> The library text PB5608V holds one line, DISPLAY X<U+20AC>.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5608V.
       PROCEDURE DIVISION.
       MAIN-P.
           COPY PB5608V REPLACING X€ BY "Z".
           STOP RUN.
