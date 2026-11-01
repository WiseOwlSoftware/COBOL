*>--- conformance-case
*> id: pb4234-underscore-inside-a-word
*> rules: [GR-8.1.3.2-4]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4234 - ISO 8.3.2.1: "Each character of a COBOL word ... shall be selected from the set of basic
      *> letters, basic digits, extended letters, and the basic special characters hyphen and underscore. The hyphen or
      *> underscore shall not appear as the first or last character in such words." The underscore is a COBOL-2002
      *> introduction (the COBOL-85 set is hyphen-only). Inside a word, after a letter and before a letter or a
      *> digit, it is legal; the refused spellings (_NAME, NAME_) are the sibling negatives.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4234P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MY_NAME PIC X(5) VALUE "CREME".
       01 A1_2-B PIC 9(2) VALUE 42.
       PROCEDURE DIVISION.
           DISPLAY MY_NAME " " A1_2-B.
           STOP RUN.
