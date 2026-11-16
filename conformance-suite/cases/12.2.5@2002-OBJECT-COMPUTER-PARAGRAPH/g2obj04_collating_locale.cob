      *>--- conformance-case
      *> id: g2obj04_collating_locale
      *> rules: [2002-GR-12.2.5.3-11]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.5.3 GR11, last sentence - "When alphabet-name-1 or
      *> alphabet-name-2, or both, is associated with a locale, locale
      *> category LC_COLLATE is used to carry out these comparisons."
      *>   cite.py --check "§2002:12.2.5.3 GR11" "locale category
      *>   LC_COLLATE is used to carry out these comparisons" -> OK
      *> The program collating sequence is ESA, ALPHABET ESA IS LOCALE
      *> ES, LOCALE ES IS "es-ES": an alphabet associated with a named
      *> locale. The comparison is then locale-based
      *> (§2002:8.8.4.1.1.6.2, "the collating sequence defined by
      *> category LC_COLLATE"). In the es-ES collation (CLDR) letters
      *> differ first by letter and only then by case, so "a" < "b"
      *> and "b" < "B" < "c"; the native (code-point) order puts every
      *> capital before every small letter ("B" < "a").
      *>   REL  "a" < "B"  (relation, a)    locale true  -> LOC
      *>   88   "B" in 88 VALUE "a" THRU "c" (condition-name, b):
      *>        "a" < "B" < "c" under the locale, so true -> LOC
      *> Derived output: LOC-REL=LOC, LOC-88=LOC. Under the native
      *> sequence both print NAT. The CONTROL leg (c) and the
      *> literal-phrase sequences are g2obj03_collating_relation_88_
      *> control.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OBJ04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS ESA.
       SPECIAL-NAMES.
           ALPHABET ESA IS LOCALE ES
           LOCALE ES IS "es-ES".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-A             PIC X VALUE "a".
       01  W-B             PIC X VALUE "B".
           88 W-IN-AC      VALUE "a" THRU "c".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF W-A < "B" DISPLAY "LOC-REL=LOC"
           ELSE DISPLAY "LOC-REL=NAT" END-IF
           IF W-IN-AC DISPLAY "LOC-88=LOC"
           ELSE DISPLAY "LOC-88=NAT" END-IF
           STOP RUN.
