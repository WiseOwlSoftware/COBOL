- **D-KWV — the byte positions of a key that follows a variable-length member of its record** (2026-09-22; kb/Work
  PB1025). A SORT/MERGE key (§2023:14.9.40.3 SR6 / §2023:14.9.24.3 SR4) and an indexed RECORD KEY / ALTERNATE RECORD KEY
  (§2023:12.4.5.12 / §2023:12.4.5.6) are byte positions of the record, and a record with a dynamic-length member is sent and
  held as its contiguous image (§2023:8.5.1.11.2; D-FRA (iii)), so a key that such a member PRECEDES sits at a different
  position in each record. The standard bounds it — *"all the data items identified by key data-names shall be
  contained within the first x bytes of the record, where x equals the minimum record size"* (§2023:14.9.40.3 SR6 g);
  the same sentence is §2023:14.9.24.3 SR4 g), §2023:12.4.5.12.3 SR4 and §2023:12.4.5.6.3 SR5) — and says nothing more.
  **WiseOwl COBOL's determination:** (i) the rule holds for ANY record, so a key is measured at its furthest reach —
  its offset with every preceding variable-length member at its maximum size (§2023:13.18.43.4 GR8 b)) plus its length —
  against the file's minimum record size (§2023:13.18.43.4 GR9 when no integer-2 is written, which D-FRA (iv) makes every
  dynamic item at zero length); a key that can reach past it is rejected (COBOL0874 for SORT/MERGE, COBOL0863
  for an indexed key — both rules enforced on every variable-length file, a fixed-position key included); (ii) a
  legal such key — trailing fixed material keeps it within the minimum — is located in each record by D-FRA's own
  split: its position is its offset in the record's fixed run plus what each preceding member takes from that
  record, the same take step the decomposition uses, so a key compares, and an indexed file indexes, exactly the
  bytes a READ / RETURN of that record puts in the key: by the record's own extent table (D-FRA (v)) when it was
  written with one — so a key with variable-length members on BOTH sides is exact too (kb/Work PB1053) — and by
  the take step otherwise, exact whenever no variable-length member follows the key. A key taken from the RECORD
  AREA (a WRITE / REWRITE, a random READ, a START, a DELETE) is located by the area's own table the same way.
  **Rejected readings:** refusing the key by name (the previous posture — it rejected legal source); slicing it at
  its fixed-run offset (the previous indexed behaviour — a READ … KEY returned a different record); a length-
  prefixed image (rejected under D-FRA). Implemented once — `RecordLayout.KeyWindowOf`, `CobolContiguousLayout`
  (FILES_DESIGN D27); witnessed by `conformance:cases/12.4.5.6-ALTERNATE-RECORD-KEY/pb1025_key_after_dynamic_member`,
  `conformance:cases/12.4.5.12-RECORD-KEY/pb1025-indexed-key-beyond-minimum` and
  `conformance:cases/14.9.24-MERGE/pb1025-sort-key-after-dynamic-beyond-minimum`.
