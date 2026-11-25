## Entry 4061 — 2026-11-24 22:36 PST — PB6135: the text-layer diff weighs UNCOVERED rows by the second reading

`scripts/spec/text_layer_diff.py` sent only UNEXPLAINED rows to tesseract; the 807 UNCOVERED rows (Markdown words the 1985
PDF's text layer lacks) had no witness, so a Markdown insertion could hide there. `classify_page` now hands UNCOVERED rows their
block, and `arbitrate_rows` weighs them by the same positional rule: tesseract reading the words at the place makes the row
OCR-ARBITRATED, tesseract's neighbours touching without them make it LIKELY-ERROR, anything else keeps it UNCOVERED.

Measured over all 976 pages (`--arbitrate`, committed JSON): UNCOVERED 807 to 284, OCR-ARBITRATED 1344 to 1757, LIKELY-ERROR
88 to 198, UNEXPLAINED 554 unchanged; rows a reader opens 1449 to 1036, pages 445 to 336. The new LIKELY-ERROR rows are
mostly Amendment text applied in place (the class the 59-row verdict found, no Markdown error) and module tables read by
column; they join the PB6051 page review. Self-test case 20; finding `kb/Work/PB6135/2026-11-24-uncovered-arbitrated.md`.

