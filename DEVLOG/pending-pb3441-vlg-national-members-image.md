## PB3441: the image of a variable-length group is its storage, for a dynamic national item and a table of national elements

A variable-length group holding a dynamic-length NATIONAL item or a dynamic-capacity table of national elements had two
widths: DISPLAY showed its positions (`xy06`, 4) while `FUNCTION LENGTH` / `BYTE-LENGTH` counted bytes (6), and
`FUNCTION CONVERT (QG ANY ANUM HEX)` aborted in the Tier-C island, because the group's current-extent image held such a
member one character a position where a FIXED national member was its UTF-16BE byte pairs. The re-probe on this build
(`E:/COBOL-coord/scratch/w1133b/p/NV.cob`) reproduced the note and found the damage wider than its one symptom: a group
with a national table displayed `xypqrs06` against a `BYTE-LENGTH` of 14, `MOVE QG TO QH` between two groups of one shape
stored the character image as the byte image of the receiving table (`QH` came back with one element `pqrs` and
capacity 1, a silent wrong answer), an EXTERNAL group's table elements left in the excess part of a MOVE were
space-filled with the byte 0x20 twice (U+2020, not a space), a fixed-length file trimmed a national member's U+2020 as
padding, and a .NET host's published record landed such a leaf as characters.

**The one law, stated once: the image of a group IS its storage.** `DataItem.ImageUnit` is the one reader of the unit
(image characters a position: two for a national item or GROUP-USAGE NATIONAL group). `GroupImageCodec.CurrentMemberImage`
and `DynElementImage` compose a national leaf and a national element through `CobolBits.NatBytes` (the national-form
NUMERIC element composes the two as `AsImageOf` does, D-N7); `FromVarImage` and `TableElementFromImage` decode through
`CobolBits.NatFromStorage` and truncate at the LIMIT in positions; the contiguous layout counts the item in positions of its
unit. The cell lane keeps the same form: a cell's component slot holds the image (`PlaceRenderer`'s dynamic-slot read and
write arms, `CellDynSeeds`), the window's maximum is `DataItem.SlotMaxSize` (image characters) and its layout code carries
the unit (`FileModel.LayoutCodeOf`, `CobolDynStructure.PlainCode` / `UnitOf`, `StorageCell.LayoutOf`), and
`CellGroupShape.DynLayout` / `StoreVarGroupAt`'s `dynLayout` make an excess-part table element the national space (ISO
14.6.9.4). The fixed form of a fixed-length file pads and trims a national member in whole national spaces
(`CobolVarGroup.SpacesFor` / `TrimTrailingSpaces`, read off the layout code, never off a component's width), the host's
published record lands and reads a national leaf as its image, and `DataItem.CurrentImageIsStorageImage` with its
`OperandText` guard is deleted: CONVERT ANY reads the image of every variable-length group. The shipped runtime surface
changed (`StoreVarGroupAt`, `CellGroupShape`, `FromContiguous`), so the runtime major is now 12 and
`RuntimeAbiPinDriftTests` is re-pinned. CONFORMANCE.md A.1 items 57 and 63, the D-DL3 structure text and D-SLOT in the data
model design state the single rule (the PB3424 "recorded divergence" text is gone).

**Goldens and tests.** `14.9.11-DISPLAY/pb3441_vlg_display_national_members` and `..._cell`,
`A.1-Implementor-defined-language-element-list/pb3441_dynamic_national_member_record` (sequential and fixed-length file,
U+4E2D and U+2020 round-trip), the 2002 negative `_unassigned/pb3441-vlg-national-member-at-2002` (COBOLNET0900);
`VariableLengthGroupImageAgreementTests` (eleven shapes, declared and cell-backed, DISPLAY width = LENGTH = BYTE-LENGTH =
CONVERT ANY digits / 2, the drift test the note asked for), the renamed `ConvertAnySource_DynamicNationalMemberRidesAsItsUtf16Bytes`,
`HostEntryTests.AHost_PassesANationalVariableLengthGroup_AsItsUtf16Image` and `NationalDynamicComponentImageTests`. Each was
mutation-checked: disabling the composer's national arm fails the agreement tests, reverting the emitter edit fails the host
test. Rows: none claimed, none closed (`closes_rows: []`, the note's reason stands). PB2746 (the carrier written twice) is not
folded in: the unit rule is now one reader on the model that both lanes ask and the agreement test pins the lanes against each
other, so that extract wave inherits a smaller duplication. Self-review: the first gate was RED on two tests, both real
findings of this change: the fixed-form space was read off a component's width (a table of two-character elements was
padded with national spaces) and the shipped surface changed without the major; fixed by taking the space from the layout
code and by raising the major. A group comparison (ISO 8.8.4.2.17) remains an alphanumeric comparison of the group's
storage, so two national members that differ only by a trailing national space compare unequal; that is the same rule a fixed
national member already followed. Gate: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (run
`20261020T203628Z-0e8334`, Conformance 11,077/18,046 · Unit 34,011/34,867 · Characterization 5/36, leg 2 left to CI).
