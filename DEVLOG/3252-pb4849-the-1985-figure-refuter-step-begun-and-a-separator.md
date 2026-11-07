## Entry 3252 — 2026-11-07 07:17 PST — PB4849: the 1985 figure refuter step begun, and a separator the underline does not span is plain

Wave 20261901 group A took kb/Work PB4849's step 1 first, as the note's re-ordering asks: decide the 1985 `<pre>`
blocks awaiting a verdict from the printed page, rather than tune the measurer.

**Re-probe.** `sweep_figures.py --edition 1985 --cross-check` on main: 184 agree, 82 disagree, 195 unpaired, 40
unlocated; 352 blocks awaited a verdict.

**The refuter step.** Each block was read from the rendered PDF page (a crop of its band, or the page) beside the
batch's drawing and the regeneration, and decided by one verdict file in
`docs/rearchitecture/spec-reconciliation/figures-1985/`. The classes, so the next refuter can take them in bulk:
COBOL examples, diagrams, amendment table entries, the glossary and page furniture are `not-a-format`; a level-2 box the
batch lost, checked on the page and against the box pass, is `generated`; the typeset intrinsic-function formats
(pages 863-904), which the measurer cannot pair, are `drawn`; a page-wide box drawn by the measurer as a choice bar, a
box around one word or delimiter, and a format no reading located are `neither`, drawn by hand from the page by
FIGURE-STYLE.md rule 9 and checked with `figure_reading.compare` against both drawings. The counts are in the wave
report.

**The measurer, one class at its root.** Twelve blocks disagreed only because a trailing period or colon was read as
underlined. The 1985 standard is typewritten, an underline struck one underscore per character cell, so an underlined
separator has the rule running wholly under it. `figure_raster._trim_separators` takes a separator (`.` `,` `;` `:`)
off either end of an underlined word unless the rule spans its glyph, whether or not the word's components pair with
its letters: the rule under `DECLARATIVES.` (page 182) stops short of the period, and the one under `I-O-CONTROL.`
(page 380) overruns the L's foot into it. `test_figure_raster.py` case 10k plants both, and the spanned and
no-separator cases that keep the rule.

**Gate.** See the wave report for the verdict line.

