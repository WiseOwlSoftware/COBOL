- **D-OVL1 — operands that overlap: a MOVE (and an arithmetic statement) reads its sender in full, then writes
  its receiver** (kb/Work PB1907; Annex A.2 item 36; `GR-14.6.10-1`). §2023:14.6.10 1): "When the data items are not
  described by the same data description entry, the result of the statement is undefined" (`cite.py --check 14.6.10
  "When the data items are not described by the same data description entry"` → OK, 1)); 2): "When the data items
  are described by the same data description entry, the result of the statement is the same as if the data items
  shared no part of their respective storage areas" (OK, 2)); Annex A.2 item 36 lists both the entries that do not
  share a description (a) and "one or more of the operands is reference-modified" (b) (`cite.py --check A.2 "When
  one or more of the operands is reference-modified"` → OK, 36) b)). **WiseOwl COBOL's behavior is the snapshot:**
  the sending operand is evaluated completely before any part of the receiving operand changes, whatever the two
  share, so `MOVE X(1:5) TO X(2:5)` of `ABCDEF` gives `AABCDE` (the characters 1 to 5 are copied as they stood),
  the backward move `MOVE X(2:5) TO X(1:5)` gives `BCDEFF`, a REDEFINES view gives the same, and a group sent into
  its own subordinate item, or the reverse, copies the image it held when the statement began. A shorter sender
  space-fills the receiver after the copy (`MOVE Y(1:3) TO Y(2:5)` gives `AABC  `). `ADD M2 TO M1`, where `M2`
  redefines the last three digits of `M1`, reads both operands before it stores: `1234 + 234` gives `1468`. This is
  what GnuCOBOL does for every one of these shapes (GnuCOBOL 3.2.0 built and run on each; its `memmove`-based
  alphanumeric moves and its digit-extracting numeric moves are snapshots). Witness: `conformance:cases/14.6.10-Overlapping-operands/pb1907_overlap_move` (the `RM-*`, `RD-*`, `GRP-SUB`, `SUB-GRP`,
  `RM-PAD`, `NUM-ADD`, `JUST-EQ` and `NUM-SAME` lines).
