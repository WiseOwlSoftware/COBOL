## PB4849: the 1985 figure refuter step continued, and the batch's drawing redrawn in the house style

Wave 20261902 group A continued kb/Work PB4849's step 1: decide the 1985 `<pre>` blocks awaiting a verdict from the
printed page.

**Re-probe.** `sweep_figures.py --edition 1985 --cross-check` on main: 232 agree, 33 disagree, 196 unpaired, 40
unlocated; `--check`: 167 blocks awaited a verdict.

**The mechanism: `figure_reading.restyle`.** A refuter who finds the batch's drawing right must still write the figure
by FIGURE-STYLE.md, and the batch drew with the Miscellaneous Technical pieces rule 2 forbids and with two-row groups
rule 5 forbids. Every earlier refuter redrew such figures by hand, so only one-line figures had been decided `drawn`.
`restyle` redraws the batch's drawing, words kept in their rows and columns: rule 3's pieces, a two-row group grown to
three rows (a brace's point on the new row), a brace drawn without its point given one, a corner on one row typed
(rule 7), the ellipsis `…`, and each delimiter set one space clear where no other row's word is in the way. It asserts
that `compare` finds nothing between the drawing and the redrawing. It refuses a box drawn as a full rectangle, the
batch's arrows, and (strict) a drawing whose multi-row piece pairs with nothing or whose corner sits beside its partner
one column off: the `<u>` tags hide one-column slips, and 63 of the 501 blocks carry one. `sweep_figures.load_verdicts`
now refuses a verdict whose figure carries a forbidden piece, and a `drawn` verdict whose figure reads differently from
the block it decides, so a `drawn` verdict is the batch's drawing by construction.

**Transcription repairs.** Pages 156, 180 and 599 carried their one general format twice, a copy set above the running
head; the printed pages print it once, and the copies were removed.

**The refuter step.** 79 verdicts in `docs/rearchitecture/spec-reconciliation/figures-1985/`, each decided from the
rendered page and written with `--apply`. The 1985 Sequential, Relative and Indexed I-O and Inter-Program Communication
modules box nearly every level-2 element (one word, one ellipsis, a clause, a whole format with its label, or the
page's outline round a whole statement), and the batch lost every box, so most are `neither`: the batch's drawing with
the box's two sides added. 85 blocks still await a verdict, most of them the composite skeleton (pages 195-232).

**Self-review.** One mechanism: `restyle` builds on `figure_reading`'s own column reader and `compare`, and adds no
second reader. The guards live in `load_verdicts`, the one loader. `restyle` runs only on a refuter's request.

**Gate.** See the wave report for the verdict line.
