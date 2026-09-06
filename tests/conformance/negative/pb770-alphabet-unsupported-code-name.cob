*> reject-at: 85 2002 2014 2023
      *> ISO 12.3.7.3 SR15: "The implementor shall specify the names supported for code-name-1 and code-name-2
      *> in the ALPHABET clause, if any." This implementation supports NONE, so ASCII is a source error.
      *>
      *> It used to be reinterpreted as a LITERAL PHRASE SPELLING OUT ITS OWN LETTERS: the alphabet's first
      *> four positions became A, S, C, I, and every downstream reference read that (kb/Work PB770 leg e -
      *> the GnuCOBOL differential's run_misc:5406 printed a character from position 49 of the permuted
      *> alphabet). A bare word is not a literal at all (SR14 b2), so the literal-phrase reading was never
      *> available to fall back on. CONFORMANCE.md carries the SR15 statement of the supported names.
     
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB770ALPHABETUNSUPPO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET A-ASC IS ASCII.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FILLER PIC X.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
