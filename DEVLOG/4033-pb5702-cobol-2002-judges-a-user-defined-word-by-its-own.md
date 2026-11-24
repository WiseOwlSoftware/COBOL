## Entry 4033 — 2026-11-24 05:15 PST — PB5702: COBOL 2002 judges a user-defined word by its own Annex C, not the 2014 table

**Re-probe.** `01 Xµ PIC X(3)` (U+00B5 MICRO SIGN last) compiled and ran at `--std 2002` (output `OK`). §2002:8.1.2.1 GR4b
bars the special characters of Annex C from the first and the last place of a user-defined word, so COBOL2773 is
expected.

**Root cause.** `CobolCharacterRepertoire.PositionOf` answered 2002 and 2014 from one table, 2023's Annex B.3 with Annex E
reversed, on the belief that the standard records no Annex B change between 2002 and 2014. Both editions print their own
annex: 2002's Annex C.2 is TR 10176:2001 (narrower: no Syriac, Thaana, Sinhala, Myanmar ...), and it makes the special
characters (C.2.1's row, C.2.2's MIDDLE DOT) and the combining characters medial; 2023 permits the micro sign anywhere.

**Fix.** `scripts/spec/gen_annex_bc.py` transcribes 2002 Annex C.2.1 (letters, special row) and C.2.2, and the rows 2014's
Annex B.3.1 labels combining (2002 Annex C labels none and cites TR 10176:2001), into `C2002Letters`, `C2002Special`,
`C2002Additional` and `B2014Combining`. `Edition2002` builds the classes: special and combining characters medial, the rest of
C.2.1 anywhere, everything else not permitted. `Violation` words the medial refusal with §2002:8.1.2.1 GR4b at 2002 and
`IsWordAtSomeEdition` also asks 2002. `AnnexBCDriftTests` re-reads the 2002 and 2014 lists from the specifications and
checks `PositionOf` at 2002 for every code point; a second test holds the 2002 repertoire inside the 2014 or 2023 table.

**Goldens.** `pb5702-annex-c-special-character-inside-a-word` (accepted 2002 onward, output `ONE TWO`) and
`pb5702-annex-c-special-character-last-in-a-word-2002` (refused at 2002, COBOL2773). Unit rows added to
`Violation_IsTheEditionsAnnexB`.

**Register.** `2002-GR-8.1.2.1-4` is PARTIAL (GAP 1135 after the review batch, one row still open): the branch review (round 1, R1-1) found GR4c, the fold by 2002 Annex D, answered with the 2014 table; 86 pairs differ (XU+2160Y and XU+2170Y are one name at 2002, two by the spec). PB5702 stays open, with `kb/Work/PB5702/2026-11-24-gr4c-fold-is-the-2014-table.md` for the fix.

**Left open (COBOL 2014 waits).** 2014 is still the table derived from 2023, so `Xµ` is accepted at `--std 2014` though
§2014:8.1.2.1 GR4b bars it, and the non-ASCII digits are "not first" there where 2014's Digits row permits them anywhere.
Noted on PB5702.

