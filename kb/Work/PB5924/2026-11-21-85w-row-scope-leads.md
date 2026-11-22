## 2026-11-21 - the 85w readers' catalog leads: rows that end at their lead sentence (wave 20262131 group o)

The read fleet of the 195 rows the 85w mint added (slices g-01..g-06, s-02, recorded as `lineage-reads-1985/g-*.json`)
reported rows whose text stops at the lead of a list or table that no row holds as the row's own scope. On branch
train85b-reads `catalog_prose_holes.py --edition 1985` reads 16 uncovered printed paragraphs (all in VI) and counts the
glossary's paragraphs covered by row, `restates` or `not-a-rule` dispositions, so these are not prose holes of that
measure; they are a SCOPE question the measure cannot see: the row's obligation is in its list, and the list's paragraphs
are covered by a disposition, not by the row that states them.

- `1985-SR-III.2-p213`: ends at "The permissible paragraph headers in the Identification Division are:"; the
  Identification list, the Environment lead and its list are in no row (g-04).
- `1985-GR-III.2-p231`: only "A character that belongs to the following set:"; the punctuation-character table is in no
  row (g-04).
- `1985-SR-III.2-p275`: stops at "INPUT-OUTPUT SECTION."; the Data Division and Procedure Division section headers of
  page III-22 have no row (g-05).
- `1985-GR-III.2-p83`: the editing-character table (B 0 + - CR DB Z * $ , . /) has no rows, unlike T7 (g-02).
- `1985-GR-III.2-p217`: the readers asked whether p214-p217, p221, p224, p226, p228, p232-p234 (Paragraph-Name ...
  Qualifier) are rowless by the mint's intent (g-04): the measure counts their paragraphs covered (its 16 uncovered
  paragraphs are all in VI), so a disposition, not a row, holds each.
- `1985-GR-B.2.1-p2`/`-p3`: the opening paragraphs of 2.1A and 2.1B (Amendment 2, C324) are filed under 2.1, so 2.1A and
  2.1B have no locator and their items would collide with 2.1's (s-02).
- `1985-GR-IV.4.2.2.1.3-L2.1`: the required-word definition ("A required word is a word whose presence is required when
  the format in which the word appears is used in a source program", §1985:IV.4.2.2.1.3.1 p1 per the reader's cite.py
  check) and its lead "Required words are of two types" have no row (s-02).
- p29, p249 (the readers' COBOL character set and special-character lists): the same shape.

What is owed: a row whose text is a lead takes its list as its scope (the segmentation joins a lead and the list it
introduces into ONE unit), and the measure reports a lead-only row; then each list above is read under its row.
