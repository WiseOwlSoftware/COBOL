      *>--- conformance-case
      *> id: pb5707-currency-picture-symbol-literal-8-repeated
      *> rules: [SR-12.3.7.3-21]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5707. §2023:12.3.7.3 SR21: "No two CURRENCY SIGN clauses within a single source unit shall specify
      *> equivalent currency symbols unless they specify identical currency strings" (§2014:12.3.6.2 SR21 the same;
      *> cite.py --check: OK), so two clauses naming "U" with the identical string "USD" are legal. COBOL-2002 adds
      *> "No two occurrences of literal-8 may be the same value" (§2002:12.2.6.2 SR22; cite.py --check: OK) and
      *> refuses the second clause (the diagnostics file of this id).
      *> Expected, derived from the picture: as pb5707-currency-picture-symbol-national-literal-8, " USD12.50".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5707R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "USD" WITH PICTURE SYMBOL "U"
           CURRENCY SIGN IS "USD" WITH PICTURE SYMBOL "U".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E PIC UU99.99.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 12.5 TO E
           DISPLAY "[" E "]"
           STOP RUN.
