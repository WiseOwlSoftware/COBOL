      *>--- conformance-case
      *> id: pb4278-trailing-hyphen-in-a-word
      *> rules: [GR-8.1.3.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4278 - ISO 8.3.2.1: "The hyphen or underscore
      *> shall not appear as the first or last character in such
      *> words." ISO 8.3.1: "A character-string is delimited by
      *> separators", and the hyphen is no separator, so NAME- is one
      *> COBOL word that breaks the rule. The refusal names the rule
      *> and the word at every edition (COBOL2773), where it used to
      *> name only the parse error the minus split caused (COBOL1941).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4278T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NAME- PIC X(5) VALUE "CREME".
       PROCEDURE DIVISION.
           DISPLAY NAME-.
           STOP RUN.
