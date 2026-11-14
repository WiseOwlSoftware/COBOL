- **D-UNS4 — UNSTRING: the POINTER is read once at initiation and stored last (kb/Work PB1907, row `GR-14.9.48.4-18`).**
  GR13 — *"the content of the data item referenced by identifier-7 will contain a value equal to the initial value plus
  the number of characters examined"* (`cite.py --check 14.9.48.4 "will contain a value equal to the initial value plus
  the number of characters examined"` → OK, 13)) — describes the non-overlapping case; GR18 (A.2 item 61) leaves
  identifier-7 sharing storage with identifier-1, -4, -6 or -8 undefined. **WiseOwl COBOL reads the POINTER item once, at
  initiation, and that value governs the whole examination**: INTO or COUNT IN stores into the pointer item do not move
  it, and the final pointer value overwrites them. The POINTER and TALLYING items are stored only at the end of the
  statement, so a sending item that contains either one is examined unchanged and holds the new value only afterwards
  (libcob `cob_unstring_init` and `cob_unstring_finish`; this case already matched GnuCOBOL on every leg). Pinned by
  `conformance:cases/14.9.48-UNSTRING/pb1907_unstring_overlap_pointer`, which also guards D-UNS1 to D-UNS3 against re-reading the pointer
  mid-statement.
