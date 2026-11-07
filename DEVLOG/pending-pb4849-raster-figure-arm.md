## PB4849: the 1985 general formats regenerated from the scan, their level-2 boxes drawn, and the batch's drawing cross-checked

**What the note asked.** Regenerate every 1985 general format from the measured page, compare it with the batch's
drawn figure on the four things a format carries (grouping, choice bars, underlines, alignment), send each
disagreement to a refuter, write the decided figures and run the four figure audits. This landing carries two
mechanisms: the raster arm of the figure pipeline with the cross-check (wave 20261890), and the level-2 boxes (wave
20261891, at the end). The refuter run, writing the figures and the audits' raster arms remain open on PB4849.

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

**Found on the way.** `sweep_figures.py --check` on the 2023 transcription reports 2 drifted figures (§2023:14.9.9.2 and
§2023:14.9.28.2 Format 3) at the base commit too: a lead in the wave report, not this mechanism.

**Tests.** `test_figure_raster.py` cases 10a-10f (planted: dotted letters, ellipsis, typed delimiters, half-line
alternatives, delimiter order, an uphill line, two figures sharing words, the assignment); `figure_reading.py
--self-test` (13 cases); `sweep_figures.py --self-test` (`drawn_figures`). The twelve calibration crops still measure
as counted by eye.

**The level-2 boxes (wave 20261891).** The 1985 standard boxes syntax unique to level 2 of a module (PDF page 23).
The re-probe on this build: `find_boxes` measured them (page 389: `[RECORD]` and the MULTIPLE FILE TAPE line), but
`render_figure` drew none and the batch's drawing had lost them, so the cross-check could not see a box at all.
FIGURE-STYLE.md rule 9 now draws a box as a fourth family in DOUBLE lines, its two sides only (`╔ ║ ╚` / `╗ ║ ╝`, and
`║ … ║` one row tall): a top or bottom rule would need a row that is no printed line, and a box set inside a line has
none. `figure_raster.classify_page` gives each box as two stems of kind `box`, which `render_figure.build` pairs, seats
and draws as it does a bracket's, in a column of its own; a box is no group, so it adds and removes no blank row.
`figure_reading` reads a box from either drawing (these sides, or the batch's full double-line rectangle) and `compare`
names a fifth thing, `boxes`; `sweep_figures.regenerated` names a measured box the drawing lost because its words
stayed unread. On the way, `find_boxes` read an underline fused to a descender (page 222) and one side of a drawn
diagram's frame (page 353) as boxes 1.3 pt tall and 1.4 pt wide and erased their ink; it now requires the rectangle
its rules enclose to be a box's size, and page 222's stamp-free crop is a calibration case that fails without that.
The batch drew some boxes as brackets (page 662's `┌DESTINATION TABLE …┐`, dropping a typed `[`): those figures stay
unpaired for the refuter.

**The run again** (all 976 pages, 501 drawn figures, 104 boxes reaching a figure): 181 agree, 68 disagree (27 of them
on the boxes alone, the batch having dropped them), 212 unpaired, 40 unlocated; 5 figures name a box lost with its
words. Tests: `test_render_figure.py` 21/21 (two figures measured from pages 389 and 303, and a box-pairing check),
`figure_reading.py --self-test` 17/17, `test_figure_raster.py` 52/52 (13 crops).
