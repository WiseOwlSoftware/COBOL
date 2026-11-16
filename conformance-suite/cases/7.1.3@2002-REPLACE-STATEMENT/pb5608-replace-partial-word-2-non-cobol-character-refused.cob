      *>--- conformance-case
      *> id: pb5608-replace-partial-word-2-non-cobol-character-refused
      *> rules: [2002-SR-7.1.3.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.3.2 SR6 - "Partial-word-2 shall consist of zero or
      *> one text-word"; §2002:7.1.1.4 - a text-word is "any other
      *> sequence of contiguous COBOL characters bounded by separators".
      *> Partial-word-2 holds the character U+00A4 (the currency sign),
      *> which is no character of the 2002 COBOL character repertoire
      *> (§2002:8.1.2), so Y¤Z is no text-word and
      *> partial-word-2 is no text-word the rule allows: the program is
      *> refused at 2002. COBOL-2014 takes a text-word from the
      *> compile-time coded character set (§2014:7.2.1.4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5608B.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-P.
           REPLACE LEADING ==XX== BY ==Y¤Z==.
           DISPLAY W.
           STOP RUN.
