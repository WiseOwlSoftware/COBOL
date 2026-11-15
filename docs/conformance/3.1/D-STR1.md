- **D-STR1 — STRING runs in place: a SENDING operand is read when its transfer begins (kb/Work PB1908, row
  `GR-14.9.43.4-10`).** §2023:14.9.43.4 GR10 — *"If identifier-1, or identifier-2, occupies the same storage area as
  identifier-3, or identifier-4, or if identifier-3 and identifier-4 occupy the same storage area, the result of the
  execution of this statement is undefined, even if they are defined by the same data description entry"* (`cite.py
  --check 14.9.43.4 "or if identifier-3 and identifier-4 occupy the same storage area, the result of the execution of
  this statement is undefined, even if they are defined by the same data description entry"` → OK, 10)) — and Annex A.2
  item 60 (`cite.py --check A.2 "If the STRING identifier, or DELIMITED BY identifier, occupies the same storage area
  as the INTO identifier"` → OK, 60)) leave the case open; §2023:4.4 2) makes a run unit that allows it conforming, and GR10
  widens §2023:14.6.10 rule 2 so the overlap is undefined even for one data description entry. **WiseOwl COBOL moves the
  characters into identifier-3 as the statement runs and reads each sending operand when its own transfer begins:**
  the image the earlier transfers built is stored into identifier-3 first, so a sender that shares storage with
  identifier-3 (the same record, a REDEFINES view, a level-66 alias, a table element, a group that contains it, a LINKAGE
  item passed twice by reference or a BASED item) reads what those transfers wrote. GR6 words the transfer as though the
  characters "were moved one at a time from the source into the character positions of the data item referenced by
  identifier-3" (`cite.py --check 14.9.43.4 "the moves behave as though the characters were moved one at a time from the
  source into the character positions"` → OK, 6)), which is the in-place reading; before this entry every sender and
  delimiter was read once before the first character moved. This is libcob `cob_string_append`, which moves with `memcpy`
  into `string_dst->data` and reads `src->data` (and the delimiter's storage, D-STR2) on every call. Measured (cobc 3.2.0
  and this build agree): `STRING "XY" R1(1:2) R1(3:2) INTO R1` over `R1 = "ABCDEFGHIJKL"` gives `XYXYXYGHIJKL` (the
  earlier behavior gave `XYABCDGHIJKL`), a table element sender `T(1)` after `"123"` filled `T(1)` gives `123123ZZZ`, a
  numeric DISPLAY item as its own sender (`"99"` then `N` over 123456) gives `999934` with the overflow of GR8, a group
  sender that contains its receiver (`"ab"` then `G` into `G`'s first four bytes) gives `ababBBBB`. **Not an overlap
  effect and unchanged:** ONE sender whose own transfer overlaps the positions it writes (`STRING R(1:6) INTO R WITH
  POINTER 3`): libcob calls `memcpy` over overlapping storage, which the C standard leaves undefined, and glibc moves
  the sender as it stood when that transfer began (`ABABCDEFIJKL`; a character-at-a-time smear, GR6 read literally,
  would give `ABABABABIJKL`). WiseOwl COBOL does the same, because a transfer is one `StringTransfer` over the sender's
  image, and it is pinned as the platform's behavior, not as a rule. The decision "can a transfer reach this operand?"
  is `StorageOverlap.MayShareStorage` (the finer of the two predicates, kb/Work PB1911) asked by
  `StringEmitter.ReadsStorageOf`, and a STRING whose operands are provably disjoint, or whose only reachable operand is
  its first sending, renders the single store it always did. Pinned by
  `conformance:cases/14.9.43-STRING/pb1908_string_overlap_sender` (S1 to S5) and
  `conformance:cases/14.9.43-STRING/pb1908_string_overlap_linkage` (K1, by-reference aliasing), and held by
  `unit:StringLiveReadDriftTests`.
