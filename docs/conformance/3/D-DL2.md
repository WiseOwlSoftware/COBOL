- **D-DL2 — the SIZE of a dynamic-length RECEIVING operand is its maximum size** (2026-09-22; kb/Work PB871).
  §2023:8.5.1.10.4 fixes a dynamic-length item's size only where it is a SENDING operand or reference-modified —
  *"treated as a fixed-length data item whose length is the dynamic-length elementary item's current length"* — and
  for a receiving operand gives the new CONTENT (*"the new value becomes the content of the item"*), not a size.
  Three statements are written over the receiver's size itself: STRING (§2023:14.9.43.4 GR6/GR8, *"the number of
  character positions in the data item referenced by identifier-3"*), UNSTRING without DELIMITED BY (§2023:14.9.48.4
  GR11b, *"the size of the current receiving area"*) and ACCEPT format 1 (§2023:14.9.1.4 GR3/GR4). **WiseOwl COBOL reads
  that size as the item's MAXIMUM size (§2023:8.5.1.10.1)**: an item "for which the number of character positions
  allocated to the data item may vary during program execution" can receive as many as it may hold. For STRING,
  GR7 (*"only the portion of the data item referenced by identifier-3 that was referenced during the execution
  of the STRING statement is changed"*) still governs, so the new length is the old content's, extended through
  the last position written, and a position between the old end and the pointer is a space — the character
  §2023:14.9.39.4 GR39 gives every position a dynamic-length item grows by. **Rejected reading:** the CURRENT length,
  which makes an empty dynamic-length item unwritable by STRING (every character an overflow) and makes UNSTRING
  examine zero characters. Written once for the emitters (`ReceivingStore.DynamicReceivingSize`, beside the one
  elementary character receiving store `ReceivingStore.Characters`) and read through the ONE receiver-size reader
  `ReceivingStore.CharacterPositions` — by ACCEPT format 1's device transfer (kb/Work PB1013, which also answers an
  ANY LENGTH receiver's size with its argument's, §2023:13.18.2.4 GR1 b)) and by UNSTRING's examination size
  (`ReceivingStore.ExaminationSize`, kb/Work PB979); witnessed by `conformance:cases/14.9.43-STRING/pb871_dynamic_length_receivers`
  (STRING into an empty item, into a longer one, through a grown gap and past the maximum; UNSTRING with and
  without DELIMITED BY).
