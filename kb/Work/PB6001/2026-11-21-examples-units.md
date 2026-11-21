## 2026-11-21 — the last 16 holes: §1985:VI.6.18.5 "Examples" is units, and a fence is a display (wave 20262132 group a)

**Sites:**
- `scripts/spec/text_units.py` (`display_mask`: new, the one reading of a display)
- `scripts/spec/catalog_prose_holes.py` (`prose`), `scripts/spec/rule_catalog_1985.py` (`_bodies`, `_spans`; `EXAMPLES_TITLE` deleted)
- `docs/rearchitecture/spec-unharvested-rule-blocks-1985.json`, `spec-rule-catalog-1985.json` (regenerated: 19 pending units)

`catalog_prose_holes.py --edition 1985` counted 16 holes after train B, all in §1985:VI.6.18.5. Two causes:

1. **The segmentation kept the Examples paragraph out of the units with no record** (`rule_catalog_1985.EXAMPLES_TITLE`,
   from reader 85u-12's `repair` records, which no refuter attacked and the import skipped). That is an exemption by
   pattern. Removed: an Examples paragraph is text like any other, so its preamble (p1), its five labels "Example n:"
   (p2 to p6), "The above INSPECT is equivalent to the following INSPECT:" (p7) and the 12 rows of its five result tables
   (T1.1 to T5.1) are 19 units, `pending` until their reader and refuter records are imported. The digests are the ones
   reader 85u-12 read, unchanged.
2. **The measure read a ``` fence as prose.** `catalog_prose_holes.prose` skipped a `<pre>` display but not a fence, while
   the segmentation (`text_units.segment`) makes no unit of either, so 14 of the 16 holes were lines of the examples'
   INSPECT code that no disposition could ever cover. Root fix: ONE reading of a display, `text_units.display_mask`
   (`<pre>`, a fence, an HTML table, closed as `segment` closes them), read by the measure and by the 1985 unit spans
   (`_spans`, which had its own copy). Sibling sweep: 38 fenced lines of the normative region were measured as prose;
   24 of them happened to be counted covered by a row. The 1985, 2002 and 2023 catalogs are unchanged by it
   (`extract_rule_catalog.py --held --check` GREEN; the 1985 rules identical, only `pending-units: 19` added).

Measure: 16 → 2 (p1 and p7 of VI.6.18.5, pending). Printed paragraphs measured 4,188 → 4,150. Self-tests that fail on
the base: `edition_status` case_07c (an Examples clause is units; its fenced code is no hole), `text_units` case_17
(the one display reading).
