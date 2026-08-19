      *> reject-at: 2002 2014 2023
      *> ISO 1989:2023 12.3.7.2 - the ALPHABET clause's `IS LOCALE [locale-name-2]` phrase. Since kb/Work PB101 the
      *> bare form (A1, A3 below: the locale CURRENT at each use, 12.3.7.4 GR7e) is IMPLEMENTED — the derived
      *> CLDR/UCA collating sequence — and no longer diagnosed. The NAMED form (A2: locale-name-2 shall be a
      *> locale-name of the SPECIAL-NAMES LOCALE clause, 12.3.7.3 SR24) and the LOCALE clause itself are the next
      *> increment of the optional locale module (Annex A.4.9 item 10; CONFORMANCE.md 4 item 5) and stay refused BY
      *> NAME with COBOLNET1518 (kb/Work PB100 - it used to draw a false "reserved word used as a user-defined
      *> word"). Each alphabet still registers, so the PROGRAM COLLATING SEQUENCE reference binds without a cascade.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB100AL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS A1.
       SPECIAL-NAMES.
           LOCALE FR IS "fr_FR"
           ALPHABET A1 IS LOCALE
           ALPHABET A2 FOR ALPHANUMERIC IS LOCALE FR
           ALPHABET A3 FOR NATIONAL IS LOCALE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X.
       PROCEDURE DIVISION.
           STOP RUN.
