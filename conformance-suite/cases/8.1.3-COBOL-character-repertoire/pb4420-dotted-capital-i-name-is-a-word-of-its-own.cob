      *>--- conformance-case
      *> id: pb4420-dotted-capital-i-name-is-a-word-of-its-own
      *> rules: [GR-8.1.3.2-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4420 (owner decision 2026-11-01, follow GnuCOBOL) -
      *> ISO 8.1.3.2 4) b): "An uppercase extended letter is treated as
      *> though it were folded to its corresponding lowercase extended
      *> letter, if any, in accordance with the case mapping described
      *> in Annex C" (cite.py --check: OK). Annex C.2 pairs U+0130 LATIN
      *> CAPITAL LETTER I WITH DOT ABOVE with the BASIC letter i,
      *> (0130,0069); i is no lowercase EXTENDED letter, so the fold
      *> does not apply the pair: CUSTOMER-İD and CUSTOMER-ID are two
      *> user-defined words, two items. A basic letter still folds to
      *> its lowercase (8.1.3.2 3): customer-id is CUSTOMER-ID). It
      *> fails when the fold maps U+0130 to i: the two declarations
      *> become one name and every reference is ambiguous.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4420A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CUSTOMER-İD PIC X(4) VALUE "DOTI".
       01 CUSTOMER-ID PIC X(4) VALUE "BASI".
       PROCEDURE DIVISION.
           DISPLAY CUSTOMER-İD.
           DISPLAY CUSTOMER-ID.
           DISPLAY customer-id.
           STOP RUN.
