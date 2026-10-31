*>--- conformance-case
*> id: pb3845-removed-character-unparseable-2023
*> rules: [GR-8.1.3.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3845 - Annex B.3 item 3 / Annex E.2 item 4: from COBOL 2023 U+30FB KATAKANA MIDDLE DOT is permitted only
      *> inside a word, so it may no longer end one. The statement below does not parse; the removed-construct gate is
      *> asked of the word the lexer read, so it is named all the same.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3845D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ABC・ PIC X VALUE "M".
       PROCEDURE DIVISION.
           DISPLAY ABC・ MOVE MOVE.
           STOP RUN.
