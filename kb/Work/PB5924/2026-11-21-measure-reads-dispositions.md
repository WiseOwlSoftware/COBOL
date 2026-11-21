## 2026-11-21 — the prose-holes measure reads the dispositions (wave 20262131 group e, kb/Work PB6001; branch `train85b-cat`)

`catalog_prose_holes.holes` now covers a printed paragraph in no row by the unit that holds its line
(`text_units.Unit.lines`) when that unit is `restates` or `not-a-rule` on an UPHELD refuter record of its key
(`text_units.record_refusal`), or, when no unit holds it, by its clause's `not-rules` block on its record at the text's
digest now (`text_units.block_refusal`). It reads the manifest through `extract_rule_catalog.units_1985` (no second
reader) and skips what the segmentation does not read as prose (tables in both markups, captions and labels, a page's
repeated section title). Cases: `scripts/selftests/spec/edition_status/test_edition_status.py`
`case_07b_a_paragraph_leaves_the_count_only_on_a_disposition_and_its_record` (RED on the base, GREEN on the branch).

| cause | holes of the 357 |
|---|---|
| in a `restates` unit on its UPHELD record | 235 |
| in a `not-a-rule` unit on its UPHELD record | 52 |
| in a `not-rules` block's held list on its record | 22 |
| no prose: the two HTML tables' lines | 21 |
| no prose: a caption (Table, Figure) | 5 |
| no prose: a page's repeated section title | 5 |
| still counted | 17 |

The 17: §1985:IV.4.2.2.1.3 p3 (a pending unit), and the 16 printed paragraphs of §1985:VI.6.18.5, the paragraph headed
"Examples", which the segmentation keeps out of the units (`rule_catalog_1985.EXAMPLES_TITLE`, on reader 85u-12's repair
records) with no refuter record: no record establishes their cover, so they stay counted until one does (a refuter
pass over the exclusion, or units). The deontic floor went 24 -> 0 and the elsewhere lead 32 -> 0.
