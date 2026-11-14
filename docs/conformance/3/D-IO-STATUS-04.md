- **D-IO-STATUS-04 — I-O status '04' (record-length mismatch, §2023:14.9.30 GR14 / §2023:9.1.13.2 item 3)**: **emitted** on a
  record-sequential READ (and, from PB5480, a relative or indexed READ) whose physical record is outside the reading connector's min/max record size (fixed leg: shorter than
  the record width; varying leg: outside [VaryMin, VaryMax]) — the READ is successful, the record is delivered,
  status '04' (VCR 21; golden `2002/io_status_04`). Line-sequential is excluded (§2023:14.9.30 GR15 pads short records
  with trailing spaces — its '06' write path remains unimplemented). The value is version-invariant — the E.2
  item 15 2023 delta only clarifies *when* it is set.
  ⚖ **THE MEASURE IS THE FILE'S FIXED ATTRIBUTES, NOT THE READING PROGRAM'S DESCRIPTIONS** (kb/Work PB5238, a determination). §2023:14.9.30.4 GR14
  (and §1985:VII.4.4.4 GR13, §1985:VIII.4.5.4 GR16, §1985:IX.4.5.4 GR18) state the conflict as a record shorter or longer than "the
  minimum/maximum size specified by the record description entries", while §2023:9.1.13.2 item 3 and §1985:VIII.1.3.4 item 1 b define
  '04' as a record outside "the fixed file attributes", and §2023:9.1.6 makes "the minimum and maximum logical record size" such an
  attribute; §2023:14.9.30.4 NOTE 2 adds that the condition "will occur only when the operating environment does not check either the
  minimum or maximum record length as a fixed file attribute during OPEN processing or when a specific physical record within a
  physical file violates the fixed file attributes for that physical file". The two statements are reconciled by reading "the record description entries" as the file's minimum and
  maximum record size, the quantity §2023:13.18.43.4 GR9/GR10 take from the descriptions when the RECORD clause omits integer-2 or
  integer-3 and the RECORD clause states when it does (`FileConnector.VaryMin`/`VaryMax`). The other reading, a record measured
  against the smallest and largest description of the program that READs it, would answer '04' for every short record of the idiom
  `RECORD VARYING 1 TO 80 DEPENDING ON n` over one `PIC X(80)`, and make the clause's integer-2 and integer-3 mean nothing to a READ; it
  was weighed and refused (the code read's refuter and the held-rows adjudication took it; measured on this tree, it turned 24 existing
  conformance tests red, each written from the same rules and expecting '00'). A RELATIVE or INDEXED record outside the
  attributes reaches a READ only through a file whose attributes the OPEN could not check (kb/Work PB5480): the WRITE refuses it ('44')
  and the OPEN refuses a file created under other bounds ('39', A.1 item 129), but a ZERO-BYTE store records no attributes
  (`KeyedConnector.FixedAttributeConflict`, `StoreFormat.Empty`), so a connector opened INPUT on one can read the records another connector
  writes later under wider bounds. The relative and indexed READ (random and sequential) therefore tests the record's length against the
  reader's own minimum and maximum as the sequential READ does, through the one test `FileConnector.RecordLengthConflicts`: outside it the READ is
  successful, the long record truncated on the right to the maximum, status '04' (§1985:VIII.4.5.4 GR16, §1985:IX.4.5.4 GR18; every
  edition, §2023:14.9.30.4 GR14), and the RECORD VARYING DEPENDING ON item holds the length of the record read, as for the sequential READ.
  An indexed READ that is also a duplicate-key READ answers '04', not '02': the one status states the length conflict, which GR18 makes the
  READ's own outcome, where '02' only reports a neighbour (the standards state no order; determination). Pinned by
  `conformance:cases/VIII.4.5@1985-READ-STATEMENT/pb5480_relative_read_record_length_conflict`, `conformance:cases/IX.4.5@1985-READ-STATEMENT/pb5480_indexed_read_record_length_conflict` and
  `conformance:cases/14.9.30-READ/pb5238_read_inside_record_clause_is_no_length_conflict` (every edition).
