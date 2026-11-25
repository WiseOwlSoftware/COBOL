*>--- conformance-case
*> id: pb6175-word-outside-annex-c-cites-2002
*> rules: [GR-8.1.3.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6175 - at COBOL 2002 a user-defined word is made of "basic letters, basic digits, extended letters, and
      *> the basic special characters hyphen and underscore" (§2002:8.3.1.1), and "The set of extended letters consists of
      *> characters from the repertoire specified in Annex C" (§2002:8.1.2.1 GR4). U+1E900 ADLAM CAPITAL LETTER ALIF is in
      *> no 2002 Annex C row (Adlam came with Unicode 9.0; COBOL 2023's Annex B.3 adds it), so the word is refused, and the
      *> message names the 2002 rule and Annex C, not the 2023 rule and Annex B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6175W.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A𞤀B PIC X(3) VALUE "OK".
       PROCEDURE DIVISION.
           DISPLAY A𞤀B.
           STOP RUN.
