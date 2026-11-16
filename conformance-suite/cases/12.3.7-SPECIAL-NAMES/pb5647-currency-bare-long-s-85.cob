      *>--- conformance-case
      *> id: pb5647-currency-bare-long-s-85
      *> rules: [SR-12.3.7.3-22]
      *> accept: [85, 2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5647 - the bare CURRENCY SIGN form with LATIN SMALL LETTER LONG S (U+017F). §1985:VI.4.5.4 GR11
      *> excludes the uppercase letters A, B, C, D, P, R, S, V, X, Z and the lowercase letters a through z (cite.py
      *> --check "§1985:VI.4.5.4 GR11" with that quotation); §2002:12.2.6.2 SR19 excludes the letters "or their lowercase
      *> equivalents". U+017F is neither a basic letter nor one of a through z, so it is a legal symbol in both editions,
      *> and in a PICTURE it is the symbol, not the sign symbol S. From 2014 it is refused
      *> (l1c06-currency-long-s-prohibited).
      *> Expected, derived from the picture: PIC ſſ9.99 is a floating insertion string, the currency string is the
      *> symbol itself, so 12.5 shows as ſ12.50.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5647B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "ſ".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC ſſ9.99.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 12.5 TO WS-X
           DISPLAY "X=[" WS-X "]"
           STOP RUN.
