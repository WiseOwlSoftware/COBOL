# Amendment-applied differences are a class of their own (2026-11-23, revised after the round-2 review)

**Finding.** A triage of the 21 LIKELY-ERROR rows on p21, p28, p132, p152, p168, p345 and p474 found no transcription error.
20 sat under a Markdown `> ⚠ **CORRECTED — see the Errata (Cnn).**` note that says an Amendment's text was `applied in place`
(Amendment 1, X3.23a-1989, or Amendment 2, X3.23b-1993): the PDF prints the base standard and the Markdown the amended text.
One was the digit zero in a quoted character range (p152) read as the letter O by both the text layer and tesseract. The md
excerpt of one row (p132 L5662) had dropped words that had paired off as OCR noise.

**Changes in `scripts/spec/text_layer_diff.py`** (cases 11 to 14 of its self-test).
- AMENDMENT-APPLIED: `amendment_notes` reads, per page, each CORRECTED note that names an Amendment and says `applied in place`
  (a `damaged letter` note is no amendment): its first backticked quote is what the standard prints, its other quotes are the
  amended text, and `its change for page P (page N, folio A-n): "instruction"` names the Amendment's entry (round 3: R3-1). A
  difference is AMENDMENT-APPLIED only when BOTH sides hold: the layer's words are a run of the printed quote, and the
  Markdown's words are a run of the amended text. Most notes quote no amended text (`the text above`), so the amended text is
  the text layer of THAT NOTE'S OWN entry on the cited Amendment page (`entry_text`): the row of the page's change table whose
  page cell and first eight instruction words are the note's, exactly one row; its words run in the layer from the place its
  first words are found to the place the next row's are found. Fail closed: no row, two rows, a start the layer does not show
  or a successor it does not show gives no entry, the note's rows stay LIKELY-ERROR/UNEXPLAINED, and the `--check` summary
  counts it (`amendment entry not located: N`, stored in the result). Round 2 took the whole cited page's text, so a change of
  another entry on that page could vouch for a wrong word (planted in case_14). Dropped words qualify only if the two words
  around the gap are consecutive in the amended text; added words only as a run of it.
- A zero read as O: only a number of two digits or more holding a zero (`100` against `1OO`) is OCR-NOISE, and only when both
  readers give the letter. A lone `0` against `O` is decided neither by context nor by the page's words (`'Z', '0', '9'` and
  `'Z', 'O', '9'` read alike) and stays LIKELY-ERROR; the p152 range row is therefore LIKELY-ERROR again, a triaged false
  positive for a reader of the page (review R2-2; case_12).
- A row's `pdf` and `md` excerpts are the whole aligned block, not what is left after words paired off.

**Counts over 976 pages** (`python scripts/spec/text_layer_diff.py --arbitrate`):

| outcome | before the class | round 1 | round 2 (whole page) | now (own entry) |
|---|---|---|---|---|
| AMENDMENT-APPLIED | 0 | 74 | 88 | 57 |
| LIKELY-ERROR | 141 | 88 | 114 | 126 |
| UNEXPLAINED | 607 | 598 | 599 | 602 |
| OCR-NOISE | 5269 | 5251 | 5258 | 5261 |
| UNCOVERED | 823 | 811 | 773 | 787 |

OCR-ARBITRATED 1344, CORRECTION 823, FIGURE 1186, PUNCT 2322 and CASE 423 are as before (OCR-ARBITRATED 1343 in round 2).
Of the 300 amendment notes, 207 have their entry located; 93 do not (45 name no row of the Amendment page by page cell and
instruction, 25 name more than one, 20 have a row start or successor the layer does not show, 3 quote no instruction), and
their rows stay unexplained for a reader of the Amendment page. Only the entries' rows on p132 and p152 of the seven triaged
pages are AMENDMENT-APPLIED now.
