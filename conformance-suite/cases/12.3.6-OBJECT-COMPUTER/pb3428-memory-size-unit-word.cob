      *>--- conformance-case
      *> id: pb3428-memory-size-unit-word
      *> rules: [FMT-12.3.6.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3428 - the X3.23-1985 OBJECT-COMPUTER paragraph's
      *> MEMORY SIZE clause is MEMORY SIZE integer {WORDS | CHARACTERS
      *> | MODULES}: the braces require one of the three unit words
      *> (ISO 5.2.6.3 reads braces the same way), and only CHARACTERS
      *> is a lexer token, so WORDS and MODULES are recognized by the
      *> TEXT of the word through a semantic predicate. DERIVED, NOT
      *> QUOTED (the repository holds no X3.23-1985 text; VERSION_CHANGE
      *> _REFERENCE row 7.7). FOO is none of the three, and the predicate
      *> refused it where the expected set could not name the words it
      *> tests, so the parse stopped at a bare COBOL0001 "cannot parse
      *> construct near 'FOO'". The parse at FOO, every predicate on the
      *> way evaluated, now names the three (COBOL5506) at every edition:
      *> the grammar parses the deleted clause at 2002+ too, so the
      *> refusal comes before the clause's deleted-2002 gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3428MS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ MEMORY SIZE 12 FOO.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3428MS".
           STOP RUN.
