## PB4849: the agreed 1985 general formats written in the house style, and the refuters' verdict files read

**What the note asked, and this slice.** PB4849 regenerates every 1985 general format from the scan, compares it with
the batch's drawing, sends each disagreement to a refuter and writes the decided figures. Earlier waves landed the
raster arm, the cross-check and the level-2 boxes. This one writes the figures: `sweep_figures.py --edition 1985
--report | --apply | --check` now run, driven by `decide`. The refuter fleet, the local witness as its third reading,
and the audits' raster arms stay open on the note.

**The write rule.** A `<pre>` block is written in the house style only when it is decided: a refuter's verdict file
decides it (`docs/rearchitecture/spec-reconciliation/figures-1985/p<page>-<figure>.json`, read by `load_verdicts`,
which refuses a malformed file, two verdicts on one block, a decision outside generated / drawn / neither /
not-a-format, a missing figure, reason or `drawn_lines`, and a verdict whose block moved), or the block is a general format whose
words were all read and whose two drawings agree on every word, the four things and the boxes. `--check` is clean when
every block is in its decided form, none awaits a verdict and every verdict names a block.

**What the first apply showed.** Keyed on agreement alone, it rewrote 181 blocks, and the re-check found two defects.
(1) The batch set COBOL examples, flow diagrams and tables in `<pre>` (the table definitions of PDF pages 96-100, the
appendix examples from page 763). Their two drawings agree word for word, and the regeneration flattened their
indentation and merged a two-column example into one. `Drawn.general_format` now says whether a block sits where a
general format is printed (under a heading or after a sentence that names one, `spec_headings.GENERAL_FORMAT_NAMED`, or
with a `Format N` label). No agreement rewrites any other block; it waits for a `not-a-format` verdict. (2) 67 of the
written figures no longer paired when regenerated from their own house-style drawing. The house `…` was paired as one
glyph against the page's three dots, so `figure_reading.token_rows` now reads it as `...`. The OCR layer reads `]}` as
`])`, so `figure_raster._Frame.cost` takes a hint of any delimiter of the typed delimiter's side (`DELIM_SIDE`). On
the batch drawings that change also fully paired two figures that had been unpaired (pages 201 and 204). A trial that
removed the house padding inside typed delimiters was reverted: the batch draws the page's own spaces (`} ... ] ...`,
page 592), and it broke that figure.

**A blind spot in the comparison.** Page 522 is one page-wide level-2 box. Its left side, measured alone beside the
figure, was drawn as a lone `│` that holds no word, so `compare` reported agreement. `figure_reading.strays` now
names every delimiter that pairs with nothing, and `compare` reports a stroke the generated drawing leaves unpaired
beyond those the drawn one leaves. A typed bracket set across two lines is unpaired in both, so it is no difference.

**Review leads R1-1 and R1-2 (wave 20261893).** `render_figure.build`'s two-row spacer counted a level-2 box stem as a
group, so a box exactly two lines tall gained a blank row (FIGURE-STYLE.md rule 9). The witness is
`TWO_ROW_BOX_ADDS_NO_ROW`, RED on the old renderer. `figure_reading`'s self-test hard-coded 17 for 16 checks and now
counts the checks it runs.

**Tests.** `sweep_figures --self-test` (18 checks: block context, ordinals, every `decide` arm, every
`load_verdicts` refusal), `figure_reading --self-test` (the house ellipsis read as printed, the spaces kept, strays),
`test_figure_raster` 10g (an OCR `)` supports `}`), `test_render_figure` (22). No compiler behaviour changed, so there
are no goldens and no inventory rows.

**Result.** Before the write, the cross-check over all 501 `<pre>` blocks gave 178 agree, 73 disagree, 210 unpaired
and 40 unlocated. `--apply` wrote 148 general formats in the house style. The 30 other agreeing blocks are not where a
general format is printed, so they were left as drawn. The re-run `--check` finds all 148 in their decided form and
none drifted. 353 blocks await a verdict: 277 general formats and 76 other blocks. The local build gate ran on this
head; its verdict line is in the wave report.

**Review round 1 (wave 20261897 A finisher).** One blocking finding, R1-1: `decide` guarded a verdict against its
block having moved by comparing only the clause and label, but the blocks of a page usually share both, so removing
an earlier `<pre>` (the furniture cleanup this entry's report proposes) shifted a verdict onto the next block without
the FATAL firing. A verdict now carries `drawn_lines`, the block's lines as drawn (the cross-check ledger's new
`drawn`), and `decide` accepts the block its ordinal names only when it holds those lines or the decided figure. The
clause and label left the verdict format. The self-test plants the review's page 202 shape (23/23).
