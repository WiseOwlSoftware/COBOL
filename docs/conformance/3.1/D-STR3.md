- **D-STR3 — STRING: the POINTER is read once at initiation and stored last (kb/Work PB1908, row
  `GR-14.9.43.4-10`).** GR4 and GR6 describe identifier-4 for operands that share no storage; GR10 leaves identifier-4
  sharing storage with identifier-1, identifier-2 or identifier-3 undefined. **WiseOwl COBOL reads the POINTER item
  once, at initiation, and that value governs the whole statement**: a transfer that overwrites the pointer item does
  not move the next transfer, a sending operand that is the pointer item is read like any other (D-STR1, so it holds the
  characters the earlier transfers wrote), and **the pointer's final value is stored after identifier-3**, so it
  overwrites whatever the transfers left there (libcob `cob_string_init` reads `cob_get_int(string_ptr)` once and
  `cob_string_finish` stores `string_offset + 1`; this case already matched GnuCOBOL on every leg, and the golden pins
  it so D-STR1 cannot start re-reading the pointer). The pointer is stored only when characters moved (GR6: it
  changes as they move), where libcob stores it unconditionally; the two cannot differ, because with nothing moved the
  value stored is the value read and nothing else wrote it. Measured (cobc 3.2.0 and this build agree): `STRING "WXYZ"
  INTO G WITH POINTER P` with `P` the first two bytes of `G` gives `05YZcdefghij`; `"ab" "cd"` with `P` (3) at bytes
  3-4 of the receiver gives `..07cd..` (the second transfer continues at 5 although the item now holds `ab`); and the
  pointer item as a sender after a transfer overwrote it gives `05ABefgh`. Pinned by
  `conformance:cases/14.9.43-STRING/pb1908_string_overlap_pointer` (P1 to P3).
