*>--- conformance-case
*> id: pb4234-leading-underscore-in-a-word
*> rules: [GR-8.1.3.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4234 - ISO 8.3.2.1: "The hyphen or underscore shall not appear as the first or last character in
      *> such words." _NAME is a COBOL word that breaks that rule, so the refusal names the rule and the word at every
      *> edition (COBOL-85 also lacks the underscore itself), instead of "unexpected '_'" beside a word it split.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4234L.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 _NAME PIC X(5) VALUE "CREME".
       PROCEDURE DIVISION.
           DISPLAY _NAME.
           STOP RUN.
