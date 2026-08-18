      *> reject-at: 2002 2014 2023
      *> ISO 1989:2023 13.18.40.2 Format 2 - `PIC IS character-string-1 LOCALE [IS locale-name-1] SIZE IS integer-1`
      *> is an element of the optional locale module (Annex A.4.9 item 8) that COBOL.NET documents as not supported
      *> (CONFORMANCE.md 4 item 5): refused BY NAME with COBOLNET1518 (kb/Work PB100 - it was a raw parse error at
      *> SIZE). Both spellings (with and without locale-name-1) parse and are named.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB100PL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr_FR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P1 PIC +$9.9 LOCALE SIZE IS 10.
       01 P2 PICTURE IS +$$9.99 LOCALE IS FR SIZE 12.
       PROCEDURE DIVISION.
           STOP RUN.
