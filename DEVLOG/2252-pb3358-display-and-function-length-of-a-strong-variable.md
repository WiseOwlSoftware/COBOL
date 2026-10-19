## Entry 2252 — 2026-10-19 03:05 PDT — PB3358 - DISPLAY and FUNCTION LENGTH of a strong variable-length group holding a pointer or object-reference leaf

**What was wrong.** `DISPLAY` of a strongly-typed variable-length group (a dynamic-length item, or a dynamic-capacity
table, beside a POINTER, PROGRAM-POINTER or OBJECT REFERENCE leaf, section 13.18.60.3 SR14) compiled clean and aborted at run time
with the Tier-C "whole-group image of ... pointer/object-class leaf" island; the EXTERNAL and ADDRESS-OF cell forms aborted
with the cell twin of the same loud, and `FUNCTION LENGTH` of a table of variable-length elements holding the leaf staged its
named Tier-C. Section 14.9.11.4 GR7 leaves the format of a variable-length group to the implementor, and DOC-A.1-57 had defined
it for every other variable-length group.

**Root cause.** One capability answered two questions. `DataItem.CurrentExtentImageCapable` gates the DISPLAY format, LENGTH and
the section 8.5.1.12 component carrier; its member law tested `IsImageCapable`, which excludes a pointer/object leaf, so the
format was refused for a reason only the carrier has (the placeholder has no inverse, so the receiving side cannot store it
back). PB244 had split the fixed-group arm (`TransferImageCapable` beside `IsImageCapable`); the variable-length arm never was
split, which is the repository's two-arm dispatch shape.

**Fix.** `DataItem.CurrentExtentTransferImageCapable` is the one-way twin. The member law (`CurrentExtentComposes`) stays ONE
function and takes a `transfer` flag for its fixed-image tests, so the two capabilities cannot classify a member differently.
`GroupImageCodec.VarParts` and `DynElementImage` classify by the one-way predicates (identical to the two-way ones for every
group without a slot leaf), `RecordStructEmitter` emits `CurrentImage()` for the wider capability and the
`AsVarImage`/`FromVarImage` pair only for the narrower, and `PlaceRenderer.VarGroupCurrentImage` takes `transfer:` (the twin of
`GroupImage`'s flag) through its declared and cell-window arms. DISPLAY, FUNCTION LENGTH and BYTE-LENGTH pass it. MOVE,
comparison, CALL and a record keep the carrier capability, so PB3139's refusals stand. A pointer leaf shows its A.1 item 216
storage image (NULL as eight X"00"), an object reference its D-SLOT placeholder (eight spaces), exactly as in a fixed group.

**Re-probe and sweep.** `WHOLE.cob` (the note's repro) reproduced on a fresh build of main b349e6d44 (`LEN 0015`, then the
abort) and now prints `G=hello` + the eight pointer positions + `05`. Shapes probed after the fix, each width checked by hand
against DOC-A.1-57: a nested fixed strong group, a nested variable-length one, a fixed table of references, an OCCURS DEPENDING
table of fixed and of variable-length elements, a dynamic-capacity table of fixed and of variable-length elements, a fixed table
of variable-length elements, one subscripted element of each, POINTER and PROGRAM-POINTER leaves, and the EXTERNAL and
ADDRESS-OF cells. A BASED variable-length group is refused at its declaration (COBOLNET2560). FUNCTION UPPER-CASE of a
variable-length group stays refused (COBOLNET1627); under `--permissive` its coercion aborts at run time with the Tier-C island
for a plain variable-length group as well (no reference leaf needed), which is a separate lead.

**Goldens.** `pb3358_vlg_display_reference_leaf` (2014), `pb3358_vlg_display_reference_leaf_cell` (2023: strongly-typed EXTERNAL
is a 2023 feature), `pb3358_vlg_display_pointer_leaf` (2014), the negative `pb3358-vlg-display-reference-leaf-at-2002` (COBOLNET0900
at 85 and 2002), expected output derived from DOC-A.1-57/214/216 and confirmed against the build. The drift test
`VariableLengthBoundaryDriftTests.CurrentExtentTransferCapability_IsDefinedOnceOverTheSameMemberLaw` keeps the capability single
and its consumers on the right side. Citations checked with `cite.py --check`: 14.9.11.4 GR7, 13.18.60.3 SR14, 13.18.60.4 GR22 a)
and GR23, 13.18.63.4 GR4, 8.5.1.12.1, 15.50.4 rule 7. DOC-A.1-57 and the data-model design carry the new capability.

**Merge with PB3273.** Between the first implementation and this finish, PB3273 landed `DataItem.CurrentExtentAreaCapable`, the
identical member law (a reference a fixed placeholder, via `TransferImageCapable`) for the host entry's cell area. Two properties
over one law is the duplication this repo forbids, so the finish merged them: `CurrentExtentTransferImageCapable` is the one
capability, `ComposesCurrentExtent(transfer)` the one function, and the host-entry and published-record callers, the external
repository design doc and the PB3273 note were renamed to it.

**Gate.** `build-local.ps1 -Priority BelowNormal`, run 20261019T084208Z-7a96d8: `LEG 1 ONLY (batched gating, PB2515): GREEN`,
Conformance 11,384/17,952, Unit 33,994/34,641, Characterization 5/36 cases ran. The new goldens passed in
`leg-1-Conformance.trx` (pb3358_vlg_display_reference_leaf 2014, pb3358_vlg_display_reference_leaf_cell 2023,
pb3358_vlg_display_pointer_leaf 2014, refusal pb3358-vlg-display-reference-leaf-at-2002 at 85 and 2002); the drift test ran in
leg 1 of Unit. `scripts/semgrep/verify.py`: PASS, no rule count raised. Linux gate: the lander's (batched gating).

**Finisher: the landing's Linux gate went RED, and a missed shape.** The landing stopped `GATE RED (Linux)` on 161bacdc3:
`NulByteDriftTests.NoTrackedTextFileContainsANulByte` found the golden `pb3358_vlg_display_pointer_leaf.out` holding the NULL
images (eight X"00" per pointer leaf) byte for byte. A pointer's image ALWAYS holds zero positions (NULL is all zeros and an
address token's high-order positions are zero), so no golden can pin a displayed pointer leaf. The case was re-expressed: its
DISPLAYs show each group at an extent with no pointer occurrence (an OCCURS 0 TO 3 DEPENDING table at count 0, section 13.18.38.4
GR8 b; a dynamic-capacity table at its initial capacity zero, section 8.5.1.9.1 item 3), and FUNCTION LENGTH counts the 8 positions
of each leaf at non-zero extents. The pointer positions' characters moved to `VariableLengthGroupPointerImageTests` (Conformance
assembly), whose expected strings are built from the determination: a fixed leaf and fixed table, an OCCURS DEPENDING table, and
a dynamic-capacity table of program-pointers and of object references. Writing that last shape found a defect of this branch: a
dynamic-capacity table whose ELEMENT is a pointer or object leaf failed the backend compile (CS0029, the element lambda
`static __e => __e` returned the managed pointer as a string), where main had refused it loudly at run time.
`GroupImageCodec.DynElementImage` now gives a slot-carried element its 8 positions exactly as `SlotImageOf` does for a fixed table
(the pointer's `PointerImage.Of`, the object's placeholder). Why the Windows gate missed the NUL: the three tree-walking facts of
`NulByteDriftTests` are recorded at 2.4 to 25 s, over `SWEEP_CHEAP_SECONDS` (2.0 s), so tier 0c leaves them in leg 2 on Windows
and only the landing's Linux whole-population unit leg runs them (kb/Work PB3112, open; PB3155 for the re-gate).

