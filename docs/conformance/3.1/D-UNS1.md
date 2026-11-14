- **D-UNS1 — UNSTRING runs in place: the SENDING item is read live (kb/Work PB1907, row `GR-14.9.48.4-18`).**
  §2023:14.9.48.4 GR18 — *"If identifier-1, identifier-2, or identifier-3, occupies the same storage area as identifier-4,
  identifier-5, identifier-6, identifier-7, or identifier-8, … the result of the execution of this statement is
  undefined, even if they are defined by the same data description entry"* (`cite.py --check 14.9.48.4 "the result of the
  execution of this statement is undefined, even if they are defined by the same data description entry"` → OK, 18)) —
  and Annex A.2 item 61 (`cite.py --check A.2 "UNSTRING statement. If the UNSTRING identifier, DELIMITED BY identifier,
  or OR identifier, occupies the same storage area as the INTO identifier"` → OK, 61)) leave the case open; GR18 widens
  §2023:14.6.10 rule 2 (*"When the data items are described by the same data description entry, the result of the statement is
  the same as if the data items shared no part of their respective storage areas"*, OK) so the overlap is undefined even
  then. **WiseOwl COBOL examines identifier-1 as it stands before each receiving area:** the characters of the sending
  item are read from storage again before every receiving area after the first, so an earlier area's INTO, DELIMITER IN
  and COUNT IN stores into the sender's storage (the same record, a REDEFINES view, a level-66 alias, a BASED item or a
  LINKAGE item passed twice by reference) change what the later areas see. Nothing is snapshotted at initiation except
  the pointer's starting value and, for a fixed-length sender, its size; a variable-length sender is re-read at its current
  extent, and a subscript or reference modifier inside the sender is evaluated again with the re-read. No diagnostic is
  issued. This is libcob `cob_unstring_into`, which scans `unstring_src->data` on every call. Measured (cobc 3.2.0 and
  this build agree): `UNSTRING REC DELIMITED BY "," INTO F2 B C TALLYING IN T`, with `F2` the second 4-byte field of
  `REC = "AAA,BBB,CCC"`, gives `REC=[AAA,AAA CCC] B=[AAA CCC] C=[zzz] T=02` (a snapshot would give `B=[BBB    ] C=[CCC]
  T=03`). The decision "does a store reach this operand" is `StorageSharing.MayShare`, a conservative storage-overlap
  predicate (a false positive costs one redundant re-read and changes no result); an UNSTRING whose operands are
  provably disjoint emits no re-read. Pinned by `conformance:cases/14.9.48-UNSTRING/pb1907_unstring_overlap_sender` (same record,
  COUNT IN, REDEFINES) and `conformance:cases/14.9.48-UNSTRING/pb1907_unstring_overlap_linkage` (by-reference aliasing), and held by
  `unit:UnstringLiveReadDriftTests`.
