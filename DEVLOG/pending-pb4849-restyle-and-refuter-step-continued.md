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

**The refuter step finished (wave 20261903 group A).** The cherry-picked branch gated GREEN first (run
000001-20261107T164055Z). The cross-check on that head: 85 blocks awaiting a verdict. Every one was decided from the
rendered page, and `--apply` then reported `awaiting a verdict: 0` (92 figures written). This wave wrote or changed 91
verdict files: 48 `drawn` and 43 `neither`, including the four renderer corrections below. How they were decided:
- **Drawn where the batch was right:** `figure_reading.restyle` redrew the batch's drawing, and
  `load_verdicts` held each to it.
- **Drawn from an equal decided figure** where the batch's columns were off: the composite skeleton (pages 195-232)
  reprints the module formats. The decided module figure was reused only after the rendered skeleton page showed the
  same words and grouping, and with its level-2 boxes removed, because the skeleton draws none. These cover SPECIAL-NAMES
  (4.5.2), the data description entry (IPC 4.3.2), FD (IPC 4.2.2), RD (3.5.2), report group Format 1 (3.9.2),
  EVALUATE (6.13.2), UNSTRING (6.27.2), CALL (page 213), I-O-CONTROL (2.10.2) and SELECT (page 197).
- **Hand-drawn from the page** (`draw.py` specs): PERFORM VARYING, SORT and MERGE, the USE formats, SEND Format 2 (with
  its three one-row boxes), COPY with its stepped level-2 outline drawn as two boxes, and the CD formats of pages 661-662,
  whose full `╔═╗` rectangles were redrawn by rule 9.
- **As printed in typed delimiters**: the nested-program skeletons (pages 231-232) and the Communication Section
  (page 660) open a typed `[` or `{` on one row and close it on a later one. The batch had redrawn these as multi-row
  groups the page does not draw.

**Transcription repairs.** The batch set three pages' formats inside one `<pre>` each, together with the running head
and page title: page 220 (six formats), page 225 (S and RI WRITE) and page 226 (COPY, REPLACE, REPLACE OFF). Each was
split into one block per format, with the furniture moved outside the block. Page 678 carried its ENABLE format twice,
a copy above the running head, and the copy was removed (its decided figure renumbered `p678-0`). Page 217's `BEFCRE`
is a misprint on the page and is transcribed as printed.

**A renderer defect, swept.** The decided report group Format 1 (page 602) drew the TYPE brace's left point a row
above its right point and its label. A sweep of every written figure for a brace whose two points disagree
(`points.py`) found three more: pages 407, 426 and 559. In each, the scan's measured middle pieces of the opening and
closing piece disagree by a row, and `render_figure.build` places each piece's point independently. Page 407's outer
closing brace also ran one row short. All four figures were corrected from the page, as `neither` verdicts (559 had
no verdict until now). The renderer root cause is filed as a lead in the wave report.

**Gate.** Run 000002-20261107T175931Z: `=== BUILD-LOCAL GATE: GREEN — Conformance 22,463/22,463 · Unit 40,427/40,427 ·
Characterization 37/37 cases ran (skipped 0)`.
