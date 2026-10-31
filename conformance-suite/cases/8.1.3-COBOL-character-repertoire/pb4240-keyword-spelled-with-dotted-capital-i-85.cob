      *>--- conformance-case
      *> id: pb4240-keyword-spelled-with-dotted-capital-i-85
      *> rules: [GR-8.1.3.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4240 - the COBOL-85 twin of
      *> pb4240_keyword_spelled_with_dotted_capital_i. DİSPLAY is the
      *> word DISPLAY by the Annex C fold (8.1.3.2 GR4 b)), but COBOL-85
      *> has no extended letters (8.3.2.1: a 2002 introduction), so the
      *> U+0130 in it is refused there (COBOL0900) even though the word
      *> is the DISPLAY keyword. It fails when the extended-letter
      *> screen asks only the IDENTIFIER token, and the keyword the fold
      *> made of the word compiles at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4240KW85.
       PROCEDURE DIVISION.
           DİSPLAY "HI".
           STOP RUN.
