## The 1985 text-layer diff anchors an insertion on three words a side and names two classes of header and marker difference (PB6204, PB6135)

**PB6204.** Inside the block an Amendment note governs, an inserted run was still located by one neighbour word on each side, so a
short insertion at a wrong place in a wide block passed as AMENDMENT-APPLIED. Probe (a one-paragraph block where `is` and `a` stand
around `not` in the amended sentence and in a sentence the Amendment does not touch): AMENDMENT-APPLIED on the tool as it stood,
UNCOVERED now. `_stands_between` takes `ANCHOR` = 3 page words on each side of an inserted run (and of a drop) and finds them
consecutive around it in the note's own entry; a side with fewer words on the page, or a run flush with the entry's edge, is never
anchored. Measured over all 976 pages: AMENDMENT-APPLIED 33 to 33, insertions 15 to 15, no row moved (every committed insertion
already stands on three entry words a side). The PB6144 cases were retyped with entries long enough for three words a side.

**PB6135.** Two new classes keep 40 LIKELY-ERROR rows from the page review. TABLE-HEADER: a header the layer reads line by line
across the columns (`2ND 3RD ELEMENT STD STD` for `ELEMENT | 2ND STD | 3RD STD`) is one reading of the header when the words left
unexplained beside it and the header's words left in the Markdown are the same bag (`table_header_groups`, `_header_reading`); it runs
before the moved-run pairing. FOOTNOTE-RENUMBERED: a hunk of numbered markers (and the layer's damaged words beside them) that the
Markdown numbers higher by one offset per shape, with the words either side equal, the offsets never falling on the page and a second
marker at the same offset (`_renumbering`, `_renumbered_hunks`). Both fail closed; neither is an excuse for a word that differs. The
brief's premise that the Markdown holds the header once was not what the pages show (it holds it on each page); the rule follows the
pages. LIKELY-ERROR 128 to 88, UNEXPLAINED 603 to 554, OCR-NOISE 5266 to 5260, TABLE-HEADER 26, FOOTNOTE-RENUMBERED 17; every other
class unchanged. PB6135 stays open; evidence in `kb/Work/PB6135/2026-11-24-two-classes-measured.md`.

**Self-test.** Cases 17 to 19 (the wide-block probe; header read in another order, its negatives; marker offsets, its negatives).
