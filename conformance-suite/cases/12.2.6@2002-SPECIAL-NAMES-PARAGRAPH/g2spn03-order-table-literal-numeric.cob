      *>--- conformance-case
      *> id: g2spn03-order-table-literal-numeric
      *> rules: [2002-SR-12.2.6.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.6.2 SR10 - a numeric literal-9 is refused.
      *>   cite.py --check "§2002:12.2.6.2 SR10" "Literal-4 and literal-9
      *>   shall be alphanumeric or national literals" -> OK
      *> ORDER TABLE OT1 IS 5 writes literal-9 as a numeric literal, a
      *> class the rule excludes; the same clause with an alphanumeric
      *> literal is accepted (g2spn01_locale_order_literal_classes), so
      *> the only defect is the literal class. Expected: a diagnostic at
      *> the ORDER TABLE clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SPN06.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ORDER TABLE OT1 IS 5.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "X".
           STOP RUN.
