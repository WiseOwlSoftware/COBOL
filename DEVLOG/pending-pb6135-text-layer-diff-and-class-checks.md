## The 1985 transcription gets a PDF text-layer diff (PB6135)

PB6051's closing condition moved from another reading pass to mechanical checks (PB6135). This change builds the second one:
`scripts/spec/text_layer_diff.py` aligns each page's PDF text-layer words with the Markdown page, folds the OCR confusions
(l/1/I, O/0, rn/m, Z/2, S/5, ligatures, line-end hyphens, reading order), explains differences from the ledger and the
correction notes, sets figures apart, and writes `docs/rearchitecture/spec-reconciliation/TEXT-LAYER-DIFF-1985.json`
(`--check` reads it with no PDF). Over 976 pages: 2272 OCR-NOISE, 647 CORRECTION, 818 FIGURE, 667 UNCOVERED (Markdown words
the layer lacks), 128 UNEXPLAINED on 87 pages. The three reading-only classes (table indentation, brace continuation, a
boxed figure ending a page) were measured and not gated: the layer gives no precise rule (kb/Work PB6135 addendum). The
transcription is unchanged; PB6051 and PB6135 stay open.
