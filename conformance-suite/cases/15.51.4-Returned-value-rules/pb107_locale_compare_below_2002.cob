*>--- conformance-case
*> id: pb107_locale_compare_below_2002
*> rules: [RV-15.51.4-3]
*> source-format: free
*> portable: unverified
*>---
      *> PB107 (kb/Work): the same function the accepted pb107_locale_compare_settings reaches the collation settings
      *> through, written for COBOL-85, which has no intrinsic LOCALE-COMPARE (introduced by ISO/IEC 1989:2002, §15):
      *> refused at 85 with COBOL1502, the version matrix's "introduced by" diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB107LB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE NUM IS "en-u-kn-true".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NUM=" FUNCTION LOCALE-COMPARE("a9" "a10" NUM)
           STOP RUN.
