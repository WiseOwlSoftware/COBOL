## PB4917 and PB4813: a refusal is never a page, and the 2002 text-layer backend

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
pages, so that run, the figures and the assembly continue in a later wave. No gate ran (the stop forbids starting one).
