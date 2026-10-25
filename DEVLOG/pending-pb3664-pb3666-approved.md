## The owner approves the fourth fuzz night's three findings for fixing (PB3664 to PB3666)

The operator explained each finding: the ISO text, what it requires, and what GnuCOBOL 3.2 does and why, read in the
pinned 3.2 source. The owner then wrote (2026-10-24 ~18:00 PDT): "Approve all three for fix, and make DISPLAY and the
boolean operators agree, and record the choice in docs/CONFORMANCE.md". The three notes go from `status: owner` to
`open` in a cluster of their own, led by PB3664, so that one planned wave can take them beside the loop's PB1957
campaign.

- PB3664, the fixed editing sign, keeps its derivation. GnuCOBOL's `cob_move_display_to_edited` writes `-`, CR and DB
  only on `neg && !is_zero`, the zero read from the digits that land in the edited picture.
- PB3665 is now a defect with the decided direction: refuse both integer-function forms. §14.9.43.3 SR1 requires usage
  display or national, and §15.2 gives integer functions no usage. The folded `FUNCTION LENGTH` arm must give the
  identifier rule as its reason, never "a numeric literal". GnuCOBOL is no precedent here: its STRING check never looks
  at a function call (`default: break;`), and `FUNCTION INTEGER` stores the raw bytes of a 4-byte binary result.
- PB3666 is now a defect with the owner's choice. The MOVE converts each character, '1' to boolean 1 and any other to 0,
  so DISPLAY and the boolean operators agree. The choice is recorded in `docs/CONFORMANCE.md` because the standard
  defines no conversion. GnuCOBOL has no precedent: USAGE BIT is "not implemented", and its display `PIC 1` moves text as
  a number, right-aligned, against §14.6.8.6.
