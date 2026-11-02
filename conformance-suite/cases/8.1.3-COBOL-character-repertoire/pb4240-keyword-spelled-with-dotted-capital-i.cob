      *>--- conformance-case
      *> id: pb4240-keyword-spelled-with-dotted-capital-i
      *> rules: [GR-8.1.3.2-3, GR-8.1.3.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4240 - ISO 8.1.3.2 3) a): "COBOL basic letters
      *> appearing elsewhere within the compilation group are treated in
      *> a case-insensitive manner", and 4) opens "Extended letters in
      *> user-defined words are subject to the following rules": the
      *> Annex C fold of U+0130 (capital I with dot above) to i governs
      *> user-defined words only (owner decision 2026-10-31). So DİSPLAY
      *> is not the DISPLAY verb, and a user word in a statement's place
      *> is no statement: COBOL0001 at every edition with extended
      *> letters. It fails when a keyword is matched by the fold, which
      *> makes the line a DISPLAY statement that compiles and runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4240V.
       PROCEDURE DIVISION.
           DİSPLAY "HI".
           STOP RUN.
