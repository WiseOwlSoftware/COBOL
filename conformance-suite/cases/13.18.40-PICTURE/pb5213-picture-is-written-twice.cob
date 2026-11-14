      *>--- conformance-case
      *> id: pb5213-picture-is-written-twice
      *> rules: [FMT-13.18.40.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5213 - ISO 13.18.40.2 Format 1 prints PICTURE IS
      *> character-string-1 (cite.py --check "§2023:13.18.40.2"
      *> "IS character-string-1 LOCALE" OK): IS is an optional word the
      *> format prints once, so the word after the one IS is
      *> character-string-1. Here that word is a second IS, written on
      *> the next line, and IS holds no PICTURE symbol (I is none,
      *> 13.18.40.3 SR2), so the clause is refused at every edition,
      *> naming the character-string IS. It fails when the lexer skips
      *> every IS before the string: the second IS then vanished and
      *> the entry's next word became its picture.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5213TWICE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  B PICTURE IS
               IS.
       PROCEDURE DIVISION.
       P1.
           DISPLAY B.
           STOP RUN.
