- **D-OVL2 — an overlapping MOVE writes its receiver while it reads its sender: GnuCOBOL's order** (kb/Work PB1907,
  PB1910; Annex A.2 item 36; `GR-14.6.10-1`). GnuCOBOL's `libcob` differs from the snapshot of D-OVL1 in five
  families, all because the library prepares or writes the receiver while it is still reading the sender, and the
  owner chose to follow it (2026-10-01). **(a) A JUSTIFIED RIGHT receiver with a shorter alphanumeric sender:**
  §2023:13.18.32.4 2) aligns the data "at the rightmost character position" of a receiver "larger than the sending operand"
  (`cite.py --check 13.18.32.4 "the receiving data item is described with the JUSTIFIED clause and it is larger than
  the sending operand"` → OK, 2)); WiseOwl COBOL writes the leading pad positions (receiver size minus sender size,
  space-filled) FIRST, then moves the sender, so a sender that overlaps those positions reads spaces: with `J1 PIC
  X(4)` holding `ABCD` and `J2 PIC X(3) JUSTIFIED RIGHT` redefining its last three characters, `MOVE J1(1:2) TO J2`
  gives `A A ` (the snapshot `A AB` is not produced). A sender of the same size or larger is a plain `memmove` and
  stays the snapshot (`JUST-EQ`, `AABC`). The sender may be an alphanumeric or alphabetic item, a group, or a
  reference-modified slice; a numeric sender keeps the snapshot. **(b) Conversion into a numeric DISPLAY or a
  PACKED-DECIMAL receiver is IN PLACE** (`cob_move_display_to_display`, `cob_move_alphanum_to_display`,
  `cob_move_display_to_packed`, `cob_move_packed_to_display`): the library clears the receiver and then writes it one
  digit (one digit pair, for PACKED-DECIMAL), left to right, taking each from the sender's bytes AS THE STORAGE THEN
  HOLDS THEM. A position the receiver has already written is read back as the receiver's content. WiseOwl COBOL runs
  the same algorithm (`OverlappingMove` in the runtime, one method per family) on the two storage images laid over
  each other, for a sending numeric DISPLAY item whose description differs from the receiver's (GnuCOBOL's compiler
  copies identical descriptions byte for byte, and so does this: the snapshot, `NUM-SAME`), for an alphanumeric
  sending item or slice, and for a PACKED-DECIMAL sending item into a numeric DISPLAY receiver; a group sender is a
  group move (§2023:14.9.25.4 GR4, `cite.py --check 14.9.25.4 "alphanumeric to alphanumeric elementary move"` → OK, 4)) and
  is not touched. Measured results (`9(6)` holding `123456`): `PIC 9(5) PACKED-DECIMAL` over its first three bytes gives
  `00456` (the receiver's cleared bytes are read as zeros); over its last three bytes `23300` — neither the snapshot
  `23456` nor "clear the receiver, then convert" (`23000`), because the second digit pair is formed from the byte the
  first pair has just written; `PIC 9(4)` over bytes 4 to 7 gives `3333` (each digit written is the next one read);
  `PIC 9(3)` over the last three digits of `9(4)` holding `1234` gives `000` (`NUM-MOVE`); an alphanumeric `X(6)`
  sender behaves as the numeric one (`3333`, and `3434` for the receiver over bytes 5 to 8). The sender's operational
  sign is read BEFORE the receiver is written and written back after it, as `libcob` does, so a receiver that covers
  the sender's sign byte ends with the sender's sign punched onto whatever that byte then holds (`MOVE S4 TO S5`, `S4
  PIC S9(4)` holding `-1234` and `S5 PIC S9(5)` over its bytes 2 to 6, gives `01p1p` under the ASCII
  convention and `01}1}` under the IBM one); a PACKED-DECIMAL sender's sign is read from its last byte AFTER the store
  (`P-SIGN`: `S9(7) PACKED-DECIMAL` holding `-1234567` into `S9(4) SIGN IS LEADING SEPARATE` over its first five bytes
  gives `+4567`). The IBM and ASCII conventions both follow `libcob` under `-fsign=EBCDIC` and `-fsign=ASCII`
  respectively. **Scope — only where the overlap is a compile-time fact.** The compiler decides at bind time
  (`MoveOverlap.Classify`, carried on `MoveStore.Overlap`): both operands lie in one storage area (a REDEFINES class, or
  one record) and every offset and length is a constant (`StorageExtent`). An operand under OCCURS, a dynamic or
  variable-length item, a NATIONAL or BIT operand and a reference modification with a run-time bound are not provably
  overlapping and keep the snapshot; a MOVE that cannot overlap emits exactly what it always did. A reference-modified
  RECEIVER is never converted in place (§2023:8.4.3.3.4 6), "the unique data item is considered to be an elementary data item
  without the JUSTIFIED clause", `cite.py --check 8.4.3.3.4 "without the JUSTIFIED clause"` → OK, 6)), and a
  reference-modified numeric item is an alphanumeric one (6) c), `cite.py --check 8.4.3.3.4 "numeric-edited are
  considered class and category national if the usage is national; otherwise they are considered class and category
  alphanumeric"` → OK), so no numeric receiver is a slice. Because Annex A.2 36 b) makes any reference-modified
  operand's overlap undefined, a slice of the receiver itself counts: `MOVE JR(1:2) TO JR`, `JR PIC X(4) JUSTIFIED RIGHT`
  holding `ABCD`, gives four spaces; `MOVE NR(1:3) TO NR` is the alphanumeric-to-numeric family. **Measured GnuCOBOL
  differences not adopted:** (i) GnuCOBOL moves a numeric DISPLAY item into a shorter JUSTIFIED RIGHT alphanumeric item
  keeping its LEFTMOST digits (`MOVE 1234 TO X(3) JUSTIFIED RIGHT` gives `123`), which is not an overlap effect and
  contradicts §2023:13.18.32.4's right alignment; WiseOwl COBOL keeps `234` (the ISO text controls). (ii) The sign of a ZERO
  result: GnuCOBOL keeps the sender's sign on a zero result in EVERY MOVE, overlapping or not (`MOVE -1000 TO PIC S9(3)`
  stores `00p`, a negative-signed zero, and DISPLAY shows `+000` and `IF < 0` is false on both compilers), and WiseOwl
  COBOL stores a positive zero (`00{` under the IBM convention). That is a representation of the sign of zero, not an
  overlap effect; it is documented with the SIGN encoding determinations (Annex A.1 items 177 and 178), and the
  in-place conversions above reproduce the library's negative zero only because they reproduce its sign handling.
  (iii) A numeric-edited receiver: `cob_move_display_to_edited` scans the picture while it reads the sender and
  back-patches positions it has already written (floating insertion, sign symbols), so its result for a receiver to the
  right of the sender's digits depends on the order of those steps and is not a rule that can be stated beside the
  editing one; where the receiver does not clobber an unread digit (`3456` for `ZZZ9` over either end of `123456`) it
  agrees with the snapshot, and the shapes that differ are listed in kb/Work/PB1910. (iv) A PACKED-DECIMAL sender into a
  PACKED-DECIMAL receiver: `cob_move_bcd` clears the receiver and then calls `memcpy` over overlapping storage, which the
  C standard leaves undefined and whose result depends on the C library, not on a rule. (v) Under the IBM convention
  only (`-fsign=EBCDIC`), a signed overpunched DISPLAY sender into a BINARY receiver that covers the sender's sign byte:
  the library re-punches that byte after the binary store; the ASCII convention, which is GnuCOBOL's default, has no
  such step and agrees with the snapshot. (vi) A P-scaled operand whose digit positions share no decimal position with the other operand's, in a DISPLAY-to-PACKED-DECIMAL move (`9(6)` into `PP9(3) PACKED-DECIMAL`, `PP9(4)` into `9(5) PACKED-DECIMAL`): `cob_move_display_to_packed` then reads the bytes past the sender (storing their nibbles) or writes past the receiver (corrupting the storage after it). That is not a conversion any rule describes, so the snapshot stands (`MoveOverlap.SharesADigitPosition`; `PP-OUT`, `PP-LOW`). Where the windows do share a position the library counts the STORED digits (a P position stores no byte, it moves the scale), and the in-place algorithm agrees with it (`PP-TAIL`, `PP-SEND`, `PP-PACK`; a 320-case P-scaled matrix, every DISPLAY and PACKED-DECIMAL sender and receiver shape at five offsets, differs from the library only in (iv) and in this disjoint case). Implemented in `MoveOverlap` (classification), `StorageExtent` (the overlap
  fact, on `RecordLayout.OffsetInRecord`'s walk), `MoveEmitter.EmitOverlapStore` (the one rendering) and
  `OverlappingMove` (the four algorithms), held by `MoveOverlapDriftTests` and `OverlappingMoveTests`. Witnesses:
  `conformance:cases/14.6.10-Overlapping-operands/pb1907_overlap_move` (`JUST`, `JUST-SELF`, `JUST-GRP`, `JUST-IN`,
  `JUST-LEAD`, `NUM-MOVE`, `NUM-SELF`, `ALNUM-NUM`, `SLICE-NUM`, `NUM-NUM`, `NUM-SCALE`) and
  `conformance:cases/14.6.10-Overlapping-operands/pb1910_overlap_in_place_move` (`PK-HEAD`, `PK-TAIL`, `SMEAR-N`,
  `SMEAR-X`, `SMEAR-X4`, `P-SIGN`, `PP-TAIL`, `PP-SEND`, `PP-PACK`, `PP-OUT`, `PP-LOW`). Every line of those goldens, and a matrix of 1,260 sender-by-receiver-by-offset
  cases over unsigned, signed, separate-sign, scaled, PACKED-DECIMAL and alphanumeric senders and DISPLAY, PACKED-DECIMAL
  and BINARY receivers, was also run through GnuCOBOL 3.2.0 (`cobc -std=cobol2014`, ASCII and `-fsign=EBCDIC`), and the
  outputs are identical except for the families (iii) to (v) and three pairs whose NON-overlapping control already differs in representation.
