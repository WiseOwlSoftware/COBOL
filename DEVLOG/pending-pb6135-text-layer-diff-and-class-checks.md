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
OCR-ARBITRATED, 141 LIKELY-ERROR (both readings differ from the Markdown; the full list is in the kb/Work PB6135
addendum), 607 stay UNEXPLAINED. The three
reading-only classes (table indentation, brace continuation, a boxed figure ending a page) were measured and not gated: the
layer gives no precise rule (kb/Work PB6135 addendum). The transcription is unchanged; PB6051 and PB6135 stay open.
