*>--- conformance-case
*> id: pb107_locale_compare_settings
*> rules: [RV-15.51.4-3, RV-15.51.4-5]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB107 (kb/Work): the UTS #35 collation settings the locale facility's collation engine implements, reached from
      *> COBOL through the BCP 47 -u- keys of a locale-name. Implementor content (the order of a locale is the CLDR data
      *> the implementor chose, Annex A.4.9); the standard fixes only what LOCALE-COMPARE returns:
      *>   cite.py --check 15.51.4 "'<' if argument-1 is less than argument-2"  ->  OK  15.51.4 5)
      *>   cite.py --check 15.51.4 "the locale used for comparison is the one associated with locale-name-1"  ->  OK  15.51.4 3)
      *> The expected values are computed from the settings' definitions (UTS #35 Part 5), not from the engine's output:
      *>   NUM-*    numericOrdering (-u-kn-true): "any sequence of Decimal Digits (General_Category = Nd) is sorted at a
      *>            primary level with its numeric value"; "A-21" < "A-123" is the standard's own example. Without the
      *>            key the digits compare one by one: '2' after '1', so ">". 02 and 2 are the same value: "=".
      *>   CASE-*   caseLevel (-u-kc-true) at primary strength (-u-ks-level1): "To ignore accents but take cases into
      *>            account, set strength to primary and case level to on" - e = e-acute, and lowercase before
      *>            uppercase at the case level: e < E, E > e-acute. Without the keys the accent is a secondary
      *>            difference: e < e-acute.
      *>   KANA-*   the quaternary relation (<<<<) of CLDR's Japanese private-kana: hiragana small a and katakana small a
      *>            are the same through level 3 ("=" under ja), and the hiragana is first at level 4 ("<" under
      *>            ja-u-ks-level4; JIS X 4061 4.4.10.2 (3), "Hiragana < Katakana").
      *> Non-ASCII appears only inside literals (UTF-8 source); every DISPLAY is ASCII.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB107LC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE PLAIN IS "en-US"
           LOCALE NUM IS "en-u-kn-true"
           LOCALE CAS IS "en-u-ks-level1-kc-true"
           LOCALE JA3 IS "ja"
           LOCALE JA4 IS "ja-u-ks-level4".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NUM-PLAIN-21-123=" FUNCTION LOCALE-COMPARE("A-21" "A-123" PLAIN)
           DISPLAY "NUM-21-123=" FUNCTION LOCALE-COMPARE("A-21" "A-123" NUM)
           DISPLAY "NUM-9-10=" FUNCTION LOCALE-COMPARE("a9" "a10" NUM)
           DISPLAY "NUM-PLAIN-02-2=" FUNCTION LOCALE-COMPARE("a02" "a2" PLAIN)
           DISPLAY "NUM-02-2=" FUNCTION LOCALE-COMPARE("a02" "a2" NUM)
           DISPLAY "NUM-19-100=" FUNCTION LOCALE-COMPARE("a19" "a100" NUM)
           DISPLAY "CASE-PLAIN-E-ACUTE=" FUNCTION LOCALE-COMPARE("e" "é" PLAIN)
           DISPLAY "CASE-E-ACUTE=" FUNCTION LOCALE-COMPARE("e" "é" CAS)
           DISPLAY "CASE-E-UPPER=" FUNCTION LOCALE-COMPARE("e" "E" CAS)
           DISPLAY "CASE-UPPER-ACUTE=" FUNCTION LOCALE-COMPARE("E" "é" CAS)
           DISPLAY "KANA-3=" FUNCTION LOCALE-COMPARE("ぁ" "ァ" JA3)
           DISPLAY "KANA-4=" FUNCTION LOCALE-COMPARE("ぁ" "ァ" JA4)
           DISPLAY "KANA-4-BACK=" FUNCTION LOCALE-COMPARE("ァ" "ぁ" JA4)
           STOP RUN.
