- **D-85-RENAMES-SAME-NAME — "the same data-name" in RENAMES is the same item, not the same word, at COBOL-85 (kb/Work
  PB5870).** This entry speaks for COBOL-85. "The same data-name" in RENAMES is the same item, not the same word. Row
  `1985-SR-VI.5.11.3-4`: data-name-2 and data-name-3 "cannot be the same data-name" (`§1985:VI.5.11.3 SR4`). This compiler
  refuses `RENAMES A THRU A` and accepts `RENAMES A OF G1 THRU A OF G2`, two different items that share a word, as GnuCOBOL
  does (`cobc/field.c#cb_validate_renames_item` compares the two resolved items; 3.3-dev, source read, not run). Decided by
  the precedence of CLAUDE.md rule 1 (the text leaves the choice, GnuCOBOL decides). Source: `kb/Work/PB5870.md`, item 2.
  Witnesses: `conformance:cases/VI.5.11@1985-RENAMES-CLAUSE/pb5870_renames_same_word_two_items_85` (the ruling's probe
  `r4c.cob`, made a case) pins the two-item arm; `conformance:cases/13.18.45-RENAMES/pb1283_renames_legal` is the row's own
  case.
