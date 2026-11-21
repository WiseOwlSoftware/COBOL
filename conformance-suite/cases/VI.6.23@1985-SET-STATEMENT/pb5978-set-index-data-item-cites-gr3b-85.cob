      *>--- conformance-case
      *> id: pb5978-set-index-data-item-cites-gr3b-85
      *> rules: [1985-GR-VI.6.23.4-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5978. A refusal at 85. §1985:VI.6.23.4 GR3b (cite.py:
      *> OK): "If identifier-1 references an index data item, it may be
      *> set equal to either the content of index-name-2 or
      *> identifier-2 where identifier-2 also references an index data
      *> item". XD is an index data item and 3 is an integer literal,
      *> the "No/3b" cell of GR5's table. The refusal cites GR3b, the
      *> rule the statement breaks, never §1985:VI.6.23.3 SR2. A
      *> compiler that accepts the SET, or one that names SR2, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5978S2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XD USAGE IS INDEX.
       PROCEDURE DIVISION.
       P-1.
           SET XD TO 3
           STOP RUN.
