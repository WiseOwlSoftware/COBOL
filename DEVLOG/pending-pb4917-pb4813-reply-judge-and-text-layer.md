## PB4917 and PB4813: a refusal is never a page, and ISO/IEC 1989:2002 transcribed from its text layer

**PB4917.** `transcribe_pdf.py collect` wrote any reply whose `stop_reason` was `end_turn`, so the 2002 canary's four
"I can't transcribe this page verbatim ..." replies, each followed by a summary, were written as pages with `bad: []`.
Re-probed with the fake client: reproduced. `transcribe_pdf.judge(text, layer, edition)` is now the one test of a reply.
It has a refusal shape (a first-person declining of the task, a first-person summary or omission, a sentence naming a
summary of the page) and, where the page's text layer holds at least 30 words, a word-agreement floor of 0.5. `collect`
and `pages --local` ask it before a page is written. A refused batch reply is recorded in `state.json`'s `failed` (reason,
model, batch, time), still counted in the cost, listed by `status` and resubmitted by `retry`; `assemble` uses the refusal
arm. A bare "In summary" is the standard's own prose (1985 page 93, 2023 Annex D), so it is not a shape. The sweep of the
1985 run's 976 pages refuses one, page 833, whose OCR layer is mangled table codes: a false hit, the transcription is right.

**PB4813 (in progress).** The owner chose the text-layer route on 2026-11-07. `transcribe_pdf.py text` is a third backend
beside the batch and local ones, implemented in `scripts/spec/text_layer.py`. It reads words from per-glyph positions,
drops the furniture and the stamp, takes headings, rules, notes, lists, tables and code from fonts and positions, and leaves
every general format as a placeholder for the vector arm, which the assembly refuses until it is drawn. 2002's folio
offset (22) is recorded in `spec_pdf.EDITIONS`. Self-tests: `test_text_layer.py` (planted layouts, and a PDF it writes when
PyMuPDF is present) and new cases in `test_transcribe_pdf.py`. The wave's stop file arrived before the run over the 882
pages, so that run, the figures and the assembly continue in a later wave.

**Replayed onto main (wave 20261918).** The two commits were cherry-picked onto a main whose batch state had moved into
`message_batches.BatchRun` (kb/Work PB4886). `collect_batches` now hands `BatchRun.collect` a `take` that asks `judge`
and records a refused page in `failed`, so the one batch loop serves both batch tools; usage is keyed by batch and custom
id there, and the self-test checks that key. `spec_pdf.EDITIONS[2002]` keeps main's body-anchor field with the measured
offset 22. `docs/DOC_INDEX.md`'s converter row now names the `text` backend and `judge`. The first gate was RED on `spec_headings.py --self-test`: `text_layer.py` wrote its own General format heading regex, which also missed the "General forms" spelling; it now asks `spec_headings.GENERAL_FORMAT_TITLE` (kb/Work PB284). The re-gate: `BUILD-LOCAL GATE: GREEN — Conformance 22,616/22,616 · Unit 40,486/40,486 · Characterization 37/37` (run 000002-20261108T033558Z).

**The vector arm reads 2002, and the transcription lands (wave 20261923).** Replayed again onto main b89815ed59 (gate
000001-20261108T092826Z GREEN). The 2023 figure pipeline drew nothing from a 2002 page, and the probe named five reasons:
`render_figure.headings` wanted an 11 pt heading (2002's are 10 pt over a 9 pt body, so a heading is now larger than the
page's regular body text); 2002 decodes a brace's Symbol pieces as Adobe private-use code points, which `GLYPH_PIECES`
(moved to `figure_extract`, where the text layer is read) now carries; 2002 strokes its underlines, brackets and feet as
lines, many to one path, so `figure_geometry.strokes` measures each item and a 2.7 pt foot counts; the Symbol face's taller
ascent lifted `{`, `}` and the raised ellipsis onto the row above (`figure_extract.level_top` gives such a word the top of
the text face on its baseline); and a word split off a brace hook kept the hook's underline state. The 2023 oracle
(`sweep_figures --check`) held still except for five figures, each a trailing ellipsis that sat on the row above its printed
line in the 2023 transcription too (7.3.13.2, 12.3.7.2, 13.18.54.2, 14.9.26.2, 14.9.43.2, each checked against the rendered
page), now applied; PB4913's two drifts are unchanged. The text backend now draws each figure it places through
`render_figure.draw`, reads a General format clause's prose as text (`classify_row`), keeps a Courier `Format 1` as a
label, joins NUMVAL's out-of-clause notation rows to their figure, splits a found table where no vertical rule runs
(folio 802's two INSPECT tables had been one, the code between them a cell) and finds two columns only across a 10 pt
gutter (the old test split the contents and 26 other single-column pages). With `PARTS[2002]`, the Preface (the
permission stated, no person named; 2002 prints no acknowledgment) and the 2002 rows of `verify_publishable` and
`sweep_figures`, `specs/ISO_COBOL_2002.md` is assembled: 882 pages, 450 general formats all drawn, PUBLISHABLE, LINT
CLEAN, `sweep_figures --edition 2002 --check` clean with 439 of 445 located figures byte-identical, and `cite.py` resolves
`§2002:` citations against it. The page-by-page reconciliation and the refuter pass over the figures remain, in PB4813.
The 2023 figure moves re-digested four rule-catalog rows (verdicts unchanged), and the citation audit, which now measures
2002 citations against the 2002 text, caught its own planted twin citing a 2002 clause the edition lacks (now
`§2002:14.8.12.2 SR6`). Three gates were RED on audits (the stale catalog, that citation, a comment that spelled the
General format heading beside a `re.compile`); the fourth: `BUILD-LOCAL GATE: GREEN — Conformance 22,656/22,656 · Unit
40,494/40,494 · Characterization 37/37` (run 000005-20261108T110430Z).
