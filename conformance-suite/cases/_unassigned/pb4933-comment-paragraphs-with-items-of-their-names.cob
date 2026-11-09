      *>--- conformance-case
      *> id: pb4933-comment-paragraphs-with-items-of-their-names
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4933 - from COBOL-2002, where 8.9 leaves AUTHOR, INSTALLATION, DATE-WRITTEN,
      *> DATE-COMPILED and SECURITY free, data items of those names are user-defined words, and
      *> the five COBOL-85 comment paragraphs (deleted by COBOL-2002, VCR Table 7) draw the same
      *> named removal COBOL0902 as without the items.
      *> The reservation gate retypes the declared free word to a user-defined word, but gives
      *> the header back its keyword where its user-word reading fails (kb/Work PB4933, PB4958).
      *> Fails if a paragraph header draws COBOL0001.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4933A.
       AUTHOR. ME.
       INSTALLATION. HERE.
       DATE-WRITTEN. TODAY.
       DATE-COMPILED. TODAY.
       SECURITY. NONE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AUTHOR PIC X.
       01 INSTALLATION PIC X.
       01 DATE-WRITTEN PIC X.
       01 DATE-COMPILED PIC X.
       01 SECURITY PIC X.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
