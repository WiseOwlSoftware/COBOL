- **D-DEBUG-STORE — a program's store into DEBUG-ITEM or a member** (kb/Work PB4858, review round 2 R2-3). General rule 11 says how the facility
  updates the members "immediately before control is passed to that debugging section"; no rule of the 1985 Debug module bars a program's own
  store (it states the bar where it means one: LINAGE-COUNTER may be changed only by the input-output control system), and syntax rule 11 restricts
  only where the register is referenced, within a debugging section. **Determination (ISO silent, so GnuCOBOL 3.2, which compiles and stores):**
  `MOVE`, `STRING` and the other receiving statements accept DEBUG-ITEM, each member and a reference-modified DEBUG-CONTENTS as receivers; the store
  lands in the register (`DebugItem.StoreImage`, `StoreLine`, `StoreName`, `StoreSub`, `StoreContents`, fitted to the member's width) and stands until
  the facility updates the register for the next section. Witness: `conformance:cases/_unassigned/pb4858-debug-register-receiving`.
  **DEBUG-ITEM is a GROUP (kb/Work PB4903; the text decides, no latitude).** General rule 10 describes `01 DEBUG-ITEM` with the six members and the
  one-character FILLER entries between them, so the item is a group item whose entries are those eleven: a store into the whole item lands in the
  FILLER entries too (`DebugItem.StoreImage`), INITIALIZE walks the entries by category (ISO/IEC 1989:2023 §2023:14.9.20.4 GR4, GR5 and GR6: the
  DEBUG-SUB-n entries become zero, the alphanumeric entries spaces, the FILLER entries are left alone), and CORRESPONDING takes it as a group operand
  (§2023:14.9.25.3 SR12 is met; no user item can share a member's name, DEBUG-* being reserved at COBOL-85, so no pair exists and the statement changes nothing). This differs from GnuCOBOL 3.2, which models the register as one
  alphanumeric item; the text controls (CLAUDE.md rule 1). Witness: `conformance:cases/_unassigned/pb4903-debug-item-group`
  (refused from 2002: `pb4903-debug-item-group-removed-2002`).
