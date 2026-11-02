      *>--- conformance-case
      *> id: pb4420-kelvin-spelled-key-is-a-user-word
      *> rules: [GR-8.1.3.2-4]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4420 (it was kb/Work PB1965's refusal COBOL0901) -
      *> ISO 8.1.3.2 4) b): "An uppercase extended letter is treated as
      *> though it were folded to its corresponding lowercase extended
      *> letter, if any, in accordance with the case mapping described
      *> in Annex C" (cite.py --check: OK). KEY, the Kelvin sign for its
      *> first letter (permitted first from 2023, Annex E.3.3 item 4),
      *> is not folded to the reserved word KEY (8.9): the pair
      *> (212A,006B) is not applied, so the data-name is a user-defined
      *> word that 8.3.2.1 1) does not forbid. It fails when the fold
      *> maps U+212A to k.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4420Q.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KEY PIC X VALUE "A".
       PROCEDURE DIVISION.
           DISPLAY KEY.
           STOP RUN.
