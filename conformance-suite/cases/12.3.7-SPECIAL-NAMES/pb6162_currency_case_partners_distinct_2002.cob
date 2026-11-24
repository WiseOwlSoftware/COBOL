      *>--- conformance-case
      *> id: pb6162_currency_case_partners_distinct_2002
      *> rules: [SR-12.3.7.3-21, SR-13.18.40.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6162. Two CURRENCY SIGN clauses whose PICTURE
      *> SYMBOLs are 'é' and 'É', bound to different strings, at
      *> COBOL 2002. The 2002 rule is "No two occurrences of literal-8
      *> may be the same value" (§2002:12.2.6.2 SR22), and 'é' and 'É'
      *> are two values: 2002 has no counterpart of 2014's grant of an
      *> equivalence for a character outside the COBOL repertoire
      *> (§2014:12.3.6.2 SR20; kb/Work PB6130), and its rule that a
      *> lowercase-letter currency symbol "is treated as its uppercase
      *> equivalent" (§2002:12.2.6.3 GR13) reaches the letters of the
      *> Annex D mapping only, which hold no 'é'. So the program is
      *> legal and each symbol inserts its own string: 1.5 edits under
      *> 9.99 as 1.50, preceded by EUR for 'é' and USD for 'É'. From
      *> 2014 the two symbols are equivalent and the same program is
      *> refused (l1c06-currency-non-cobol-equivalent-rebound).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6162D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL "é"
           CURRENCY SIGN IS "USD" WITH PICTURE SYMBOL "É".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-LO PIC é9.99.
       01 WS-UP PIC É9.99.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1.5 TO WS-LO WS-UP.
           DISPLAY "LO=[" WS-LO "]".
           DISPLAY "UP=[" WS-UP "]".
           STOP RUN.
