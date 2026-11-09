      *>--- conformance-case
      *> id: pb4933-repository-paragraph-with-an-item-named-repository
      *> rules: [FMT-12.3.8.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4933 - at COBOL-85, where 8.9 leaves REPOSITORY free, a data item named
      *> REPOSITORY is a user-defined word, and the REPOSITORY paragraph (§2023:12.3.8.2), a
      *> COBOL-2002 paragraph, draws the same edition refusal COBOL0900 as without the item.
      *> The reservation gate retypes the declared free word to a user-defined word, but gives
      *> the header back its keyword where its user-word reading fails (kb/Work PB4933, PB4958).
      *> Fails if the paragraph draws COBOL1971 (not a paragraph of the CONFIGURATION SECTION).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4933R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REPOSITORY PIC X.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
