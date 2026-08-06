      *> reject-at: 2023
      *> The §12.3.7 SPECIAL-NAMES LOCALE clause is A.4.9 ITEM 10 — "SPECIAL-NAMES paragraph: LOCALE
      *> clause and LOCALE phrases in the ALPHABET clause (12.3.7)" — so COBOL.NET's non-support of it is
      *> documented non-support, conformant per §4.2.7 / §A.4.1 ONLY BECAUSE IT IS DIAGNOSED.
      *> It used to be a raw COBOL0308 parse error (fix-queue PB25): with no localeClause in the grammar
      *> the entry fell into implementorSwitchEntry, matched as far as the bare word LOCALE, re-entered as
      *> `FR IS "fr_FR"` and died on the LITERAL with "a data-name is expected here, not a literal" —
      *> pointing at the wrong token, for a clause the standard defines. An unexplained rejection is not
      *> documented non-support.
      *> The companion locale_keyword_a49 pins the FUNCTION side (§15.57 / §15.68 LOCALE phrases); this
      *> pins the DECLARATION side, without which a locale-name could never be introduced at all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB25SNLOC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr_FR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3) VALUE "AbC".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY X
           STOP RUN.
