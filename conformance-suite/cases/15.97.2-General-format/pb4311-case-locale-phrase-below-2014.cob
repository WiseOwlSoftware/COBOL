*>--- conformance-case
*> id: pb4311-case-locale-phrase-below-2014
*> rules: [FMT-15.97.2, FMT-15.57.2]
*> source-format: free
*> portable: unverified
*>---
      *> The control of pb64t5_case_locale_phrase (kb/Work PB4311). The printed 2002 formats of FUNCTION UPPER-CASE
      *> (15.97.2) and LOWER-CASE (15.57.2) are ( argument-1 ) with no phrase; the 2009 CD that became the 2014 edition
      *> prints ( argument-1 [ LOCALE locale-name-1 ] ) and lists it among its substantive changes (D.3 items 9 and 23).
      *> The SPECIAL-NAMES LOCALE clause declaring TR is itself a 2002 form, so at 2002 only the phrase is refused by
      *> edition (COBOL0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4311LPB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE TR IS "tr-TR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION UPPER-CASE("i" LOCALE TR) TO S
           STOP RUN.
