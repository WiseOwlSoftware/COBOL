*>--- conformance-case
*> id: pb6116-picture-two-extended-floating-strings
*> rules: [SR-13.18.40.3-27]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6116 - ISO 1989:2023 13.18.40.3 SR27: "No more than one of the following may be specified in
      *> character-string-1: a string of two or more symbols '+'; ... a string of two or more currency symbols;
      *> ...", and NOTE 5 makes +++$$$ invalid. 13.18.40.5 GR6 names the extended editing sign control symbols
      *> among the floating insertion symbols, so a string of two or more of one character-1 is a floating
      *> insertion string as '+', '-' and the currency symbol's are, and a character-string holds one:
      *> PE01 LL99FF - the FF after the digits is a second floating string, of another character-1. Before
      *> kb/Work PB6116 it bound clean and MOVE -5 rendered " -0005", which no rule describes.
      *> PE02 LL9LL  - the same two strings of one character-1, a digit position between them.
      *> COBOL1934, 2023 only (the EDITING phrase is a COBOL-2023 introduction, Annex E.3.3 item 19).
      *> The positive sibling pb6116_picture_one_extended_floating_string_2023 holds the shape SR27 admits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6116NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PE01 PIC LL99FF EDITING L FOR NEGATIVE IS "-"
                EDITING F FOR NEGATIVE IS "-".
       01 PE02 PIC LL9LL EDITING L FOR NEGATIVE IS "-".
       PROCEDURE DIVISION.
           STOP RUN.
