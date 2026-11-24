*>--- conformance-case
*> id: pb5702-annex-c-special-character-inside-a-word
*> rules: [GR-8.1.3.2-4]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5702 - §2002:8.1.2.1 GR4b bars the special characters of Annex C (U+00B5 MICRO SIGN, and U+00B7
      *> MIDDLE DOT, which C.2.2 adds) only from the first and last place of a user-defined word; inside a word, between
      *> basic letters, each is permitted. The sibling negative ends a word on U+00B5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5702P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Xµy PIC X(3) VALUE "ONE".
       01 L·L PIC X(3) VALUE "TWO".
       PROCEDURE DIVISION.
           DISPLAY Xµy " " L·L.
           STOP RUN.
