## PB3424 - a variable-length group coerced under --permissive to a function string argument or a numeric operand is the image DISPLAY shows, not a run-time abort

**What was wrong.** `MOVE FUNCTION UPPER-CASE (PG) TO X` over a group holding a DYNAMIC LENGTH item compiled under `--permissive` with the
COBOLNET1627 warning ("accepted ... with the existing coercion") and then aborted with `NotImplementedCobolFeatureException`, the Tier-C
byte island. The same group in a numeric position (`FUNCTION ABS (PG)`, `COMPUTE X = PG + 1`, `ADD PG TO X`, each a COBOLNET0844 or
COBOLNET1627 warning) died the same way. ISO 15.3 admits a variable-length group as a function argument only where the function says so
(LENGTH, BYTE-LENGTH) and 8.8.1.1 bars a group arithmetic operand, so strict refuses all of it; the defect is that a coercion the compiler
chooses to accept ended in a run-time abort.

**Re-probe.** The note's repro (`P6.cob`) reproduced on main fae388908 for the plain group and for the strong typedef holding an
object-reference leaf; the numeric position was found by the sibling sweep.

**Root cause.** One question, what are this group's characters, answered three ways. DISPLAY carried the variable-length arm inline
(`VarGroupCurrentImage`, gated on `CurrentExtentTransferImageCapable`); `OperandText.AsTransferString`, which the function-argument
visitor asks, only had the fixed pointer-leaf arm; `NumericRenderer.FieldNumCore` went through `SendingGroupValue`.

**Fix.** `PlaceRenderer.VarGroupOperandImage` is the one reader. `AsTransferString` asks it and DISPLAY now calls `AsTransferString`
alone, so DISPLAY and a function argument cannot differ. `FieldNumCore` asks it too and reads a fixed group through `SendingGroupValue(..., transfer: true)`,
because a group holding a pointer or object leaf, fixed or variable-length, also aborted in a numeric position (found by the sweep) and a
digit decode never reads the characters back. The drift test `VariableLengthBoundaryDriftTests` now pins the one reader. The image is the A.1 item 57 current-extent
image (fixed members, each dynamic member at its current extent, an OCCURS DEPENDING table at its current count, a dynamic-capacity table
at its current capacity); the DOC-A.1-57 row names the two coercions and their witnesses.

**Goldens.** `VariableLengthGroupArgumentPermissiveTests` (8 cases): strict refusal, the string lane over a fixed control group, a scalar
variable-length group, an ODO-plus-dynamic group, a dynamic-capacity group and a strong typedef with an object-reference leaf, and the
numeric lane (a plain variable-length group, and a strong typedef group with an object-reference leaf, fixed and variable-length). The conformance-suite manifest runs strict only, so the permissive run-time witness is a Conformance test class; the strict
negative is the standing `15.3-Arguments/pb124-vlg-function-argument`.

**Rows.** None: PB3424 claimed no inventory row, closes none.

**Review round 1.** The reviewer found the sibling raw-storage channel unswept: `FUNCTION CONVERT (PG ANY ANUM HEX)` over a variable-length
group under `--permissive` warned COBOLNET1627 and aborted in the Tier-C island (`IntrinsicRenderer.StorageArg` -> `OperandText.AsStorageImage`).
`AsStorageImage`'s group arm now asks `PlaceRenderer.VarGroupOperandImage` first when the consumer is one-way (`transfer`), so the
contiguous current-extent image (8.5.1.11.2) is the storage image too; `ConvertAnySource_IsTheCurrentExtentImageDisplayShows` pins it
(fixed control `6162633037`, variable-length `61623036`, BYTE-LENGTH 4). A lead was also answered: the pointer-leaf comments said the
leaf "contributes no digit", true for an object reference and for a NULL pointer only; a non-NULL pointer's address token can carry
digit characters. The comment and the DOC-A.1-57 row now say so, and `StronglyTypedGroupWithANullPointerLeaf_...` pins the pinnable NULL case.

**Review round 2.** The round-1 CONVERT ANY fix read the current-extent character image as the group's storage, which is wrong for a group holding a
dynamic-length national item or a dynamic-capacity table of national elements (composed character for character, A.1 item 63; `N"xy"` + `PIC 9(2)` gave
`78793036` against the storage `007800793036`): a loud abort had become a silent wrong value. `DataItem.CurrentImageIsStorageImage` is asked by
`OperandText.AsStorageImage` before the one reader, so such a group stays at the named run-time loud for the storage channel; DISPLAY, the function string argument
and the digit decode (character consumers) are unchanged. Tests: the fixed-national control, the item and table loud pins and the item's own CONVERT. Lead: for such a
group DISPLAY's width and `BYTE-LENGTH` disagree (A.1 item 57's "no exception" is false for a dynamic national item); the DOC-A.1-57 row records it, the fix is a record-framing redesign.
