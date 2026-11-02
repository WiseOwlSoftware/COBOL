      *>--- conformance-case
      *> id: pb4240-reserved-word-spelled-with-dotted-capital-i-as-user-word
      *> rules: [GR-8.1.3.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4240 - the user-defined-word half of the owner's
      *> 2026-10-31 decision: the Annex C fold of 8.1.3.2 4) b) governs
      *> user-defined words, and Annex C.2 maps U+0130 to i, so DİSPLAY
      *> written as a data-name is the user word display, which 8.3.2.1
      *> 1) forbids (a reserved word is no user-defined word): COBOL0901.
      *> E.2 14) confirms a basic-letter-target mapping folds user words.
      *> The sibling goldens hold the keyword half: the same spelling is
      *> no DISPLAY verb (pb4240-keyword-spelled-with-dotted-capital-i).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4240U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DİSPLAY PIC X VALUE "A".
       PROCEDURE DIVISION.
           STOP RUN.
