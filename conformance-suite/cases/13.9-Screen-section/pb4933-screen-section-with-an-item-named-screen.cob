      *>--- conformance-case
      *> id: pb4933-screen-section-with-an-item-named-screen
      *> rules: [FMT-13.9.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4933 - at COBOL-85, where 8.9 leaves SCREEN free, a screen item named SCREEN
      *> is a user-defined word, and the SCREEN SECTION (§2023:13.9.2), a COBOL-2002 facility this
      *> compiler declines (Annex A.4.2, docs/CONFORMANCE.md section 5), draws the same named
      *> refusal COBOL1560 as without the item.
      *> The reservation gate retypes the declared free word to a user-defined word, but gives
      *> the header back its keyword where its user-word reading fails (kb/Work PB4933, PB4958).
      *> Fails if the header draws COBOL0001 "unexpected 'SCREEN'".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4933S.
       DATA DIVISION.
       SCREEN SECTION.
       01 SCREEN LINE 1.
       01 S2 BLANK SCREEN.
       01 S3 ERASE SCREEN.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
