*>--- conformance-case
*> id: pb2763_locale_modifier_availability
*> rules: [GR-12.3.7.4-5, GR-14.9.39.4-24, RV-15.51.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2763. ISO §2023:12.3.7.4 GR5 — "The implementor specifies the allowable external-locale-names
      *> and the allowable content of literal-4"; WiseOwl COBOL's specification is DESIGN-locale-facility
      *> DETERMINATION L1: a POSIX "@modifier" that is a CLDR collation type becomes the -u-co- extension, and
      *> "any other modifier makes the locale unavailable". §2023:14.9.39.4 GR24 — "If the locale specified by
      *> locale-name-1 is not available, the EC-LOCALE-MISSING exception condition is set to exist" — and
      *> §2023:15.51.4 RV3 — "If the locale associated with locale-name-1 is not available, the EC-LOCALE-MISSING
      *> exception condition is set to exist".
      *> THE DEFECT: "es_ES@nosuchmod" normalized to the tag es-ES@nosuchmod, which no CLDR file names, but the
      *> .tailor lookup falls back to the tag's language and es.tailor is embedded, so the locale was AVAILABLE
      *> (Spanish) — no condition at SET LOCALE, none at LOCALE-COMPARE. A tag whose subtags are not RFC 5646
      *> subtags (one to eight letters or digits) now names no locale (LocaleTag.IsWellFormed).
      *> What each line proves (checking on; the USE declarative observes the fatal condition and RESUME AT
      *> NEXT STATEMENT continues, as pb64t1_ec_locale_missing does):
      *>   HANDLED / SET=done    SET LOCALE LC_COLLATE TO BM raises EC-LOCALE-MISSING.
      *>   HANDLED / BM=?        LOCALE-COMPARE under BM raises it at use; the MOVE is interrupted, R keeps "?".
      *>   TRAD=>                "es_ES@traditional" IS a collation type: es-ES-u-co-traditional, available, and
      *>                         traditional Spanish orders "ch" as its own letter after every "c" word: "ch" > "cz".
      *>   MODERN=<              "es_ES" (modern Spanish): "ch" < "cz", letter by letter.
       >>TURN EC-LOCALE-MISSING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2763MOD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE BM IS "es_ES@nosuchmod"
           LOCALE TR IS "es_ES@traditional"
           LOCALE MD IS "es_ES".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R               PIC X VALUE "?".
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-LOCALE-MISSING.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SET LOCALE LC_COLLATE TO BM
           DISPLAY "SET=done"
           MOVE FUNCTION LOCALE-COMPARE("ch" "cz" BM) TO R
           DISPLAY "BM=" R
           MOVE FUNCTION LOCALE-COMPARE("ch" "cz" TR) TO R
           DISPLAY "TRAD=" R
           MOVE FUNCTION LOCALE-COMPARE("ch" "cz" MD) TO R
           DISPLAY "MODERN=" R
           STOP RUN.
