- **D-B5 — a boolean item RECEIVING characters converts each one: '1' is boolean one, every other character boolean
  zero** (kb/Work PB3666; owner decision 2026-10-24, on a point of implementor latitude). Table 16 (§2023:14.9.25.3) admits
  an alphanumeric or national sending operand against a boolean receiving operand, and §2023:14.6.8.6 transfers the data
  "after any necessary conversion, into the corresponding boolean positions of the receiving data item, with zero fill
  or truncation to the right" (JUSTIFIED per §2023:13.18.32). But a boolean character is "the value zero or one" (§2023:3.21), no rule
  converts a character that is neither, and §2023:14.6.13.2 1) calls a later reference to non-boolean content undefined with
  EC-DATA-INCOMPATIBLE, so the standard leaves the conversion to the implementor. Before the determination a MOVE of
  `"AB"` to `PIC 1(5)` stored the characters themselves, so DISPLAY showed `AB000` while `B-NOT` of the same item
  gave `11111` (the operators read a position outside `{'0','1'}` as zero). WiseOwl COBOL converts EACH transferred
  character at the store: `'1'` to boolean one and every other character (a `'0'`, a letter, a space, a national
  letter) to boolean zero, then aligns with zero fill or truncation to the right (left, under JUSTIFIED). It is the
  rule the operators already apply and the rule an ACCEPT into a boolean item applies (§2023:14.9.1.4 GR1,
  `AcceptSource.DeviceBoolean`), so DISPLAY of the item and the operators over it agree for both usages (USAGE DISPLAY
  `PIC 1` and USAGE BIT) and every other character store inherits it. GnuCOBOL 3.2 gives no precedent (probed 2026-10-24: it
  warns that USAGE BIT is not implemented and stores a `PIC 1(5)` item in one byte, and its USAGE DISPLAY `PIC 1` moves
  text as a number, right-aligning `"101"` as `00101` against §2023:14.6.8.6's zero fill to the right); IBM Enterprise COBOL
  has no boolean category. The paths that deposit characters into a boolean item and now convert: the elementary
  MOVE and every statement that MOVEs (READ INTO, MOVE CORRESPONDING, INITIALIZE ... REPLACING BOOLEAN DATA, a CALL
  argument landed by §2023:14.2.3 GR9, the element MOVE of a variable-length group MOVE between corresponding dynamic-capacity
  tables, §2023:14.6.9.2), a reference-modified boolean receiver, STRING into a USAGE DISPLAY boolean item (§2023:14.9.43.4 GR3 a)),
  a group sender into a USAGE DISPLAY or USAGE NATIONAL boolean item (§2023:14.9.25.4 GR4's alphanumeric move; into a display
  item it zero-fills where it used to space-fill), a READ or RETURN into an elementary boolean record area that no other
  description shares (§2023:14.9.30.4 GR13 c); §2023:14.9.34.4 GR3; a record area shared with another record of the FD
  (§2023:13.18.33.4 GR3) or with a SAME RECORD AREA peer (§2023:12.4.6.4.4 GR2) holds the record's bytes for every description and a
  READ leaves them as they are, as the restore of an APPLY COMMIT item does, §2023:14.9.36.4 GR3), and COMPUTE's boolean result. **The GR4 arm converts the DECODED characters, never the
  bytes:** GR4 has "no conversion of data from one form of internal representation to another", so the sender's BYTES
  fill the receiving area unchanged, and the item's characters are then decoded from them (a national item's UTF-16BE
  pairs, `ReceivingStore.StorageCharacters`) and converted like any other character store (`MOVE GN TO BN` with GN holding
  `N"101"` stores `101`; `MOVE GA TO BN` with GA holding `A1Z1` stores `0000`, because the pairs x4131 x5A31 x2020 x2020
  are no national `1`). Converting the bytes before decoding them is the defect PB3666's review R1-1 removed. **The one
  exemption is USAGE BIT:** a GROUP sender into a USAGE BIT item leaves the item's bits the first bits of the sender's bytes
  (`MOVE GA TO BB` with GA holding `1A` stores the bits of x"31", `00110`), and every bit pattern is a valid boolean value,
  so DISPLAY and the operators agree there without a character conversion. Every STORE into a boolean item converts its
  characters. That includes a GR4 group move (the decoded characters of a national or display receiver; a USAGE BIT
  receiver's bits are all valid) and a READ or RETURN into an elementary record area that no other description shares. A
  boolean item can still hold a character other than 0 or 1 only through another description of the same storage, which no
  store into the item reaches: a group receiver (GR4 moves the group's bytes), a REDEFINES view, a record area shared with
  another record of its file or with a SAME RECORD AREA peer (a READ or RETURN fills the area's bytes), an APPLY COMMIT
  restore (ROLLBACK returns the saved image), or storage shared through a BY REFERENCE argument,
  EXTERNAL, BASED or ADDRESS OF. That content is undefined (§2023:14.6.13.2 1)), and the boolean operators read such a position
  as zero. (`MOVE "A1Z" TO G` leaves `A1Z` in a boolean member of the group G.) VALUE cannot reach it (§2023:13.18.63.3 SR10: the literals of a boolean item's VALUE clause are boolean
  literals), UNSTRING refuses a boolean receiver (§2023:14.9.48.3 SR4) and a .NET host's string landing into a boolean formal is
  REFUSED when it holds a character other than `0` or `1` (`HostLanding.RequireClass`). Written once, in
  `CobolBool.FromCharacters` and its fitting store `CobolBool.Store` (`RuntimeApi.StrStoreBoolean` is the only emitter of
  that store); witnessed by `conformance:wiseowl/cases/14.9.25-MOVE/pb3666_boolean_receiver_converts_characters` and
  `conformance:wiseowl/cases/14.9.25-MOVE/pb3666_dynamic_table_group_move`, and `unit:CobolBoolTests.Store_ConvertsEachCharacterThenFits`.
