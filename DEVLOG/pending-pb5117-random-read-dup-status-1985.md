## PB5117: a random READ of an alternate key with duplicates answers I-O status 02 at --std 85

Re-probe on the Debug build: the note's program (alternate key AA with records 0001AA, 0002AA) printed `READ ALT AA 0001AA 00` at `--std 85`; the 1985 text (§1985:IX.1.3.4 item (1) b), no access mode named) requires 02. From 2002 the rule is limited to a READ with NEXT or PREVIOUS (§2002:9.1.12.1 GR2a, §2023:14.9.30.4 GR27), so 00 is right there.

Fix: a new `DialectBehavior.IndexedDuplicateKeyStatusSequentialOnly` (changed in 2002, VCR row 7.55, `tests/doc-facts/compiler-facts.json`, PublicAPI). `IndexedConnector.ReadRandom` asks it; below 2002, for an alternate key of reference, it seeks strictly past the record found in (key part, release ordinal) order (`NextRecordDuplicatesKey`) and answers 02 when the next entry has an equal key part. The prime key never repeats, so it is skipped. The sequential READ was already correct at every edition (sibling arm checked: `ReadSequential` is the only other READ producer of 02).

Goldens: `14.9.30-READ/pb5117_random_read_dup_status_1985` (accept 85, 02 on the random READ) and `pb5117_random_read_dup_status` (2002, 2014, 2023, 00), both with a sequential READ NEXT walk showing 02 at every edition. `generate.py --check` GREEN. No inventory rows closed (the inventory has no 1985 rows, PB5094).
