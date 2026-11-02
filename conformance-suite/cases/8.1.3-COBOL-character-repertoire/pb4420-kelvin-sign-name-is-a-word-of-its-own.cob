      *>--- conformance-case
      *> id: pb4420-kelvin-sign-name-is-a-word-of-its-own
      *> rules: [GR-8.1.3.2-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4420 (owner decision 2026-11-01, follow GnuCOBOL) -
      *> ISO 8.1.3.2 4) b): "An uppercase extended letter is treated as
      *> though it were folded to its corresponding lowercase extended
      *> letter, if any, in accordance with the case mapping described
      *> in Annex C" (cite.py --check: OK). Annex C.2 pairs U+212A
      *> KELVIN SIGN with the BASIC letter k, (212A,006B); k is no
      *> lowercase EXTENDED letter, so the fold does not apply the pair:
      *> WORK-K and WORK-K are two user-defined words, two items. (The
      *> Kelvin sign may end a word at every edition from 2002: Annex
      *> B.3 item 2, Annex E.3.3 item 4 added it as a first character at
      *> 2023.) It fails when the fold maps U+212A to k.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4420K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WORK-K PIC X(4) VALUE "KELV".
       01 WORK-K PIC X(4) VALUE "BASK".
       PROCEDURE DIVISION.
           DISPLAY WORK-K.
           DISPLAY WORK-K.
           DISPLAY work-k.
           STOP RUN.
