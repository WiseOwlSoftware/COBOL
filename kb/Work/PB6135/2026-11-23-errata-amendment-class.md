# Amendment-applied differences are a class of their own (2026-11-23)

**Finding.** A triage of the 21 LIKELY-ERROR rows on p21, p28, p132, p152, p168, p345 and p474 found no transcription error.
20 sat under a Markdown `> ⚠ **CORRECTED — see the Errata (Cnn).**` note that says an Amendment's text was `applied in place`
(Amendment 1, X3.23a-1989, or Amendment 2, X3.23b-1993): the PDF prints the base standard and the Markdown the amended text.
One was the digit zero in `'0' ... '9'` (p152) read as the letter O by both the text layer and tesseract. The md excerpt of
one row (p132 L5662) had dropped words that had paired off as OCR noise.

**Changes in `scripts/spec/text_layer_diff.py`** (cases 11 to 13 of its self-test).
- AMENDMENT-APPLIED: `amendment_notes` reads, per page, each CORRECTED note that names an Amendment and says `applied in place`
  (a `damaged letter` note is no amendment), as the word keys of its backticked quotes. A hunk whose layer words are a run of
  one such quote (a neighbouring word or three words, never a bag of words) is AMENDMENT-APPLIED; a hunk with no layer words
  qualifies when the Markdown's words are such a run. The class is checked in the page, not by page list.
- OCR-NOISE for a zero read as O: only after the second reading, only when both readers give the letter O, only for a
  digit-only Markdown word (`zero_read_as_o`: a lone `0` beside a digit-only word, or a number of two digits or more).
  The reverse (the Markdown has the letter) and a lone `0` among words stay LIKELY-ERROR.
- A row's `pdf` and `md` excerpts are the whole aligned block, not what is left after words paired off.

**Counts over 976 pages** (`python scripts/spec/text_layer_diff.py --arbitrate`):

| outcome | before | after |
|---|---|---|
| OCR-NOISE | 5269 | 5251 |
| OCR-ARBITRATED | 1344 | 1344 |
| CORRECTION | 823 | 823 |
| AMENDMENT-APPLIED | 0 | 74 |
| FIGURE | 1186 | 1185 |
| PUNCT | 2322 | 2322 |
| CASE | 423 | 423 |
| UNCOVERED | 823 | 811 |
| LIKELY-ERROR | 141 | 88 |
| UNEXPLAINED | 607 | 598 |

74 rows are AMENDMENT-APPLIED; LIKELY-ERROR falls by 53 (the rest of the 74 left UNEXPLAINED and other classes, whose
counts moved by the blocks that no longer split). The 88 remaining LIKELY-ERROR rows are not yet triaged outside the seven pages; the one row on p474 L18510
(`in an indexed file. 1` against the amended words) stays UNEXPLAINED because the layer's footnote digit breaks the run
inside the note's quote.
