      *>--- conformance-case
      *> id: pb5160-apostrophe-literal-85
      *> rules: [1985-FMT-IV.4.2.2.2.1.1, 1985-SR-IV.4.2.1-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5160 - §1985:IV.4.2.2.2.1: "A nonnumeric literal is a
      *> character-string delimited at the beginning and at the end by
      *> the separator quotation mark" (cite.py --check OK), and
      *> §1985:IV.4.2.1 SR5: "The punctuation character quotation mark
      *> is a separator" (cite.py --check OK) - the one literal
      *> separator; the apostrophe is in no 1985 separator rule. The
      *> apostrophe became a quotation symbol in COBOL-2002 (§2002:F.2
      *> item 3, "Apostrophe as quotation symbol"). So 'HI' below is no
      *> 1985 literal and is refused at 85 with COBOL0900, the
      *> introduction gate. It fails when the lexer delimits a literal
      *> by the apostrophe at every edition: the program then ran and
      *> displayed HI.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5160AP85.
       PROCEDURE DIVISION.
       P1.
           DISPLAY 'HI'.
           STOP RUN.
