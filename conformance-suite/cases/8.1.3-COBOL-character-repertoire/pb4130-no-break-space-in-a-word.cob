*>--- conformance-case
*> id: pb4130-no-break-space-in-a-word
*> rules: [GR-8.1.3.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.2.1 / Annex B (kb/Work PB4130): U+00A0 NO-BREAK SPACE is no character of any edition's Annex B, so
      *> it cannot stand in a COBOL word at ANY target. At COBOL-85 the refusal is the word-character rule
      *> (COBOL2773), not the extended-letter gate (COBOL0900): the character is not an extended letter, so
      *> "COBOL-2002 introduced extended letters" would blame the target for the character.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4130N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PREIS  PIC 9(3) VALUE 5.
       PROCEDURE DIVISION.
           DISPLAY PREIS .
           STOP RUN.
