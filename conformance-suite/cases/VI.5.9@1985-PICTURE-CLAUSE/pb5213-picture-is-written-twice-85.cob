      *>--- conformance-case
      *> id: pb5213-picture-is-written-twice-85
      *> rules: [1985-FMT-VI.5.9.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5213 - §1985:VI.5.9.2 prints {PICTURE | PIC} IS
      *> character-string (cite.py --check "§1985:VI.5.9.2"
      *> "PICTURE IS character-string" OK); IS is not underlined, an
      *> optional word written once, so the next word is the string.
      *> §1985:VI.5.9.3 SR2: "A character-string consists of certain
      *> allowable combinations of characters in the COBOL character set
      *> used as symbols" (cite.py --check OK), and I is no PICTURE
      *> symbol, so the character-string IS below is refused at 85. It
      *> fails when the lexer skips every IS before the string.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5213TW85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  B PIC IS IS.
       PROCEDURE DIVISION.
       P1.
           DISPLAY B.
           STOP RUN.
