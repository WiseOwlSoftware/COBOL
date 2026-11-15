- **D-ODO2 — a READ of a record made variable by OCCURS DEPENDING ON is judged AFTER the record lands, against the
  file's maximum record size** (2026-09-30; kb/Work PB1513, row `GR-9.1.13.6-4`). §2023:9.1.13.6 item 4 b): *"A READ
  statement is unsuccessfully executed because the records are variable in length because of an OCCURS DEPENDING ON
  clause in the record in the associated record description entry and the associated DEPENDING ON item contains a
  value that makes the number of bytes in the record exceed the value specified or implied by the file description
  entry"* — '34'. **WiseOwl COBOL's determination:** (i) the record's size at a count n is §2023:13.18.43.4 GR13 c)'s —
  the fixed portion plus the table at n occurrences — and "the value specified or implied by the file description
  entry" is the file's maximum record size (the largest description, or integer-1 / integer-3 of an explicit RECORD
  clause), so the test is a compile-time LIMIT on the count, the largest n whose record still fits, compared with the
  DEPENDING ON item (`SequentialIoEmitter.OdoRecordExceedsMaximum`); (ii) the DEPENDING ON item is read AFTER the
  record has landed in the record area, the only moment a count carried in the record has a value — so after a '34'
  the area holds the over-long record, which §2023:14.9.30.4 GR18 makes undefined anyway; the READ is unsuccessful, so it
  runs no NOT AT END and no INTO, stores no length and no relative key, invalidates the file position (GR18), and —
  the connector being open — begins the permanent error of §2023:9.1.13.1 (A.1 item 105); (iii) it applies to the READ
  statement of every organization (sequential, and the relative / indexed arm) and to the implicit READ of a SORT or
  MERGE USING transfer, which lands the record in the file's area for the purpose so the statement terminates under
  the same fatal-status table as any other as-if READ; (iv) **the rule is asked of a file with exactly ONE record
  description.** It speaks of *"the record in the associated record description entry"*; a file description with
  several record descriptions (implicit redefinitions of one area, §2023:13.18.33.4 GR3) has no one description a READ is
  associated with, and judging a description's count over bytes that belong to another record type would reject
  every legal record of the other types. **Rejected readings:** a bound at the table's own maximum occurrence count
  (ignores an integer-1 / integer-3 larger than the largest description — `RECORD CONTAINS 20` over a 12-byte record
  admits counts up to 18); judging every description of a multi-record file; an emit-time snapshot of the count taken
  before the record lands (a count carried in the record would be the previous record's).
  Witnessed by `conformance:cases/9.1.13.6-Permanent-error-condition-with-unsuccessful-completion/pb1513_read_odo_record_exceeds_maximum`
  (the implied value), `…/pb1880_odo_read_explicit_record_clause` (the SPECIFIED value: RECORD CONTAINS 14 and 16, and
  RECORD IS VARYING IN SIZE FROM 2 TO 16, so counts past the table's own maximum are judged by the clause's bytes),
  `…/pb1880_odo_read_bit_table` (the unit is the byte and a last item that ends inside a byte takes the whole byte,
  §2023:13.18.43.4 GR4: a count of 24 over a 1 TO 20 bit table is still five bytes and succeeds, 25 is six and is '34'),
  `…/pb1880_odo_read_depending_item_outside_record` (a WORKING-STORAGE DEPENDING ON item) and
  `…/pb1880_odo_read_34_exception_paths` (the '34' with a USE procedure only, with FILE STATUS and a USE procedure, on
  the relative arm, and uncovered, where the run unit terminates, Annex A.1 item 103).
