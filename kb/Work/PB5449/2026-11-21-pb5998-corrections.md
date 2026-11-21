## 2026-11-21 — kb/Work PB5998 corrected the mechanism and read the 72 unread 1985 paragraphs (remaining item 2 done)

PB5452's pilot found defects in this note's landings; kb/Work PB5998 (branch `pb5449-segmentation`) fixed them at the
root: a numbered list printed one item per line is one unit per item, a clause's later lists take `L<m>.<n>` (an
addendum to decision PB5448), a table printed across pages numbers its rows on, the cross-reference test reads 1985's
folio pointer, and `segment` refuses two units of one label.

**Remaining item 2 of `2026-11-20-items-6-and-3-for-1985.md` is DONE**: the 72 unread 1985 paragraphs were read on
their rendered pages. Eight were no paragraph and left at their root (`rule_catalog_1985.furniture`, the one reading of
the page furniture `parse` and `_bodies` now share; the parenthesis join of `text_units.continues`; two markup repairs
of the transcription), 16 now measure (`HEAD_Y_OF` 40 for 1985, `best`), and 48 are page readings in
`scripts/spec/data/margin-readings-1985.json`. The 1985 margin-level data records 373 paragraphs, 52 at level 0, none
ambiguous or not found; the 1985 pending units are 1,026.

Items 1, 3, 4 and 5 of that file remain as written there.
