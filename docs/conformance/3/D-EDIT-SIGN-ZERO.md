- **D-EDIT-SIGN-ZERO — The editing sign of a value the store truncates to zero (§2023:13.18.40.5 5), §2023:14.9.25.4 6), §2023:14.6.8.2 5); kb/Work
  PB3664, owner 2026-10-24).** An editing sign control symbol (the fixed `+`, `-`, `CR`, `DB`, the floating `+` and
  `-`, and a PICTURE EDITING character-1 under a FOR phrase) shows the Table 8/Table 9 column for "the value of the data
  item", whose first column is "Positive or zero value", and the data item holds the value AFTER alignment and
  truncation (§2023:14.6.8.2 5), for a numeric-edited receiver: "further alignment, zero fill or truncation, and transfer of
  digits take place as described in the general rules and editing rules in 13.18.40"). So `MOVE -0.001 TO PIC -9.99`
  stores zero and prints ` 0.00` (`+0.00` for `PIC +9.99`, `0.00  ` for `PIC 9.99CR`), and `MOVE -100 TO PIC -99`, whose
  high-order digit truncates, prints ` 00` (§2023:8.8.4.2.4 says the same of comparisons: "Zero is considered a unique value
  regardless of the sign"). The sign is decided
  ONCE per edit, from the digit string the receiver holds (`CobolEdit.Render`), and the format-2 locale editor
  (`CobolLocaleEdit.Format`, GR18 `+`) reads the same stored value. GnuCOBOL 3.2 agrees for the edited receiver
  (`cob_move_display_to_edited` guards every `-`, CR and DB with `neg && !is_zero`, `is_zero` taken from the digits that
  land in the picture). A signed NUMERIC receiver is the adjacent case and already agrees with the same choice
  through the carrier, which has no negative zero: `MOVE -100 TO PIC S9(2)` stores `+00` (GnuCOBOL stores a
  negative-signed zero there and DISPLAYs `+00`, the representation difference documented with the sign-encoding
  determinations at Annex A.1 items 177 and 178).
  Witnesses: `conformance:cases/13.18.40.5-Editing-rules/pb3664_edited_sign_stored_value`, `CobolEditStoredSignTests`.
