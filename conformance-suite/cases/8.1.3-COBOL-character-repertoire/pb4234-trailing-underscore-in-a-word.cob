*>--- conformance-case
*> id: pb4234-trailing-underscore-in-a-word
*> rules: [GR-8.1.3.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4234 - ISO 8.3.2.1: "The hyphen or underscore shall not appear as the first or last character in
      *> such words." NAME_ is a COBOL word that breaks that rule (the sibling of the leading underscore: the lexer
      *> used to end the word before the underscore, which then failed the parse on its own).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4234T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NAME_ PIC X(5) VALUE "CREME".
       PROCEDURE DIVISION.
           DISPLAY NAME_.
           STOP RUN.
