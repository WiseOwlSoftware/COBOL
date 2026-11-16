      *>--- conformance-case
      *> id: g2spn02-locale-literal-numeric
      *> rules: [2002-SR-12.2.6.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.6.2 SR10 - a numeric literal-4 is refused.
      *>   cite.py --check "§2002:12.2.6.2 SR10" "Literal-4 and literal-9
      *>   shall be alphanumeric or national literals" -> OK
      *> LOCALE L1 IS 123 writes literal-4 as a numeric literal, a class
      *> the rule excludes; everything else is legal (the accepted twin
      *> is g2spn01_locale_order_literal_classes), so the only defect is
      *> the literal class. Expected: a diagnostic at the LOCALE clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SPN05.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE L1 IS 123.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "X".
           STOP RUN.
