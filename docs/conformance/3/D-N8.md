- **D-N8 — a group sender into a national DYNAMIC LENGTH receiver** (kb/Work PB546; decision PB3099, 2026-10-12).
  §2023:14.9.25.4 GR4 makes the move a byte copy and §2023:8.5.1.10.4 sets the receiver's new length to the content's. An odd
  byte count cannot be national content (D-N1: two bytes per position), so the last position is completed with the
  alphanumeric space GR4's elementary-move fill supplies, as a fixed national receiver already is: `MOVE G3 TO DN` with
  a three-byte `G3` leaves `DN` two positions long and `FUNCTION LENGTH(DN)` = 4 (§2023:15.50.4 r6 counts bytes). GnuCOBOL
  3.2 has no dynamic-length items. Implementing site: `ReceivingStore.ContentCharacters` over `CobolBits.NatFromStorage`
  (also the READ of an elementary dynamic-length national record). Witness:
  `conformance:cases/13.18.60-USAGE/pb546_group_into_national_dynamic`.
