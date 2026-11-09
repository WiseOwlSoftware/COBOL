      *>--- conformance-case
      *> id: pb4933-options-paragraph-with-an-item-named-options
      *> rules: [FMT-11.9.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4933 - at COBOL-85, where 8.9 leaves OPTIONS free, a data item named OPTIONS
      *> is a user-defined word, and the OPTIONS paragraph (§2023:11.9.2), a COBOL-2002 paragraph,
      *> draws the same edition refusal COBOL0804 as without the item.
      *> The reservation gate retypes the declared free word to a user-defined word, but gives
      *> the header back its keyword where its user-word reading fails (kb/Work PB4933, PB4958).
      *> Fails if the paragraph draws COBOL1971 (not a paragraph of the IDENTIFICATION DIVISION).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4933O.
       OPTIONS.
           DEFAULT ROUNDED MODE IS TRUNCATION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OPTIONS PIC X.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
