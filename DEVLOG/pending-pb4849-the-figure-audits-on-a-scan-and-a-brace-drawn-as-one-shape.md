## PB4849: the figure audits on a scan, and a brace drawn as one shape

Wave 20261907 group A finished kb/Work PB4849, the 1985 general formats regenerated and cross-checked.

**Re-probe.** On main (8a5526604a) `sweep_figures.py --edition 1985 --check` was clean (505 blocks, 0 awaiting a
verdict). Each of the four figure audits refused `--edition 1985` with "this tool has no raster arm yet", so the note's
"Done when" (the four audits GREEN) was unmet.

**Step 3, as the note decided it.** The three vector audits (`audit_underlining`, `audit_figure_structure`,
`audit_figure_text`) stay vector-only: on a scan they would duplicate what `--check` already proves against the
measured page. `spec_pdf.scan_check` names that check, and `require_vector` now refuses a scan by naming it and
`render_figure.py <page> --edition E`. `audit_figure_text` refuses a scan too. It reads the text layer
`figure_extract.extract` reads, and until now it had halted on page anchors first. The drift test is in
`sweep_figures.self_test`: every scanned edition's `scan_check` runs the raster sweep's `--check`, and that check fails
on any block not in its decided form.

The fourth audit, `audit_grammar_optional_words`, checks the grammar against the figures' optional words, a job no
other check does. On a scan it now reads the transcription's decided drawings. Its population is a `Population`
(`printed_population` for a vector PDF, `drawn_population` for a scan). `drawn_formats` groups each general-format
`<pre>` under its `General Format` heading and anchors it by the 1985 title (`THE READ STATEMENT`). The drawings are read
by `figure_options.read_drawn`, which shares one holder-and-ellipsis reading (`_held_words`) with the measured page's
`read`; its self-test proves the LOCK MODE and COMPUTE fixtures read the same drawn as measured. Three things the 2023
run never met:
- the 1985 REWRITE and WRITE print `record-name-1` only, and the grammar's `(recordName | FILE fileName)` counted FILE
  as demanded, so no rule anchored them. A fourth anchoring, `choosing` (`Spelling.certain`: of a required choice, only
  what every arm writes), is tried last. Tried first, it anchored the VALUE clause's `(VALID | INVALID) (WHEN
  condition)?` to the 2023 INVALID clause;
- a lowercase metavariable (`[ length ]`, PDF page 168) read as prose and emptied a format; a decided drawing holds no
  prose, so `read_drawn` and `measure` skip that filter for it;
- the `audit:<kind>` markers decide 2023 candidates, so a marker can be dead only in the default edition's full run.
`--edition 1985`: population OK, 213 formats, 36 candidates, each a grammar spelling that may reject legal 1985 source
(the wave report lists them for the registrar). The 2023 run's output is byte-identical but for the new `choosing: 0`.

**A brace drawn as one shape.** Wave 20261903 hand-corrected four figures (pages 407, 426, 559, 602) whose brace
pieces were drawn with their points a row apart. `render_figure.build` placed each glyph piece on its own measurement.
`pair_glyphs` now pairs an opening piece with its closing one as `enclosures` does. The pair gets the union of the two
pieces' rows and one point row: the point they share, else the one on the row of the label left of the opener, else
the opener's. Pages 426, 559 and 602 now regenerate exactly as decided. `--check` then named three agreed figures
whose closing brace had been drawn short (pages 203, 222 and 628), and `--apply` rewrote them. The 2023 `--check` is
unchanged; its two drifts are PB4913's, present on main.

**Self-review.** One reader per job: `read_drawn` adds no second holder reading, and `pair_glyphs` uses the same pairing
rule as `enclosures`. The scan's coverage claim is pinned by a drift test, not prose. Performance: the 1985 audit runs
in seconds, and `pair_glyphs` is linear in a figure's glyph pieces.

**PB4850, begun.** With PB4849 landed, the 1985 transcription's publishing gates were run. `lint_rendering.py
--edition 1985` found 205 rendering defects, and now finds none. The fixes: 107 rule labels escaped, 66 bare `<pre>`
blocks (examples and tables) opened with `line-height:1`, and 31 literal asterisks escaped. The composite skeleton's
module marks (`S`, `SW`, `RI`, PDF pages 213 and 225) had been drawn as `<i>` tags, one escaped so that it rendered as
literal text; they are now plain text, and their four verdicts are updated. `sweep_figures.py --edition 1985 --check`
stays clean. `docs/DOC_INDEX.md` gains the transcription's row, and `docs/VERSION_CHANGE_REFERENCE.md` says the repo
now holds the 1985 text. Still open (PB4850): the page-by-page reconciliation, the Preface and acknowledgment (both
gates exit naming their tables, which have no 1985 row), and the check of the reference's 1985 claims.
