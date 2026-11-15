      *>--- conformance-case
      *> id: pb5647-currency-picture-symbol-long-s-2002
      *> rules: [SR-12.3.7.3-27]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5647 - §2002:12.2.6.2 SR23: "Literal-8 may be any character from the computer's coded character set
      *> except for the following ... b) alphabetic characters A, B, C, D, E, N, P, R, S, V, X, Z, or their lowercase
      *> equivalents" (cite.py --check "§2002:12.2.6.2 SR23" with the quotation: OK). Equivalence of case is, in the 2002
      *> text, between COBOL basic letters (§2002:8.1.2.1 GR3, the Annex D mapping); LATIN SMALL LETTER LONG S (U+017F)
      *> is no basic letter, so it is not the lowercase equivalent of S and is a legal PICTURE SYMBOL. From 2014 the
      *> implementor specifies the prohibited additional characters (§2014:8.1.1) and U+017F is refused
      *> (l1c06-currency-long-s-prohibited, DOC-A.1-44).
      *> Expected, derived from the picture: PIC ſſ9.99 holds the symbol twice (floating insertion); the digit positions
      *> are the second symbol and the 9, so 12.5 is 12.50 and the currency STRING "EUR" is placed immediately left of
      *> the first nonzero digit: EUR12.50.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5647P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL "ſ".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC ſſ9.99.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 12.5 TO WS-X
           DISPLAY "X=[" WS-X "]"
           STOP RUN.
