## 2026-11-21 — wave 20262131 group e (branch `train85b-cat`): both defects fixed at the root; the units they add are pending

**Sites:** `scripts/spec/text_units.py` (`segment`, `html_rows`, `Unit`, `keyed_to`, `record_refusal`, `block_refusal`,
`block_refusals`, `class_counts`, `after_rule`, `table_units`), `scripts/spec/rule_catalog_1985.py` (`_bodies`, `block_paragraphs`,
`units`, `Paragraph`, `parse`), `scripts/spec/extract_rule_catalog.py` (`iso_units`, `units_1985`, `refuter_records`).

1. **A list is held by its marker, not by its clause.** `segment(lines, held=<pattern>)` replaces `items: bool`: the 1985
   arm holds the `(n)` lists its content-keyed scan measures (`rule_catalog_1985.ORDINAL`), the ISO arm its `n)`/`n.`
   lists (`extract_rule_catalog.ORDINAL`), a rule's text every list. §1985:IV.4.2.2.1.3 gains `L2.1`-`L2.3` (pending).
2. **An HTML table is a table** (`html_rows`: each `<tr>` a `|` row), and a caption printed right below an uncaptioned
   table numbers it. §1985:XIII.3.10.7 and 3.10.9 lose the paragraph that held the table's markup (p2, pending) and gain
   `T2` and `T4` (data tables, pending); their later paragraphs move up one (p3..p7 -> p2..p6). A sibling the caption
   rule found: §1985:XIV.2.2.6's table prints "Table 2: Error Key Values" below it, and its row was
   `1985-GR-XIV.2.2.6-T1` cited the clause's "Table 1" (a table the clause does not print): now `-T2`, "Table 2".
3. **A refuter record is keyed by its unit's key** (clause, digest, order), never by its label: the relabel in 2 orphaned
   six UPHELD records whose words did not change (`text_units.keyed_to`).

Relabelled rows (all GAP, none in the 1985 store, so no move batch was written): `1985-GR-XIII.3.10.7-p4/p5` ->
`-p3/-p4`, `1985-GR-XIII.3.10.9-p4/p5` -> `-p3/-p4`, `1985-GR-XIV.2.2.6-T1` -> `-T2`.

**Other editions:** `extract_rule_catalog.py --edition 2002 --check` and `--edition 2023 --check` print byte-identical
output on the base and the branch (2002: 1767 units, 1766 pending, 1 rows; 2023: 2316 units, 2298 pending, 18 rows).

**Not done:** "Done when" needs the new units disposed (`IV.4.2.2.1.3` p3, L2.1-L2.3; `XIII.3.10.7` T2;
`XIII.3.10.9` T4), read and refuted from the slices `E:\COBOL-coord\scratch\w20262131e\in\85w-10.json`, imported and
minted. The note stays open.
