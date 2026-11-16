      *>--- conformance-case
      *> id: pb5608-copy-replacing-word-non-cobol-character-refused-85
      *> rules: [1985-SR-XII.2.3-6]
      *> files: [PB5608W.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.2.3 SR6 (cite.py --check: OK): "Word-1 or word-2 may
      *> be any single COBOL word except 'COPY'." §1985:IV.4.2.2.1.1
      *> (OK): "Each character of a user-defined word is selected from
      *> the set of characters" A-Z, 0-9 and the hyphen. Word-1 below
      *> is X, U+20AC EURO SIGN: no COBOL word, so the program is
      *> refused at 85 (COBOL2773), as the same characters written as
      *> pseudo-text are (COBOL2572; §1985:III.2: a text-word is "Any
      *> other sequence of contiguous COBOL characters"). Before
      *> kb/Work PB5608's review finding R1-1 only the pseudo-text form
      *> was screened, and this word operand replaced the library's
      *> X<U+20AC> by "Z" and the program ran.
      *> The library text PB5608W holds one line, DISPLAY X<U+20AC>.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5608W85.
       PROCEDURE DIVISION.
       MAIN-P.
           COPY PB5608W REPLACING X€ BY "Z".
           STOP RUN.
