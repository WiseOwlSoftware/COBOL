      *>--- conformance-case
      *> id: pb4420-reserved-word-spelled-with-dotted-capital-i-is-a-user-word
      *> rules: [GR-8.1.3.2-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4420 (it was kb/Work PB4240's refusal COBOL0901) -
      *> ISO 8.1.3.2 4) b): "An uppercase extended letter is treated as
      *> though it were folded to its corresponding lowercase extended
      *> letter, if any, in accordance with the case mapping described
      *> in Annex C" (cite.py --check: OK). DİSPLAY is not folded to the
      *> reserved word DISPLAY (8.9): the pair (0130,0069) is not
      *> applied, so the data-name is a user-defined word that 8.3.2.1
      *> 1) ("Reserved words shall not be used as user-defined words or
      *> system-names", cite.py --check: OK) does not forbid. It fails
      *> when the fold maps U+0130 to i.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4420D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DİSPLAY PIC X VALUE "A".
       PROCEDURE DIVISION.
           DISPLAY DİSPLAY.
           STOP RUN.
