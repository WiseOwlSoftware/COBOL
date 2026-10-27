## Entry 2650 — 2026-10-26 19:47 PDT — PB3697: every storage-area store into a boolean item converts the decoded characters, and the three texts of the rule are one sentence

PB3666 (determination D-B5) made every store of characters into a boolean item convert them, `'1'` to one and anything else
to zero, so DISPLAY, comparisons and the boolean operators agree. Its text named two paths that leave non-boolean content in
an item (a group receiver, a REDEFINES view) and two exemptions; the DATA_MODEL_DESIGN.md D19 paragraph, D-B5 and the
`StrStoreBoolean` summary each repeated a version of that list, and PB3666's own review fix added a third bypass nobody wrote
down. The adjudication (mythos lane, 2026-10-25) found a fourth and recommended closing the code, not lengthening the list.

**Re-probe on a fresh build (all reproduced).** `MOVE GA TO BN` with GA holding `A1Z1` and BN `PIC 1(4) USAGE NATIONAL` left four
CJK characters in BN (DISPLAY showed them, `BN = ZERO` was false, `B-NOT BN` gave 1111). A READ of `A1Z1B101` into an
elementary `PIC 1(8)` record area showed `A1Z1B101` and `B-NOT` gave 10101010; into a national `PIC 1(4)` record it showed four
CJK characters. A CALL BY REFERENCE of `PIC X(4)` into a boolean LINKAGE item shows `A1Z1`: another description of the same
storage, which no store reaches.

**The fix is at the one place a storage area is decoded.** §14.9.25.4 GR4 (checked with `cite.py`) has "no conversion of data
from one form of internal representation to another", so the receiving AREA takes the sender's bytes unchanged. What the
boolean item then holds is the characters those bytes decode to, and converting those is D-B5's normalization of undefined
content (§14.6.13.2 1)), not a representation conversion. Converting the raw bytes before decoding was PB3666's review R1-1 defect
(`N"101"` became three CJK characters). `ReceivingStore.StorageCharacters` now decodes (the new private `DecodedStorage`:
national UTF-16BE pairs, USAGE BIT packed bits, else the bytes) and, for a display or national boolean item, converts the
decoded characters through `RuntimeApi.BoolFromCharacters`; USAGE BIT is exempt because every bit pattern is valid. The
callers did not change: `StorageArea` (the GR4 group move into an elementary place), `MoveEmitter.GroupRecord` (the
record-value receiver) and the READ / RETURN elementary record area (§14.9.30.4 GR13 c), §14.9.34.4 GR3) all decode through it,
so one arm closes the whole-item national group move and the READ / RETURN into an elementary boolean record, display and
national (the READ / RETURN only while no other description shares the area: see the review round below). `ReceivingStore.Characters` lost its boolean arm (`AreaHoldsCharacters` guarded it): its only boolean callers are
those storage-area flows, the fit pads with spaces, and the conversion makes every space a boolean zero, which is the §14.6.8.6
zero fill, so converting there too would have converted twice. `SliceStorageArea` decodes through `DecodedStorage` because the
splice in `PlaceRenderer.Write` already converts a boolean slice.
**PB3698 folded in (the same members).** The note asked `SliceStorageArea` to call `AreaHoldsCharacters` instead of spelling
the `Usage.National or Usage.Bit` test, and a drift test to pin the predicate to the decoding arms. Both are done:
`EveryStoreIntoABooleanItem_ConvertsItsCharacters` now requires the predicate's one spelling, the slice arm's call to it,
`DecodedStorage`'s two decoding arms, the conversion arm in `StorageCharacters` and the absence of a boolean store in
`Characters`. PB3698 is landed.

**One text.** The determination sentence is now word for word in `RuntimeApi.StrStoreBoolean`, DATA_MODEL_DESIGN.md D19 and
CONFORMANCE.md D-B5 (the "Two things bypass it", "Two exemptions" and "The exemptions" wording and the national exemption are
gone; the BY REFERENCE, EXTERNAL, BASED and ADDRESS OF aliases are named as the same class as REDEFINES). The new
`BooleanStoreDeterminationDriftTests` pins the sentence in all three places and that the old wording does not return. It is its
own class because reading two documents makes a drift class a tree-wide sweep (`drift_rules.py` reads any path above three
segments that way), and that put the first gate RED in `HostWitnessTraitDriftTests`; keeping it apart leaves
`ReceivingStoreDriftTests` a specific rule.

**Golden.** `pb3666_boolean_receiver_converts_characters` gained L13 (the group `A1Z1` into a national boolean: 0000, `B-NOT`
1111), L14 (a READ of `A1Z1B101` into an elementary `PIC 1(8)`: 01010101, `B-NOT` 10101010; a second READ takes AT END) and L15
(the same record into a national `PIC 1(4)` record: 0000, 1111); the expected values are computed from the rule (A, Z and B are
no `'1'`; the pairs x4131 x5A31 x4231 x3031 are no national `'1'`). The below-2002 negative is unchanged. Sibling sweep of
every other writer of characters into a boolean item (elementary MOVE, slice, STRING, ACCEPT, INITIALIZE, MOVE CORRESPONDING,
a CALL BY CONTENT formal, COMPUTE, table element, group move to BIT): all convert; UNSTRING and VALUE cannot reach a boolean
item, and a figurative constant is refused at bind time (§14.9.25.3 SR7).

**Self-review.** No new silent catch; the `Characters` doc no longer promises a boolean arm; the XML docs of
`StorageCharacters`, `DecodedStorage`, `AreaHoldsCharacters` and `StorageArea` name the decoded-characters rule; semgrep
`verify.py` PASS with no count raised.

**Gate.** Run 1 was RED (`HostWitnessTraitDriftTests`, described above). Run `000002-20261027T012224Z`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN … Linux half (WSL clone of HEAD) GREEN` (Conformance 5,809 / Unit 37,211 / Characterization 36 cases ran, leg 2 left to CI).

**Branch review round 1 (two blocking findings, both correct).** The first cut converted in `StorageCharacters`, which every
caller of `StorageArea` reaches, and two callers do not store characters into the item. R1-1: a READ / RETURN fills the record
AREA, the widest record's view of storage every record of the FD implicitly redefines (§13.18.33.4 GR3) and a SAME RECORD AREA
peer shares (§12.4.6.4.4 GR2), so with `01 RA PIC X(4)` and `01 RB PIC 1(8)` the read of `A1Z1B101` left `RA` showing `0101`
(main: `A1Z1`). R1-2: the ROLLBACK restore of an APPLY COMMIT item (`TransactionEmitter` through `EmitImageInto`) returns the
item to its saved image (§14.9.36.4 GR3), so a REDEFINES view `XQ PIC X(4)` of `BQ PIC 1(4)` read `0000` instead of the `ABCD`
it held at the COMMIT. Both were callers my sweep of "every writer of characters into a boolean item" never listed, because
they deposit an IMAGE, not characters. Fix at the root: `ReceivingStore.VerbatimStorageArea` (the representation decode, no
boolean conversion) beside `StorageArea`, both over one private `WriteDecoded`; `SequentialIoEmitter.EmitImageInto` takes
`storesIntoOwnItem` at every call: true only for a READ / RETURN into an area with ONE description
(`FileModel.AreaHasOneDescription`: one record, no SAME RECORD AREA peer), false for a shared area, the peer view, the "also
available" store of a released record and the restore. So the single-record READ keeps the adjudication's conversion (L14,
L15) and the shared cases keep the bytes. The determination sentence names the shared area and the restore in all three
places; goldens L16 (two-record FD, `MA` reads `A1Z1`), L17 (SAME RECORD AREA peer, `SA` reads `A1Z1`) and the new
`14.9.36-ROLLBACK/pb3697_rollback_restores_boolean_item_verbatim` (2023: a display and a national boolean item restored,
their REDEFINES views read `ABCD` and `ABCDEFGH`; the below-2023 negative is `pb3338_apply_commit_below_2023`). The drift test
requires every `EmitImageInto` call to spell `storesIntoOwnItem` and the verbatim store to avoid `StorageCharacters`.

**ABI pin repinned (no bump).** The review finisher's gate was RED in `RuntimeAbiPinDriftTests.TheCallAbi_IsPinnedBesideTheBoundaryLayout`:
the boundary-layout fixture's boolean formal (`PIC 1(4)`) lands a GROUP sender through `StorageCharacters`, which now converts the
decoded characters, so the emitted landing lambda reads `CobolBool.FromCharacters(CobolString.Store(__s.Characters, 4))` where it read
`CobolBool.Store(__s.Characters, 4, justifiedRight: false)`. Diffing the fixture's emitted C# on main and on this branch showed that
single token difference. The two compute the same landing (fit to 4, a space or a short tail becomes boolean zero either way), so
nothing a caller and a callee must agree on moved: `python scripts/abi_pin.py repin` (boundary-hash 110D13CC647FE8B3), not a call-ABI bump.

Review round 2 (R2-1): `FileModel.AreaHasOneDescription` counted a second record and a SAME RECORD AREA peer but not an EXTERNAL file, whose record area is one run-unit cell every describing runtime element reads (§13.18.22.4 GR4 b)). A READ into the one boolean record of such a file converted the shared bytes, and a CALLed element that redefines the record as `PIC X(8)` read `01010101` for the record `A1Z1B101`. The predicate now also requires `!IsExternal`, so the READ takes `ReceivingStore.VerbatimStorageArea`; golden L18 of `pb3666_boolean_receiver_converts_characters` (expected `A1Z1B101`, from GR4 b)), the drift test pins the predicate, and DATA_MODEL D19 names the EXTERNAL area among the shared areas.

