# 2026-11-11 - every confirmed 2002 claim repaired from the rendered page and decided; the ledger holds the transcription

Branch `fix-pb5332-2002-repairs` (with kb/Work PB5332). All 83 confirmed claims of `LEDGER-2002.json` are decided
APPLIED, each with its edits, and 46 sweeps record each repair's siblings on pages no claim named. The ledger is in
`verify_publishable.LEDGER`: `verify_publishable.py --edition 2002` reads "882 of 882 pages compared, every repair on
its page, every claim decided". By pattern:
- **Sub- and superscripts.** Found by measurement: every span of the PDF's text layer set smaller and lowered or raised
  against its line (37 spans on 9 pages: pages 24, 42, 144, 438, 562, 639, 653, 663 and 671). Each is now
  `<sub>`/`<sup>`. Two of the spans are sweep finds (`argument-2<sub>i</sub>`, `argument-1<sub>i</sub>`), and the p24
  footnote mark is a third. ISO/IEC 1989:2023 prints the same formulas flat (`argument-11`, PDF page 915, rendered), so
  2023 has no sibling to fix. For the same reason three 2002 RV rules, §2002:15.48.3, 15.61.3 and 15.71.3, no longer pair
  word for word with their 2023 counterparts (see the pairing counts below).
- **General formats.** The six claimed figures were fixed in the generator, never by hand: `render_figure.split_fused`,
  continuation indents, `rejoin_lines`, `figure_extract.MIN_RULE_W` 2.5, and `spec_headings.INTRODUCES_FORMAT` for
  A.1.1. The generator then regenerated eight sibling 2002 figures and five 2023 ones (§2023:12.3.7.2, 12.4.5.1,
  13.18.35.2, 13.18.43.2, 14.9.47.2). Each is a layout change only, with every word the same. The three 2023 FMT rows
  and ten 1985 change records that cite them were re-affirmed against the new text.
- **Tables.** Header rows restored, wrapped cells put back, rows split or joined, and blank `<br>` lines added so the
  columns pair line by line. Swept: every 'Character Positions' table, every 'Term | Subclause' table, and the
  reserved-word, function-name and directive-word lists.
- **The Index** (pages 859-881) was regenerated from the text layer's measured 18 pt levels.
- **Annex E illustrations** (E.1, E.3-E.16) are drawn as box art from their pages.
- **Two wrapped clause headings** (§2002:8.4.5.1.1, 8.8.4.1.1.4) are joined. A sweep of every heading found no other.

**2002 rule catalog:** 3290 -> 3298 rules. **Paired to 2023** (`lineage.py --candidates 2002`): 1771 -> 1770. Two
pairs were gained (§2002:14.8.1.3 GR9, GR10) and three lost (the RV rules above).

**What remains of this note:** the refuter pass over every drawn figure (the four carried things, FIGURE-STYLE.md),
the `docs/VERSION_CHANGE_REFERENCE.md` entry, the two figure-count mismatches (§2002:11.1.1, §2002:13.14.1), the 89
pending clauses of the 2002 content-keyed manifest, and the 2002 inventory (PB4886).
