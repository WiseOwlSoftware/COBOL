## Entry 2371 — 2026-10-22 16:28 PDT — PB3530: the figure renderer keeps every alternative inside its own brace, and the figure sweep is GREEN

Wave 1150 group C. `python scripts/spec/sweep_figures.py --check` was RED on main: one figure had drifted. The
renderer (`scripts/spec/render_figure.py`) drew §13.18.35.2 Format 1, the report-writer LINE clause, with
`LINE NUMBER IS` outside the three-way brace. PB1221 had corrected that figure by hand, so `--apply` would have
reverted the fix. I rendered the PDF page (folio 420). It prints `{ LINE NUMBER IS / LINE NUMBERS ARE / LINES ARE }`
as one brace.

**The re-probe.** The check failed, but it printed only a count. It now names each drifted figure with its clause,
folio, line range and the diff that `--apply` would write.

**Four mechanisms, one root.** In each, the drawing put a word or a delimiter outside the enclosure it belongs to.

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
   open), and `place_rows` seats each row inside every enclosure that holds one of its words (see the review below).
   A row that only spans the boundary of a group alongside stays where it is (UNTIL's `condition-1`).
3. *Stems spanning by height alone.* `covers` enclosed every row inside a stem's slack, including rows of another
   group. WRITE's `| BEFORE / AFTER |` choice bars therefore drew past their brace, through `mnemonic-name-1`. A
   paired stem now spans from the first to the last row that holds its words.

4. *A choice bar read as a bracket* (the sibling sweep). Across the standard the pairing left 7 of 1,733
   delimiters unpaired. Four were SEND's choice bars (§14.9.38.2, folio 726). Each stands 0.70 pt clear of the
   bracket's foot, and that foot's end fell within `figure_geometry._foot_side`'s 1.2 pt tolerance of the bar's far
   edge, so both SEND formats drew `[ ] ON EXCEPTION … [ ]`. A foot now belongs to a stem only when its near end
   lies inside that stem's stroke. Over the whole standard this changes those four bars and no other general-format
   stem. The other three unpaired delimiters are one-sided brackets that the print continues across a page, and
   they keep the height-based span.

The column-splitting loop in `outer_label_rows` duplicated the one in `build`. Both now call `split_delimiters`.

**The sweep.** 24 figures were regenerated, and I checked each against its printed page. Several replace drawings
that were wrong. `--check` is GREEN with no hand-kept exception. The LINE figure's note and FIGURE-STYLE.md now
describe the current rules.

**Rows.** Twenty-one FMT rows' rule text changed. Only the layout changed; the words did not, which the sweep's word
gate confirms. Their verdicts were re-affirmed through the `record_verdicts` batches
`w1150c-pb3530-figure-regeneration` (20 rows) and `w1150c-pb3530-send-choice-bars` (1 row); GAP stays 66 → 66. The
catalog and the inventory were regenerated. No row closed.

**The witnesses.** `scripts/spec/test_render_figure.py` replays six measured figures through `build` without the
licensed PDF, and adds six unit checks of the delimiter splitter, the pairing and the row split. Main's renderer
fails the three cases for the new mechanisms and passes the three guard cases (ASSIGN, UNTIL, ACCEPT Format 3).
`scripts/spec/test_figure_geometry.py` holds SEND's four measured stems. Main reads both bars as brackets.

**Review round 1 (R1-1) and its fix.** The first cut split a straddling row by checking every word's height
against the boundary, including words outside the enclosure. On folio 342 `CODE-SET`, `.` and `FOR ALPHANUMERIC
IS alphabet-name-1` are one printed line, level with the outer brace's point. The inner `{ | FOR … | }` brace's
boundary moved `FOR …` down and left `CODE-SET` a row above the point it labels. On folio 441 it tore
`IS character-string-1` from `EDITING character-1`. A probe over the whole standard found four torn rows, and only
two of them (EVALUATE's and VALUE's `THROUGH`) were wanted. The row rule is now `place_rows`. A word between a
delimiter pair must sit on a row inside the boundary when it lies inside it, and outside when it lies outside. A
word beside the pair is not constrained by it. A row whose words agree stays whole and moves to the nearest height
of one of its own words that satisfies them all. It splits only on a conflict, which no figure of the standard has
today, so the self-test holds that case. That left EVALUATE's `THROUGH`. It is 3.9 pt below `identifier-3`, visibly
the next printed line, and the 4.0 pt width bound had merged the two. I measured every step between neighbouring
heights that the width bound groups. Words printed level sit up to 3.2 pt apart end to end, and the widest step
drawn as one line is 3.5 pt (`ALL` beside `BINARY ZEROES`, folio 277). Every step from 3.6 to 4.0 pt is the next
line. `cluster` therefore takes a gap as well as a width: a row ends at a step wider than 3.5 pt (`LINE_GAP`).
Narrowing the width instead moves where every later row starts, and at 3.5 pt it redrew six more figures, tearing
`IS [ NOT ] =` and `CLOSE … UNIT` apart. The gap regenerated nine figures, each checked against its printed page.
One was a real error: the `…` of the inline method invocation (§8.4.3.4.2, folio 133) repeats the argument inside
the parentheses, as its figure notes say, and the drawing had it outside them. `>> PUSH`, `CLASS IS`, `INVOKE`,
`[ BY CONTENT ]` and the CLASS clause's `IS` now sit on the point rows they are printed level with. `CODE-SET` is
back on its point row, and PICTURE draws as one line again. The nine FMT rows' verdicts were re-affirmed through
batch `w1150c-pb3530-review-r1-figure-regeneration` (GAP 66 → 66). The self-test gains the folio 342 and folio 133
figures, both of which fail on the first cut's renderer, plus unit checks for a whole row, a conflict split and the
line gap (18/18).

**Self-review.** I checked for a second copy of the pairing or span rule (none), a split that could fail to end
(each split makes both parts agree with the boundary that caused it), and the effect on
RequiredFormatPhraseDriftTests' scrape of §14.9 figures (its rows are unchanged). The semgrep counts did not change.

