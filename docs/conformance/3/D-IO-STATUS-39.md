- **D-IO-STATUS-39 — I-O status '39' (fixed-file-attribute conflict, §2023:9.1.13.6 item 7; E.3.3 item 35) — TWO DIFFERENT CASES, and
  only one of them is a gap.** '39' is not produced on either statement, but the reasons are not the same and
  collapsing them (as this bullet did until 2026-08-31) hid a conforming determination inside a deferral.
  - **DELETE FILE — CONFORMING, by definition.** §2023:14.9.10.4 GR19 requires the implementor to *define* which
    fixed-file attributes DELETE FILE validates; WiseOwl COBOL's definition is **the empty set** (§7, Annex A.1
    item 50 — see there for the full determination). GR19 nowhere requires the set to be non-empty, so with
    nothing validated GR18 has nothing to compare and its '39' is **unreachable by definition, not by
    omission**. Golden `2023/delete_file_attribute_set_empty`: a DELETE FILE issued through a connector that
    contradicts the physical file in two §2023:9.1.6 attributes (organization and record size) still deletes, '00'.
  - **OPEN — CONFORMING, and its set is NOT empty.** §2023:14.9.27.4 GR10 delegates the validated set the same
    way GR19 does, but the two obligations are not the same question and WiseOwl COBOL answers them differently:
    a DELETE FILE destroys the file, so nothing downstream depends on the connector's description having been
    right, while everything after an OPEN reads the file THROUGH that description. WiseOwl COBOL therefore
    records each physical file's §2023:9.1.6 fixed file attributes when the OPEN statement CREATES it (GR18's
    OUTPUT, GR17's absent OPTIONAL I-O/EXTEND) and compares them on every later OPEN — see §7, Annex A.1
    item 129 for the set and `FixedFileAttributes` for the mechanism. The set is **per organization**, as
    GR10's third sentence allows: the ORGANIZATION for every file; additionally the record type, the record
    sizes and the key descriptors for RELATIVE and INDEXED, whose store's structure IS those attributes.
    A SEQUENTIAL file validates nothing beyond the organization — §2023:9.1.7.2 puts its record lengths in the
    data and in the reading program, and §2023:9.1.13.2 items 3, 5 and 7 answer a re-read disagreement with the
    successful '04', '06' and '09', so a '39' there would reject the legal write-then-read-back idiom. A file
    whose attributes are not recorded — an external tool's, or one written before the catalog existed —
    supplies nothing to compare and opens normally. Goldens `85/open_fixed_attribute_conflict` and
    `2023/open_fixed_attribute_conflict_ix`.
