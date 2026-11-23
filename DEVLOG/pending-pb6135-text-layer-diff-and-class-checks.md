## The 1985 transcription gets a PDF text-layer diff (PB6135)

PB6051's closing condition moved from another reading pass to mechanical checks (PB6135). This change builds the second one:
`scripts/spec/text_layer_diff.py` aligns each page's PDF text-layer words with the Markdown page on letters and digits only,
explains a difference as OCR-NOISE only when the PDF word is the damaged side and the Markdown word the well-formed one
(known and not malformed), as CORRECTION only when one ledger `was`/`now` pair or note of that page holds that hunk, keeps
punctuation (PUNCT) and case (CASE) as classes of their own, cancels moved words only as a run of two or more, sets figures
apart, and writes `docs/rearchitecture/spec-reconciliation/TEXT-LAYER-DIFF-1985.json` with the SHA-256 of the Markdown, the
ledger and the script. The gate's TEXT LAYER DIFF 1985 audit is RED when one changed since (`--check`, no PDF). Over 976
pages, before the second reading: 5269 OCR-NOISE, 823 CORRECTION, 1186 FIGURE, 2322 PUNCT, 423 CASE, 823 UNCOVERED, 2092 UNEXPLAINED on 528 pages; the
first version's 128 unexplained hid the rest behind folds that a round-1 review showed excuse real errors. A second, independent
reading by tesseract (`--arbitrate`, positional, cached outside the tree) then settles the unexplained rows: 1344 become
OCR-ARBITRATED, 141 LIKELY-ERROR (both readings differ from the Markdown; the list is `--check --likely`, see kb/Work PB6135
addendum), 607 stay UNEXPLAINED. The three
reading-only classes (table indentation, brace continuation, a boxed figure ending a page) were measured and not gated: the
layer gives no precise rule (kb/Work PB6135 addendum). The transcription is unchanged; PB6051 and PB6135 stay open.

A triage of the 21 LIKELY-ERROR rows on seven pages found no transcription error: 20 sit under an Amendment's in-place
CORRECTED note. So a hunk is now AMENDMENT-APPLIED only when the layer's words are a run of the note's printed quote AND the
Markdown's words are a run of the amended text (the note's quote, or the text layer of the one entry of the Amendment page that the note names; an entry not located gives nothing and is counted); a zero
read as the letter O is OCR-NOISE only inside a number of two digits or more; a row's excerpt is the whole aligned block.
Re-measured: LIKELY-ERROR 141 to 126, UNEXPLAINED 607 to 602, AMENDMENT-APPLIED 0 to 57, 93 amendment entries not located (kb/Work PB6135
2026-11-23-errata-amendment-class). The list of LIKELY-ERROR rows is `text_layer_diff.py --check --likely`, not copied into a note.
