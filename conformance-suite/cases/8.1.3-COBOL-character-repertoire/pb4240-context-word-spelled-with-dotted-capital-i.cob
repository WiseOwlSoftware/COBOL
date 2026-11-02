      *>--- conformance-case
      *> id: pb4240-context-word-spelled-with-dotted-capital-i
      *> rules: [GR-8.1.3.2-3, GR-8.1.3.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4240, review finding R2-1 - a context-sensitive word
      *> (8.10) is a keyword, matched on the basic letters alone (8.1.3.2
      *> 3) a); the Annex C fold of 4) governs user-defined words only,
      *> owner decision 2026-10-31). CLASSİFİCATİON, written with U+0130,
      *> is no CLASSIFICATION clause of the OBJECT-COMPUTER paragraph.
      *> The lexer leaves the word an IDENTIFIER, so the parser's text
      *> predicate (CobolWordsMap.Is) decides; it fails when that
      *> predicate compares by the fold, which reads the word as the
      *> clause and compiles the program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4240C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X CLASSİFİCATİON IS USER-DEFAULT.
       PROCEDURE DIVISION.
           STOP RUN.
