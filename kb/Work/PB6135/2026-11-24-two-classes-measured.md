# Two more mechanical classes for the LIKELY-ERROR rows, and the insertion anchor (2026-11-24)

**Finding.** The 128 LIKELY-ERROR rows of the committed result held two groups no reader needs to open: header words of a
table that the layer reads in another order than the Markdown's cells (47 rows by a first sort, pages 32 to 861), and a
footnote or rule marker the transcription numbers higher than the print because an Amendment inserted one before it (22
rows, pages 328 to 332 and 428 and 429). The sort that found them was a script of mine over the 128 rows (page lists and
vocabulary); the classes below are not that sort but the tool's own rules, each failing closed.

**Changes in `scripts/spec/text_layer_diff.py`** (cases 17 to 19 of its self-test).
- TABLE-HEADER. `table_header_groups` reads, per page, each table header of the Markdown: a pipe row the separator row follows,
  or a line with a `<th>` cell, runs of such lines with only `<tr>`, `</tr>` and `<thead>` tags between them being one group (a
  header that wraps over rows is one). `_header_reading` takes, for each group, the Markdown words of its lines still
  unexplained and the text-layer words still unexplained at its place (a differing block whose Markdown position lies in the
  group or just after it) and calls them one reading of the header only when the two are the same words as a bag and neither is
  empty. A bag that differs (a word the layer damaged, one it lacks, one it repeats), a body row, and a stray header word far
  from the header are not readings. It runs before the generic moved-run pairing, so a header read in another order is that
  class whole.
- FOOTNOTE-RENUMBERED. `_renumbering` takes a hunk that is nothing but numbered markers (`(3)`, `8.`) and the layer's damage of
  a well-formed word (`literal-l.` for `literal-1.`), the same count on both sides in order, each pair of one shape with the
  Markdown's number higher; `_renumbered_hunks` excuses it only when the words just before and after it are equal on the print
  and the Markdown (the text it numbers matches), the offsets of one shape never fall in the order of the page, and a second
  marker of the page has the same shape and offset (a lone `15` for `13` is a typing error that looks the same).
- The two classes are named in `CLASSES` and counted in the verdict line like the others; neither is a class of difference
  the page review skips, only one it need not open.

**Premise corrected.** The dispatch (and PB6135's built note) say the print repeats a header on each page of a table that the
Markdown holds once. On every page measured the Markdown holds the header on the page too (p725 to p749 all do); the layer
reads the wrapped header cells line by line across the columns (`2ND 3RD ELEMENT STD STD` for the Markdown's `ELEMENT | 2ND
STD | 3RD STD`), which is the difference. The rule is written for that, and a header the Markdown held once with no header on
the continuation page would stay a LIKELY-ERROR here (none of the 128 was).

**The insertion anchor (PB6204).** `_stands_between` takes `ANCHOR` = 3 words of the page on each side of an inserted run or
a drop; the amended text of the note's own entry must hold them consecutively around it. Measured: AMENDMENT-APPLIED 33 to 33 and
its insertions 15 to 15 (the pages 20, 131, 151, 184, 411, 412, 415, 454, 496 and 649), so no committed row moved.

**Measured** (`python scripts/spec/text_layer_diff.py --arbitrate`, all 976 pages, the JSON committed with this change):

| Class | Before | After |
|---|---|---|
| OCR-NOISE | 5266 | 5260 |
| OCR-ARBITRATED | 1344 | 1344 |
| CORRECTION | 823 | 823 |
| AMENDMENT-APPLIED | 33 | 33 |
| TABLE-HEADER | 0 | 26 |
| FOOTNOTE-RENUMBERED | 0 | 17 |
| FIGURE | 1186 | 1186 |
| PUNCT | 2322 | 2322 |
| CASE | 423 | 423 |
| UNCOVERED | 807 | 807 |
| LIKELY-ERROR | 128 | 88 |
| UNEXPLAINED | 603 | 554 |
| amendment entries not located | 93 | 93 |

The 26 TABLE-HEADER rows are on pages 285, 338, 410, 489, 725 to 729, 731 to 736, 739 to 749 (489 and 748 were OCR-NOISE rows of
the moved-run pairing, now the header's own class); the 17 FOOTNOTE-RENUMBERED rows on pages 328, 330, 331, 428 and 429. The
rows they took are 40 LIKELY-ERROR, 49 UNEXPLAINED (the header's Markdown halves, and the OCR-noise word of a mixed marker
hunk) and 6 OCR-NOISE. A row stays where the evidence is absent, for example the p605 header (the layer's damaged `WHEN` makes the bags differ),
p329 and p332 (one marker alone at its offset), p330 L13638 (the Markdown carries a footnote sentence the print lacks) and p428
L17056 (a marker against a list letter); the 88 LIKELY rows are on the 46 pages `--check --likely` names.

**What stays open.** PB6135 stays open: 88 LIKELY-ERROR, 554 UNEXPLAINED and 807 UNCOVERED rows remain for the page review
(PB6051). The 47 and 22 of the finding are the first sort's counts, not the tool's; the table is the tool's.
