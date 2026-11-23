      *>--- conformance-case
      *> id: pb3229-sign-numeric-edited-category
      *> rules: [SR-13.18.52.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3229 - ISO 13.18.52.3 SR1 (cite.py OK): the SIGN clause may
      *> be specified only for "a numeric data or screen description entry
      *> whose picture character-string contains the symbol 'S'". E is
      *> numeric-edited (PIC ZZ9, 8.5.2.1 Table 2), so the entry is refused,
      *> and the refusal names the category in Table 2's word, numeric-edited.
      *> It used to print the model's enum name, "category numericedited".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3229SG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E PIC ZZ9 SIGN IS LEADING.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY E
           STOP RUN.
