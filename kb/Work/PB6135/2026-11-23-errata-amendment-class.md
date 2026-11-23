# Amendment-applied differences are a class of their own (2026-11-23, revised after the round-2 review)

**Finding.** A triage of the 21 LIKELY-ERROR rows on p21, p28, p132, p152, p168, p345 and p474 found no transcription error.
20 sat under a Markdown `> ⚠ **CORRECTED — see the Errata (Cnn).**` note that says an Amendment's text was `applied in place`
(Amendment 1, X3.23a-1989, or Amendment 2, X3.23b-1993): the PDF prints the base standard and the Markdown the amended text.
One was the digit zero in a quoted character range (p152) read as the letter O by both the text layer and tesseract. The md
excerpt of one row (p132 L5662) had dropped words that had paired off as OCR noise.

**Changes in `scripts/spec/text_layer_diff.py`** (cases 11 to 14 of its self-test).
- AMENDMENT-APPLIED: `amendment_notes` reads, per page, each CORRECTED note that names an Amendment and says `applied in place`
  (a `damaged letter` note is no amendment): its first backticked quote is what the standard prints, its other quotes are the
  amended text, and `(page N, folio A-n)` or `B-n` cites the Amendment's own PDF page. A difference is AMENDMENT-APPLIED only
  when BOTH sides hold: the layer's words are a run of the printed quote, and the Markdown's words are a run of the amended
  text (a quote, or the text layer of the cited Amendment page: `with_cited_text`; most notes say `the text above` and quote
  nothing, so the Amendment's own print is the independent evidence). Words the Markdown dropped qualify only when the two
  words around the gap are consecutive in the amended text; words it added only when they are a run of it. Round 1 checked
  the layer's side alone, so `13 modules` for the amended `12` was also accepted (review R2-1; planted in case_11).
- A zero read as O: only a number of two digits or more holding a zero (`100` against `1OO`) is OCR-NOISE, and only when both
  readers give the letter. A lone `0` against `O` is decided neither by context nor by the page's words (`'Z', '0', '9'` and
  `'Z', 'O', '9'` read alike) and stays LIKELY-ERROR; the p152 range row is therefore LIKELY-ERROR again, a triaged false
  positive for a reader of the page (review R2-2; case_12).
- A row's `pdf` and `md` excerpts are the whole aligned block, not what is left after words paired off.

**Counts over 976 pages** (`python scripts/spec/text_layer_diff.py --arbitrate`):

| outcome | before the class | round 1 (both-side check missing) | now |
|---|---|---|---|
| OCR-NOISE | 5269 | 5251 | 5258 |
| OCR-ARBITRATED | 1344 | 1344 | 1343 |
| CORRECTION | 823 | 823 | 823 |
| AMENDMENT-APPLIED | 0 | 74 | 88 |
| FIGURE | 1186 | 1185 | 1186 |
| PUNCT | 2322 | 2322 | 2322 |
| CASE | 423 | 423 | 423 |
| UNCOVERED | 823 | 811 | 773 |
| LIKELY-ERROR | 141 | 88 | 114 |
| UNEXPLAINED | 607 | 598 | 599 |

Of round 1's 74 rows, 37 are still AMENDMENT-APPLIED, 22 are LIKELY-ERROR, 8 OCR-NOISE, 4 UNEXPLAINED and 3 in other classes or
re-cut blocks; the Amendment pages confirm 51 more rows than the round-1 quotes did. What stays LIKELY-ERROR on the seven pages:
the renumbered 4.3.8.4 headings (p168, an instruction to renumber, not text on the cited page), the PERFORM VARYING sentences
(p345) and `specification`/`specifications` (p28), each for a reader of the Amendment page.
