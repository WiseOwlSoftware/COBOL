- **D-SORT-RELEASE-SIZE-85 — a RELEASE of a record outside the SD's RECORD IS VARYING range at COBOL-85** (kb/Work PB5124;
  the text decides, no latitude). §1985:VII.3.8.4 GR10 (the Amendment 2 paragraph): "If the number of character positions in
  the logical record to be written is less than integer-2 or greater than integer-3, the output statement is unsuccessful and,
  except during execution of a RELEASE statement, the associated I-O status is set", and the SD's RECORD clause is this
  clause. COBOL-85 has no exception checking, so the sentence is the whole outcome: nothing is released, data-name-1 keeps
  its value (GR9) and the record area its content (§1985:III.2, Unsuccessful Execution: it "does not affect any data
  referenced by that statement"); no I-O status is set, and the statement after the RELEASE runs. From COBOL-2002 the
  sentence raises EC-SORT-MERGE-RELEASE (§2023:13.18.43.4 GR14b) and with that checking off the result is the
  implementor's (D-SMA (c)), so the edition is asked once, by the binder (`BoundRelease.RangeFailsStatement`), and the
  runtime's one range test reads it (`CobolSort.ReleaseStatement`). Only data-name-1 can name a size outside the range:
  §1985:VII.3.8.3 SR2 keeps every record description inside it. Witnesses:
  `conformance:cases/VII.3.8@1985-RECORD-CLAUSE/pb5124_release_outside_record_range_85` (short, long, zero, and FROM in and
  out of range) and `unit:SortMergeStatementEcTests` (`ReleaseStatement_RangeFailsStatement_ReleasesNothing_WhenUnchecked`).
