## Entry 3720 — 2026-11-15 20:11 PST — STRING / UNSTRING pointer operands judged as bound and sized; a national group triggers INSPECT SR4 (PB5734, PB5607, PB5646)

Three defects in two binders, each a rule asked of the wrong thing.

**PB5734.** `StringUnstringBinder.StrUnstrIsInteger` read `p.Item.Pic`, the base item's PICTURE, through a reference
modification, so `UNSTRING ... COUNT IN CN (2:1)`, `TALLYING IN TN (1:1)`, `WITH POINTER PT (2:1)` and STRING's
`WITH POINTER PT (2:1)` passed the binder and failed the generated C# (CS1503, `long` to `string?`) with no COBOL diagnostic.
Re-probe on this build before the fix: the same CS1503 for `c5b.cob`. A reference modification of a numeric item is
alphanumeric (§2023:8.4.3.3.4 GR6), so it is no integer item. The fix is the PB5384 shape: `StrUnstrIsInteger` asks the one
integer classifier `IntrinsicResultType.IsIntegerOperand` of the operand as bound, then the no-P test. All four arms share
the one helper, so each is fixed. After: COBOL1651 at 85, 2002 and 2023.

**PB5607.** `BindString` never asked the second half of SR7 (§2023:14.9.43.3 SR7; §1985:VI.6.25.3 SR5; §2002:14.8.39.2 SR6):
identifier-4 must be "of sufficient size to contain a value equal to 1 plus the size of the data item referenced by
identifier-3". UNSTRING's identifier-7 has the same sentence against identifier-1 (§2023:14.9.48.3 SR6; §1985:VI.6.27.3 SR5),
so the sibling arm is fixed too. `PointerTooSmall` measures the size through `RefModPlace.PositionCount` (or a slice's
`StaticLength`) and the pointer's capacity through the new `PicInfo.CanHoldUnscaledValue` (PICTURE digits; a BinaryCapacity
item owns its container range). A size that is a run-time value (ANY LENGTH, DYNAMIC LENGTH, a run-time slice) decides
nothing at compile time and is not refused. Before: `PIC 9` pointer, 10-character receiver compiled and ran, printing OVF.
After: COBOL1651 at every edition.

**PB5646.** INSPECT SR4's trigger is "an elementary data item or literal of class boolean or national", and `Sr4Entry` read
"elementary" as "not a group", so a `GROUP-USAGE NATIONAL` group never triggered the all-national rule. §2023:13.18.29.4 GR2b
(§2002:13.16.27.3 GR2b) treats a national group as an elementary item of class national, and GR1b does the same for a bit
group. New `ItemCategory.IsElementaryOrBitNationalGroupReference` carries it; `Sr4Entry` asks it. `INSPECT NG TALLYING C1 FOR
ALL "A"` is now COBOL2306 at 2002, 2014 and 2023; `ALL N"A"` is accepted and tallies 5. STRING and UNSTRING use no trigger
list (they ask the class of every operand), so they needed no change.

**Goldens.** Refusals (all four editions unless noted): pb5734-string-pointer-refmod, pb5734-unstring-pointer-refmod,
pb5734-unstring-count-refmod, pb5734-unstring-tallying-refmod, pb5607-string-pointer-too-small (and its 2002 twin under
14.8.39@2002), pb5607-unstring-pointer-too-small, pb5646-inspect-national-group-literal (2002, 2014, 2023; and its 2002 twin).
Acceptances: pb5607-string-unstring-pointer-fits (the boundary, PIC 9 against 8 characters, 85 to 2023),
pb5646-inspect-national-group-national-literal.

**Rows.** `SR-14.9.43.3-7`, `SR-14.9.48.3-5`, `SR-14.9.48.3-6` PARTIAL to CONFORMS (GAP 9 to 6); `2002-SR-14.8.39.2-6` and
`2002-SR-14.8.21.2-4` recorded CONFORMS (2002 GAP 1805 to 1803).

**Self-review.** Architecture: one size predicate in the binder, one capacity predicate in PicInfo, one elementary-or-
bit-or-national predicate beside `IsElementaryReference`; no C# text in a bound node. Code: each citation was run through
`cite.py --check`. Duplication: STRING and UNSTRING share `PointerTooSmall`.

Review round 1 (R1-1): INSPECT SR2 (identifier-3..n) asked `IsElementaryReference`, so a GROUP-USAGE NATIONAL group as a TALLYING/REPLACING operand was refused COBOL0847 while SR4 in the same statement already treated it as an elementary national item (§2023:13.18.29.4 GR2b). `StatementValidation.IsInspectElementaryCharacterItem` (the one elementary arm of SR1 and SR2) now asks `ItemCategory.IsElementaryOrBitNationalGroupReference`; a bit group stays refused by the usage half. Golden `pb5646-inspect-national-group-operand` (output 03). R1-2 (lead): golden `pb5607-string-pointer-binary-container` reaches the `CanHoldUnscaledValue` container arm (COMP-5 PIC 99 and BINARY-CHAR UNSIGNED pointers over PIC X(200)).

