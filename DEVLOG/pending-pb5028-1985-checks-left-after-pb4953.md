## The four 1985 transcription checks PB4953 left undone are made (PB5028)

PB5028 named four checks over `specs/ANSI_COBOL_1985.md`. Each was read against the rendered page
(`render-spec-page.py --edition 1985`) and the PDF text layer.

- **Module-column cells of pages 58-83.** Each page's table cells, in reading order per column, were compared with the PDF's text
  layer. Pages 61-83 agree apart from OCR misreads of the module letters (`Nut` for `NUC`, `RFL` for `REL`) and a stray mark on page
  67. The level tables on pages 58 and 59 do not reach the text-layer comparison and were read on the rendered page, cell by cell.
  Page 60 is prose. No cell changed. The continuation rows (empty first cell) on pages 67 and 81 were read against the page: each stacked
  module cell sits under its label as printed.
- **212 underlined lines.** The text-layer measure cannot place an underline reliably (word boxes skew on a scan), so each of the 212 was
  measured against the words of its `<u>` span, and the 35 that still failed were read on contact sheets cropped from the page. All 212
  are underlined in print (`Page No.` / `Change to ANSI ...` carry the head rule of their column). Nothing was dropped.
- **49 unmatched headings.** 18 are editorial `# SECTION ...` / `# AMENDMENT ...` dividers on a blank page or after a folio (the page
  prints no such line), 15 are folios (`XIII-2` and the like), one is page 216's `GENERAL FORMAT FOR COBOL VERBS` (underlined in print,
  fixed below; the OCR spaces its letters), and the rest are sub-headings (`5.9.2 General Format`), cover-page lines, boxed headings or
  amendment text, none underlined in print, plus two underlined lines (`SUMMARY OF DIFFERENCES ...`, `FUNCTION-IDENTIFIER:`) already marked.
- **Missed underlines found by the sweep.** Reading the other direction (a heading shape the page underlines and the transcription leaves
  bare) found 33 lines the first pass left: every top-level `N. TITLE` heading it missed (pages 21, 101, 121, 283, 375, 558, 560, 576, 585,
  660, 699), 14 `GENERAL FORMAT FOR ...` titles (pages 194-228, one of them inside a `<pre>`), `CONDITION-NAME CONDITION:`, and the
  title lines of `SECTION I`, `VII`, `VIII` and `XIV`, and `MISCELLANEOUS FORMATS` (page 230). They now carry `<u>` over their words, not their number. A density measure of the rule under a
  heading gave 600 false hits on the Amendment 1 pages and was abandoned; the shape list plus a read of every hit on the page is the method
  the state document now records.
- **Page 71's paragraph.** It is one table cell of three printed lines at a 7-character indent; it carries `&nbsp;` x 7 on each
  `<br>`-separated line. Page 736 carries the same paragraph at a 3-character hang after the first line; fixed the same way (the sibling).

The ledger gained 35 sweep entries (`heading-underlines(PB5028)`, `table-body-paragraph(PB5028)`). `verify_publishable.py --edition 1985`:
PUBLISHABLE; `lint_rendering.py --edition 1985` and `extract_rule_catalog.py --edition 1985 --check`: clean. The notes' closes_rows stay empty:
a transcription residue closes no inventory row. Self-review: no code changed; the one rule (the underline and indentation conventions)
stays in TRANSCRIPTION-STATE.md.
