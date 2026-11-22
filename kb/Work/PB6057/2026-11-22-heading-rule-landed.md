## The heading rule is built, applied and audited

`scripts/spec/heading_levels.py` holds the rule of this note in one place (`level_of`, `canonical`, `normalize`) and its audit
(`check`, called by `verify_publishable.py --edition 1985`, a gate audit); its cases are
`scripts/selftests/spec/heading_levels/test_heading_levels.py`. The rule is the note's: Section 1, Appendix 2, chapter 2, clause 3,
one deeper for each further number, at most 6.

**Measured on the text before the change** (`git show 554c7c9305:specs/ANSI_COBOL_1985.md`): the audit named 1,276 findings, and the normalizer changed 1,228 heading lines: 872 levels and 148 plain numbered titles in Sections I to XVIII, 200 levels and 2 plain titles in Amendment 1's Section A, and 6 running headers made plain (pages 380, 444, 693, 812, 816 and 889), besides 18 stray `# SECTION` dividers deleted (each Section's title is now the level-1 heading at the top of its first page). Fourteen headings of six or seven number parts share Markdown's sixth level. The 148 plain titles became headings because the printed page sets each as one (read on pages 27, 396, 711 and 413, and the pages the claims named); the contents entries and their folios agree with the body but for two entries the standard prints differently from its own body (`PRINTED_DIFFERENTLY`: Section XI chapter 1, page 553; Section XIII clause 4.8, page 654).

**The drift check.** The regenerated rule catalog has the same 3,109 rules with the same ids and texts; ONE row changed:
`§1985:VII.1.3.7` paragraph 1 (the LINAGE-COUNTER paragraph), its `box` `part` to `whole` and its `level` 1 to 2. The cause is
the running header of page 380, which was a heading and so closed the rule block before the page break; as plain text it no longer
does, and the paragraph, which is one boxed line, is wholly boxed. The unharvested-block manifest is unchanged, and `cite.py`
reads `#{1,6}` and the number, so no locator moved. Six code anchors named the old heading lines or levels and were moved: the body marker (`spec_pdf.EDITIONS[1985].body`), the
heading that closes the reserved-word list (`reserved_word_order._columns_1985`, `gen-reserved-words.ps1`,
`ReservedWordsDriftTests`), the `Arguments` headings of Section A (`IntrinsicArgumentDomainDriftTests`, level 4 now) and the
planted texts of `verify_publishable`'s self-test. The first gate found the last two, which read the text by level; every reader
now reads the level the rule gives.

**Not in scope, and why.** Amendment 1's change list (pages 831 to 856) and Amendment 2 quote the standard's clause numbers
(`**4.3.8.3 Function-Identifier**`, `Paragraph 4.5.4 of ...`) as change instructions and print their own structure; they are not
headings of the standard. Section A (pages 857 to 905) is set out like the modules and is held to the rule.
