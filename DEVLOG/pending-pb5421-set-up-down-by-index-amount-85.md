## PB5421: at --std 85 SET UP BY / DOWN BY refuses an index-name or USAGE INDEX amount

**Re-probe.** On a fresh Debug build, `SET IX UP BY IDX` (IDX USAGE INDEX), `SET IX UP BY IY` and `SET IX DOWN BY IDX` (IY an index-name) compiled and ran at `--std 85` and printed OK, as the note said.

**Root cause.** §1985:VI.6.23.3 SR3 requires identifier-3 to reference an elementary numeric integer. The one 85 amount gate, `SetBinder.IsCobol85IndexAmount`, refused a scaled item, a non-integer literal and an expression, but its data-reference arm never asked whether the reference was an index-name or an index data item, which the 2023 binding row admits.

**Fix.** `IsCobol85IndexAmount` now returns false for an index-name or index data item when asked for the UP/DOWN amount (`upDown`), so the edition row (COBOL0900, `set-arithmetic-expression-2002`) refuses it. Format 1's identifier-2 keeps admitting both (§1985:VI.6.23.3 SR2), so the TO form is untouched. No new diagnostic code.

**Goldens.** `pb5421-set-up-by-index-amount-2002` (accepted 2002 and later, prints C then E), `pb5421-set-up-by-index-name-at-85` and `pb5421-set-down-by-index-data-item-at-85` (COBOL0900 at 85). Row `1985-SR-VI.6.23.3-3` re-verdicted PARTIAL to CONFORMS (GAP 169 to 168).

**Sibling.** PB5177 is the same admission in PERFORM VARYING's BY slot, already landed.
