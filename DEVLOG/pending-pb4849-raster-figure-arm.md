## PB4849 (part 1): the 1985 general formats regenerated from the scan and cross-checked against the batch's drawing

**What the note asked.** Regenerate every 1985 general format from the measured page, compare it with the batch's
drawn figure on the four things a format carries (grouping, choice bars, underlines, alignment), send each
disagreement to a refuter, write the decided figures and run the four figure audits. This landing is the first
mechanism: the raster arm of the figure pipeline and the cross-check. The refuter run, the box glyph rule, writing
the figures and the audits' raster arms remain open on PB4849.

**Re-probe.** The raster arm `figure_geometry.page_classifier(1985)` handed a PyMuPDF page to
`figure_raster.measure_page`, which takes a page number, so it could not have run; `figure_extract`, `render_figure`
and `sweep_figures` refused 1985 (`require_vector`). Measuring a whole 1985 page (PDF page 196, 305) showed why the
calibration crops were not enough: the PDF's OCR layer misreads figure words (`{iLEerT:` for `[END OF]`), nothing is
bold, and the measured words were split wrong (an i's dot a word of its own, an ellipsis three words, `[ON` one word,
INPUT-OUTPUT split across two lines by a speck above it).

**The fix.** A scan's figure is found and read by its WORDS, which the batch's drawing supplies:
`figure_raster.measure_figures` measures the page once, `locate` places the page's figures together, in order, as
disjoint runs of printed lines whose OCR words match each figure's (the OCR text is a position hint only), and `align`
pairs every drawn word with its ink by one assignment over the figure (`hungarian`, numpy only), words first and typed
delimiters second by the words beside them. Anything it cannot pair is a disagreement and the word stays unread.
`find_words` now joins an i's dot to its letter, keeps an ellipsis one word, splits a typed delimiter off its word and
groups lines by the letters nearest each glyph (a typed line runs uphill on page 310); the skew is measured clear of
the per-copy stamp. `figure_extract.page_extractor` and `figure_geometry.page_classifier` are the one dispatch on
`spec_pdf.geometry`. `figure_reading.py` (new) reads either drawing into words with their holders and underlined
letters and compares two drawings per thing. `sweep_figures.py --edition 1985 --cross-check --ledger F` gives every
drawn figure a verdict; `render_figure.py <page> --edition 1985` prints a page's regenerated figures for a refuter.

**The run** (all 976 pages, 501 drawn figures): 207 agree on all four things, 42 disagree (grouping 158, bars 29,
underlines 89, alignment 30 counted over every figure), 212 have a word the pairing could not read, 40 were not located.
The ledger is in the wave's scratch directory; the refuters take the 294 that do not agree.

**Found on the way.** `sweep_figures.py --check` on the 2023 transcription reports 2 drifted figures (§14.9.9.2 and
§14.9.28.2 Format 3) at the base commit too: a lead in the wave report, not this mechanism.

**Tests.** `test_figure_raster.py` cases 10a-10f (planted: dotted letters, ellipsis, typed delimiters, half-line
alternatives, delimiter order, an uphill line, two figures sharing words, the assignment); `figure_reading.py
--self-test` (13 cases); `sweep_figures.py --self-test` (`drawn_figures`). The twelve calibration crops still measure
as counted by eye.
