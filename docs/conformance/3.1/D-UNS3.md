- **D-UNS3 — UNSTRING: the TALLYING item is incremented at the END, and the POINTER is stored last (kb/Work PB1907, row
  `GR-14.9.48.4-18`).** GR14 gives *"its value at the beginning of the execution of the statement plus"* the count
  (`cite.py --check 14.9.48.4 "a value equal to its value at the beginning of the execution of the statement plus"` → OK,
  14)) for operands that share no storage; GR18 removes that guarantee when identifier-4, identifier-6 or identifier-7
  occupies the same storage as identifier-8. **WiseOwl COBOL adds the number of receiving areas acted upon to the
  TALLYING item's content as it stands after the last area**, so an INTO or COUNT IN store into the TALLYING item is part
  of the sum, and **stores the TALLYING item before the POINTER item**, so when both name one item the pointer is the
  last value stored (libcob `cob_unstring_tallying` then `cob_unstring_finish`, in the order `cb_emit_unstring` emits
  them). With no overlap the value equals GR14's. Measured (cobc 3.2.0 and this build agree): COUNT IN storing 03 into the
  TALLYING item over two areas gives `T=05`, INTO storing 07 gives `T=09`, and POINTER and TALLYING on one item starting at
  1 over six examined characters gives `PT=07` (a start-value tally gives 02, 02 and 03). Pinned by
  `conformance:cases/14.9.48-UNSTRING/pb1907_unstring_overlap_tallying`.
