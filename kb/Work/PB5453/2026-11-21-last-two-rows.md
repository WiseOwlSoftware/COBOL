# PB5453: the last two rows recorded on the train (wave 20262131, group m)

1985-GR-IV.4.1-p2 and 1985-SR-IV.4.1-p3 closed under `unpopulatable-antecedent`, from the controller's read
(`E:/COBOL-coord/scratch/w20262131/last2/read.json`: code location `src/WiseOwl.Cobol.Frontend/Pipeline/SourceTextDecoder.cs#SourceTextDecoder.Decode`,
citations each `cite.py` OK) and its refuter (`derivation-refute.json`, 2 UPHELD), as read slice s-101 superseding s-99 and
s-01 (`audit_batch.py import-reads`, then `derivation-batch --signature "claude: 2026-11-21"`, then `record_verdicts.py
--edition 1985`: GAP 197 -> 195). The import was blocked first by PB6020 (the read records could not follow group e's five
moves), fixed in the same change. `tests/version-matrix/derivation-refutations/lineage-reads-1985-s-12.jsonl` regenerated
from its record (`refutation_lines`): group i's s-100 re-read of 1985-GR-VI.6.4.5-p1 superseded the s-12 read its line
came from, and LINEAGE READS was RED on it. The note's `inventory_rows` is now empty; it stays open while the 195 rows
the PB5452 mint added are read (group o).