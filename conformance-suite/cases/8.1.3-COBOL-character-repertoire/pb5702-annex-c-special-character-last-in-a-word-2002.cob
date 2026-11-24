*>--- conformance-case
*> id: pb5702-annex-c-special-character-last-in-a-word-2002
*> rules: [GR-8.1.3.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5702 - §2002:8.1.2.1 GR4b: "Characters identified as special characters in Annex C shall not be
      *> written as the first or last character of a user-defined word." U+00B5 MICRO SIGN is a special character of
      *> the 2002 Annex C (C.2.1), so at COBOL 2002 a word may not end on it. From 2023 Annex B permits it anywhere.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5702N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Xµ PIC X(3) VALUE "OK".
       PROCEDURE DIVISION.
           DISPLAY Xµ.
           STOP RUN.
