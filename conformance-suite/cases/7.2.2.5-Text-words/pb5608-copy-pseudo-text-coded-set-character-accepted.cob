      *>--- conformance-case
      *> id: pb5608-copy-pseudo-text-coded-set-character-accepted
      *> rules: [GR-7.2.2.5-3]
      *> accept: [2014, 2023]
      *> files: [PB5608U.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2023:7.2.2.5 3) (cite.py --check: OK; §2014:7.2.1.4 3) alike):
      *> a text-word is "any other character or sequence of contiguous
      *> characters from the compile-time coded character set, bounded
      *> by separators". Pseudo-text-1 below is X, U+20AC EURO SIGN:
      *> one text-word from 2014, matching the library text's X<U+20AC>,
      *> which is replaced by "Z" before any word rule could see it.
      *> EDITIONS. Before 2014 a text-word is COBOL characters
      *> (§1985:III.2, §2002:7.1.1.4 3)), so 85 and 2002 refuse the
      *> operand (COBOL2572, kb/Work PB5608). Written as word-1 the same
      *> characters are refused at every edition, 2014 included: word-1
      *> is a COBOL word (pb5608-copy-replacing-word-non-cobol-character-
      *> refused, pb5608-copy-replacing-word-non-cobol-character-
      *> refused-85).
      *> EXPECTED OUTPUT, DERIVED: Z (the library's DISPLAY X<U+20AC>
      *> with the word replaced); a refusal, or no output, fails.
      *> CAN FAIL: if the operand screen refused the 2014 text-word, or
      *> if ==X<U+20AC>== failed to match, the library word X<U+20AC>
      *> would reach the lexer, which refuses it as no COBOL word
      *> (COBOL2773) at 2014 and 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5608U.
       PROCEDURE DIVISION.
       MAIN-P.
           COPY PB5608U REPLACING ==X€== BY =="Z"==.
           STOP RUN.
