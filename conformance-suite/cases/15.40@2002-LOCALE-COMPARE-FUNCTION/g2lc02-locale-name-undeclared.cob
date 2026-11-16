      *>--- conformance-case
      *> id: g2lc02-locale-name-undeclared
      *> rules: [2002-AR-15.40.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.40.2 AR4 - "Locale-name-1 shall be associated with a
      *>   locale in the SPECIAL-NAMES paragraph." NOLOC is not declared by
      *>   any LOCALE clause, so the reference at line 23 is refused. This
      *>   refusal stands beside the accepted case g2lc01-locale-compare,
      *>   which writes declared locale-names; it is not the closing witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LC02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE USA IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  C               PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE FUNCTION LOCALE-COMPARE("a" "b" NOLOC) TO C
           DISPLAY C
           STOP RUN.
