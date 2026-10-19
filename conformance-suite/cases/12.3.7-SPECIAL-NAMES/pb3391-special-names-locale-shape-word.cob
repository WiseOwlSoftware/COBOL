      *>--- conformance-case
      *> id: pb3391-special-names-locale-shape-word
      *> rules: [FMT-12.3.7.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3391 - the CLOSED ISO 12.3.7.2 general format of the SPECIAL-NAMES paragraph
      *> prints no clause FOO BAR IS "x": no alternative of the list takes a word, a word, IS and a
      *> literal but the LOCALE clause, whose first word is LOCALE. That clause is recognized by the
      *> TEXT of its word: the word run FOO BAR cannot continue at the reserved word IS, the LOCALE
      *> clause was the one reading left, and its word test refused FOO, so the parse used to stop at
      *> a bare COBOL0001 "cannot parse construct near 'FOO'" plus a false "a data-name is expected
      *> here" at the literal. FOO is now refused BY NAME in the SPECIAL-NAMES paragraph's format
      *> (COBOLNET1970), the one diagnostic, at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3391SN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           FOO BAR IS "x".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3391SN".
           STOP RUN.
