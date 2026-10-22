## PB3530: the figure renderer keeps every alternative inside its own brace, and the figure sweep is GREEN

Wave 1150 group C. `python scripts/spec/sweep_figures.py --check` was RED on main: one figure had drifted. The
renderer (`scripts/spec/render_figure.py`) drew §13.18.35.2 Format 1, the report-writer LINE clause, with
`LINE NUMBER IS` outside the three-way brace. PB1221 had corrected that figure by hand, so `--apply` would have
reverted the fix. I rendered the PDF page (folio 420). It prints `{ LINE NUMBER IS / LINE NUMBERS ARE / LINES ARE }`
as one brace.

**The re-probe.** The check failed, but it printed only a count. It now names each drifted figure with its clause,
folio, line range and the diff that `--apply` would write.

**Three mechanisms, one root.** In each, the row layout let a word leave the enclosure it belongs to.

1. *Label snapping.* `outer_label_rows` snapped a row onto a neighbour when the row's words sat left of a brace and
   inside its height. That is the test for an outer enclosure's label (ASSIGN in the file-control entry), but the
   alternatives of a sibling brace pass it too. `LINE NUMBER IS` was lifted onto `integer-1`'s row, above its own
   top hook. A row now snaps only when it is the point row of a brace further out that spans the inner brace from
   hook to hook. The narrowed rule changed 16 figures. The old rule had pulled `PICTURE` out of `{ PICTURE / PIC }`
   and `IS EQUAL TO` out of SEARCH ALL's `{ IS EQUAL TO / IS = }` twice. It had put SET's and USE's labels on a
   hook row instead of the point row, and it had fused INVOKE's `object-class-name-1` and `identifier-1` onto one row.
2. *Clustering across a hook.* Rows are clustered by a 4 pt tolerance, and a span is decided against each row's
   average. `THROUGH` sits 3.9 pt below `identifier-3` and 0.2 pt below its own brace's hook. The clustered row's
   average lay above the hook, so the brace started a row lower and drew `THROUGH` outside itself (EVALUATE
   §14.9.13.2, VALUE §13.18.63.2 Format 5, and CODE-SET's inner brace in the file description entry).
   `enclosures` now pairs every delimiter the way a reader matches parentheses (same family, same height, nearest
   open). `split_rows` splits a row at a boundary only where a word the enclosure holds and the row's average fall
   on opposite sides. A row that only spans the boundary of a group alongside stays whole (UNTIL's `condition-1`).
3. *Stems spanning by height alone.* `covers` enclosed every row inside a stem's slack, including rows of another
   group. WRITE's `| BEFORE / AFTER |` choice bars therefore drew past their brace, through `mnemonic-name-1`. A
   paired stem now spans from the first to the last row that holds its words.

The column-splitting loop in `outer_label_rows` duplicated the one in `build`. Both now call `split_delimiters`.

**The sweep.** 22 figures were regenerated, and I checked each against its printed page. Several replace drawings
that were wrong. `--check` is GREEN with no hand-kept exception. The LINE figure's note and FIGURE-STYLE.md now
describe the current rules.

**Rows.** Twenty FMT rows' rule text changed. Only the layout changed; the words did not, which the sweep's word
gate confirms. Their verdicts were re-affirmed through the `record_verdicts` batch
`w1150c-pb3530-figure-regeneration` (20 rows; GAP 66 → 66). The catalog and the inventory were regenerated. No row
closed.

**The witness.** `scripts/spec/test_render_figure.py` replays six measured figures through `build` without the
licensed PDF, and adds six unit checks of the delimiter splitter, the pairing and the row split. Main's renderer
fails the three cases for the new mechanisms and passes the three guard cases (ASSIGN, UNTIL, ACCEPT Format 3).

**Self-review.** I checked for a second copy of the pairing or span rule (none), a split that could fail to end
(each split makes both parts agree with the boundary that caused it), and the effect on
RequiredFormatPhraseDriftTests' scrape of §14.9 figures (its rows are unchanged). The semgrep counts did not change.
