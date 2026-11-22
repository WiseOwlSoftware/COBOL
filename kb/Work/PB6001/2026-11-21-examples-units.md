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

**Disposed (four refuter rounds, slices 85e-01 to 85e-04, records in `docs/rearchitecture/spec-dispositions-1985/85e-0N-refute.jsonl`).**
`import_dispositions.py --edition 1985 --slices 85e-01,85e-02,85e-03,85e-04 --write`: `19 units disposed ... GR 3,
not-a-rule (lead-in) 5, restates 11`. The reader (the implementer) read p1 a definitional GR, p2-p6 lead-ins, p7 and the
12 rows `restates`; the refuters made p7 and T4.1 rows of their own (p7 states the CONVERTING equivalence; T4.1's AB that
runs into the BEFORE "BC" is fixed only by the printed row) and widened each row's `restates` list (p1 for the zeroed
counters, GR8, SR3 and §1985:IV.4.2.2.2.3 GR5 for QUOTE), each correction refuted again until UPHELD. Three rows minted:
`1985-GR-VI.6.18.5-p1` (definitional: GAP until its derivation and that derivation's refuter), `-p7` and `-T4.1`, both
CONFORMS on their own witnesses (`conformance-suite/cases/VI.6.18@1985-INSPECT-STATEMENT/1985-GR-VI_6_18_5-p7`, `-T4_1`).
Measure: holes 0, pending units 0; 1985 GAP 195 -> 198 (minted) -> 196 (witnessed).

**p1's derivation, and the catalog's `definitional` flag.** The definitional proposal for `1985-GR-VI.6.18.5-p1` was
OVERTURNED (its subject is the standard's own examples, not a user's program; the record is
`tests/version-matrix/derivation-refutations/w20262132a-1985-p1-definitional.jsonl`) and the `informative` arm UPHELD
(read `tests/version-matrix/lineage-reads-1985/e-01.json`, its refuter record `lineage-reads-1985-e-01.jsonl`), written by
`audit_batch.py import-reads` and `derivation-batch --signature "claude: 2026-11-21"` (decision PB6012): §8
`DRV-1985-GR-VI.6.18.5-p1`, CONFORMS, GAP 196 -> 195. The refuter saw the row carry `definitional: true`. Root: the importer
copied a reader's advisory `definitional` into the manifest entry and `rule_catalog_1985.mint` copied it into the row, so
the catalog pre-judged an arm that only the store's derivation decides; nothing read the flag, and it contradicted all 11
informative rows of train B too. Both copies are deleted (`import_dispositions.decide`/`corrected`, `mint`), the 417 manifest
entries lose the key (a round-tripped strip), the catalog is regenerated without it (RULE CATALOGS GREEN), and the
importer's self-test now expects the entry without it.
