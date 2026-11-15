- **D-UNS2 — UNSTRING: an identifier DELIMITER is read live and DELIMITER IN receives its live content (kb/Work PB1907,
  row `GR-14.9.48.4-18`).** The same rule (§2023:14.9.48.4 GR18, Annex A.2 item 61, §2023:4.4 2)) covers identifier-2 and identifier-3
  overlapping identifier-4 and identifier-5. **WiseOwl COBOL reads an identifier delimiter from storage before each
  receiving area**, so an earlier area's store into it changes the delimiter for the later areas, and **DELIMITER IN
  receives the matched delimiter item's content as it stands after that area's INTO store** (libcob
  `cob_unstring_delimited` keeps the item's storage and `cob_unstring_into` copies from it after the move). A literal or
  figurative delimiter is unaffected, and an examination that ended at the end of the sender leaves DELIMITER IN empty
  (space-filled). **The ALL phrase's run of repeated delimiters (§2023:14.9.48.4 GR7) is consumed after the receiving area's
  INTO store and before its DELIMITER IN and COUNT IN stores**, over the sender and the matched delimiter as the store left
  them (libcob `cob_unstring_into`: `cob_memcpy` into the receiving area, then the repeat loop against the delimiter
  item's own storage), so an INTO store into an `ALL` delimiter or into the sender changes how much of the run is
  skipped (`CobolStringOps.UnstringExtract` consumes one occurrence and returns the matched delimiter's position,
  `CobolStringOps.UnstringSkipRun` consumes the repeats, `StringEmitter.EmitAllRun` re-reads and orders them). Measured (cobc 3.2.0 and this build agree): `UNSTRING ";,a;b,c" DELIMITED BY DLM INTO DLM DELIMITER IN
  D1 P2 DELIMITER IN D2 P3` with `DLM = ","` gives `DLM=[;] D1=[;] P2=[a  ] D2=[;] P3=[b,c]` (a snapshot gives `D1=[,]
  P2=[a;b] D2=[,] P3=[c  ]`), and `UNSTRING "a;;b;;;;c;" DELIMITED BY ALL D3 INTO D3 Y2 Y3` (`D3 = ";"`) gives `D3=[a]
  Y2=[;b;;;;c; ]` — the run after the first `;` is measured against the `"a"` the first area stored, so the second `;` is
  not skipped (consuming the run before the store would give `Y2=[b;;;;c;  ]`); and `"a;aab"` with `D4` overwritten the same
  way skips the `aa` after the first `;` (`Z2=[b  ]`, POINTER 6). The two-character delimiter, the INTO store that reaches the
  sender and the matched-position choice among several `ALL` delimiters were run through cobc 3.2.0 as well; every line
  agrees. Pinned by `conformance:cases/14.9.48-UNSTRING/pb1907_unstring_overlap_delimiter` (legs B1 to B5, kb/Work PB1909)
  and `unit:UnstringKernelTests`, held by `unit:UnstringLiveReadDriftTests`.
