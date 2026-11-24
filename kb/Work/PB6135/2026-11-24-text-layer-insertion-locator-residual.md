# PB6135 addendum (2026-11-24): the insertion locator of the text-layer diff is still a one-word-per-side rule inside the governed block

**Source:** `E:/COBOL-coord/scratch/reports/w20262160a-PB6144-report.md`, Review findings R1-1, R1-2 and R2-1 (leads, low, not blocking;
PB6144 is landed, so they are tracked here, with PB6135's checks).

1. **Residual locator (R1-1, R2-1).** After PB6144's round-1 fix a note excuses only differences in the Markdown block it governs
   (`text_layer_diff._inside`: the nearest block above the note, past other notes; a `<pre>` or fenced figure is one block). Inside
   that block an insertion is still located only by one page word on each side (`scripts/spec/text_layer_diff.py:480`
   `_stands_between`), and real governed blocks are wide: tables of 36-94 lines on 1985 pp. 18, 25, 32, 62, 74, 191, 192, 725-727 and
   739 (several with 3-4 notes each), `<pre>` figures up to 47 lines. A short meaning-changing insertion in a sibling sentence or table
   row of the same block whose two neighbour words recur in the entry passes as AMENDMENT-APPLIED. `probe_not.py` (both sentences on one
   line) still passes. The report accepts it for now, because it fails only inside text the note already governs and the PB6051 page
   review reads every page. The durable target (R1-1): anchor on the note's printed run, 3 or more words a side.
2. **Stale count (R1-2).** `kb/Work/PB6135/2026-11-23-checks-built.md:47` re-measures its paragraph to the branch counts (AMENDMENT-APPLIED
   33, UNCOVERED 807, LIKELY-ERROR 128, UNEXPLAINED 603) but keeps "the residual is on 251 pages" from main; the committed JSON at the
   branch head has UNEXPLAINED or LIKELY-ERROR rows on 250 pages (`--check` prints "603 unexplained on 250 page(s)"). Re-measure at
   main before correcting it.

**Sites:** `scripts/spec/text_layer_diff.py:456` (`_inside`, `_stands_between`), `kb/Work/PB6135/2026-11-23-checks-built.md` (whole: one count in one paragraph)
