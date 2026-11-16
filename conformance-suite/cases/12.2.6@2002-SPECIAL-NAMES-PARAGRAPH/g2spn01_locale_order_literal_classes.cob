      *>--- conformance-case
      *> id: g2spn01_locale_order_literal_classes
      *> rules: [2002-SR-12.2.6.2-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.6.2 SR10 - literal-4 (LOCALE) and literal-9 (ORDER
      *> TABLE) of the classes the rule names are accepted and work.
      *>   cite.py --check "§2002:12.2.6.2 SR10" "Literal-4 and literal-9
      *>   shall be alphanumeric or national literals" -> OK
      *> The rule is a restriction (no granting word); its refusal legs
      *> are g2spn02-locale-literal-numeric and
      *> g2spn03-order-table-literal-numeric. This positive case writes
      *> each class the rule ALLOWS: LOCALE LA with an alphanumeric
      *> literal-4, LOCALE LN with a national literal-4, ORDER TABLE OT1
      *> with an alphanumeric literal-9. A compiler that over-applied
      *> SR10 (refusing a national literal-4, say) fails the compile.
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   EQ - §2002:15.69.3 RV6: the returned value is "=" "if the
      *>     arguments compare equal" (cite.py OK); "abc" and "abc" are
      *>     the same string, so they compare equal under whatever
      *>     table and level is used (RV5) => EQ==. This does not show
      *>     that OT1 is in effect: §2002:12.2.6.3 GR17 leaves the
      *>     content of literal-9 to the implementor (cite.py OK), and
      *>     the content written here names a 2020 table, not a
      *>     14651:2001 table.
      *>   LEN - §2002:15.69.3 RV7 "The length of the returned value is
      *>     1" (cite.py OK) => LEN=1
      *> The run lines show only that the program compiled and ran:
      *> "abc" against "abc" is "=" under any table (RV5, RV6), and an
      *> unresolved literal-9 also returns "=" with EC-ORDER-NOT-
      *> SUPPORTED unchecked (implementor choice, DOC-A.1-42). The case
      *> fails only if a literal class that SR10 allows (alphanumeric
      *> or national literal-4, alphanumeric literal-9) is refused at
      *> compile time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SPN01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE LA IS "fr-FR"
           LOCALE LN IS N"fr-FR"
           ORDER TABLE OT1 IS "ISO 14651_2020_TABLE1".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  LEN-R       PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "EQ=" FUNCTION STANDARD-COMPARE("abc" "abc" OT1).
           MOVE FUNCTION LENGTH(FUNCTION STANDARD-COMPARE("a" "b" OT1))
             TO LEN-R.
           DISPLAY "LEN=" LEN-R.
           STOP RUN.
