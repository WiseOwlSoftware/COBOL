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
      *> hyphen and underscore". Word-1 of the first COPY is X, U+20AC
      *> EURO SIGN, which no Annex B holds, so it is no COBOL word and
      *> the program is refused (COBOL2773). The second COPY writes the
      *> same word as the data-name of identifier-1, whose words are
      *> COBOL words too. 2014 keeps both rules (§2014:7.2.2.2 SR10,
      *> §2014:8.3.1.1), so it refuses the program as well, although a
      *> 2014 text-word takes any character of the coded set: written as
      *> pseudo-text the operand is accepted there
      *> (pb5608-copy-pseudo-text-coded-set-character-accepted).
      *> The library text PB5608V holds one line, DISPLAY X<U+20AC>.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5608V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          02 Y PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-P.
           COPY PB5608V REPLACING X€ BY "Z".
           COPY PB5608V REPLACING X€ OF G BY Y.
           STOP RUN.
