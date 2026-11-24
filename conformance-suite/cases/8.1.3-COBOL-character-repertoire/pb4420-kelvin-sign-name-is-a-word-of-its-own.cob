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
      *> WORK-KX and WORK-KX are two user-defined words, two items.
      *> (The Kelvin sign may stand inside a word at every edition from
      *> 2002; it is a special character of the 2002 Annex C.2.1, barred
      *> first and last by 8.1.2.1 GR4b, which is why it is medial here.)
      *> It fails when the fold maps U+212A to k.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4420K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WORK-KX PIC X(4) VALUE "KELV".
       01 WORK-KX PIC X(4) VALUE "BASK".
       PROCEDURE DIVISION.
           DISPLAY WORK-KX.
           DISPLAY WORK-KX.
           DISPLAY work-kx.
           STOP RUN.
