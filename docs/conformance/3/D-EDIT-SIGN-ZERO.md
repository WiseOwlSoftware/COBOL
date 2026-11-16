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
  land in the picture). The editing sign follows the stored value at every edition: the 1985 PICTURE clause's editing sign
  table heads the same first column `DATA ITEM POSITIVE OR ZERO`, and a negative zero's value is zero. A signed NUMERIC receiver is the adjacent case. From
  COBOL-2014 it agrees with the same choice: §2014:14.9.24.3 GR6d2a and §2023:14.9.25.4 GR6d.2.a represent "the sign
  of the numeric value", so `MOVE -100 TO PIC S9(2)` stores `+00` (GnuCOBOL stores a negative-signed zero there and
  DISPLAYs `+00`, the representation difference documented with the sign-encoding determinations at Annex A.1 items
  177 and 178). At COBOL-85 and COBOL-2002 it does not: §1985:VI.6.19.4 GR4b1 and §2002:14.8.24.3 GR4b1 place "the
  sign of the sending operand", so the same MOVE stores a NEGATIVE zero (`-00`; the edition arm of VCR row 7.85, kb/Work
  PB5852, DESIGN-data-model §2.1.1) in a signed USAGE DISPLAY (or NATIONAL) receiver, whose sign is a character apart
  from its digits. A signed BINARY or PACKED-DECIMAL receiver stores the value's sign at every edition: this
  implementation represents the algebraic sign of those usages as the value's sign (§1985:VI.5.14.4 GR9 leaves "the
  representation of any algebraic sign" to the implementor), so it has no negative zero.
  Witnesses: `conformance:cases/13.18.40.5-Editing-rules/pb3664_edited_sign_stored_value`, `CobolEditStoredSignTests`,
  `conformance:cases/VI.6.19@1985-MOVE-STATEMENT/pb5852_move_sign_of_sending_operand` (1985) and `conformance:cases/14.9.25-MOVE/pb5852_move_sign_of_numeric_value_2023` (2023).
