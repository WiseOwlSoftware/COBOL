      *>--- conformance-case
      *> id: pb5978-set-integer-item-cites-gr3c-85
      *> rules: [1985-GR-VI.6.23.4-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5978. A refusal at 85. §1985:VI.6.23.4 GR3c (cite.py:
      *> OK): "If identifier-1 does not reference an index data item,
      *> it may be set only to an occurrence number that corresponds
      *> to the value of index-name-2. Neither identifier-2 nor
      *> integer-1 can be used in this case." K is an elementary
      *> integer item, so it satisfies §1985:VI.6.23.3 SR2 and is a
      *> legal identifier-1; the statement is refused for GR3c (the
      *> "No/3c" cell of GR5's table: integer data item receiving an
      *> integer literal). The refusal cites GR3c, not SR2, which the
      *> program satisfies. A compiler that accepts the SET, or one
      *> that names SR2, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5978S1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9(4).
       PROCEDURE DIVISION.
       P-1.
           SET K TO 3
           STOP RUN.
