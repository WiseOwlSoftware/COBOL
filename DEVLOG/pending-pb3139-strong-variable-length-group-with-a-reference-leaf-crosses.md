## PB3139 — a strongly-typed variable-length group with a reference leaf crosses as its area and its leaf vector

**Re-probe** (fresh Debug build of main 0b8557338, repros in `E:/COBOL-coord/scratch/w1106c/`): a strong type holding
`PIC X DYNAMIC LENGTH`, `USAGE POINTER` and `PIC 9(2)` passed to a program-prototype CALL drew COBOLNET1688 on the CALL
AND COBOLNET1513 on the prototype against its own definition; the activated program's formal staged the Tier-C loud at its
copy-out; a group MOVE of two such groups aborted with GR9's "cannot compose" loud. The fixed-length twin of the same
type crossed correctly, so the one cause was `OoClassTable.LeafCarried` excluding every variable-length strong group,
while `CurrentExtentImageCapable` excludes any group with a reference leaf: the shape had no crossing form at all.

**Rule** (each `cite.py --check` OK): §14.8.2.2 rule 2 ("If either the formal parameter or the corresponding argument is a
strongly-typed group item, both shall be of the same type"), §13.18.60.3 SR14 (a POINTER leaf "subordinate to a type
declaration that includes the STRONG phrase"), §8.5.1.12.1 (nothing bars a strong group from being variable-length),
§14.2.3 GR8 / GR9, §14.6.5, §14.9.25.3 SR2, §14.9.25.4 GR9 a), §14.6.9.2 ("The operation recreates or overwrites the
receiving table with a copy of the sending table").

**Fix.** `LeafCarried` admits a variable-length strong group whose current extent does not compose; every consumer the
gate now reaches was completed rather than staged: `StorageCell.CopyArea` / `StoreArea` carry the area's components from
its `DynBase` (a dynamic-capacity table deep-copied, `CobolDynTable.CopyOf`), so BY CONTENT (`ContentRecord`, which now
keeps the area's `Shape`) and RETURNING (`StoreReturnArea`) deliver them; a slot-riding group's BY CONTENT carrier is a
read-only view instead of an eager image read; the leaf vector carries a dynamic-capacity table as its current
occurrences, `RecordStructEmitter` emitting an instance `WithLeaves` (replacing the static `OfLeaves`) that recreates the
receiver's own table (`CobolDynTable.Recreated`) and `PlaceRenderer.CellLeaves` / `CellLeavesStore` doing the same for a
cell-backed group (`ViewCursor.AtElementCell`); `MoveEmitter` asks the leaf vector before GR9's composer. The shipped
runtime surface changed, so the runtime major is 8 (`RuntimeAbiPinDriftTests` re-pinned, characterization snapshots).
Docs: COBOLNET_OO_DESIGN (the leaf vector and the program ABI's area, whose "not yet carried" paragraph was stale since
PB1940), DOC-A.1-141 in CONFORMANCE.md and DESIGN-external-repository §4.3 (the host restriction's reason).

**Goldens:** `tests/conformance/2014/pb3139_strong_vlg_pointer_call` (CALL BY REFERENCE / BY CONTENT / RETURNING, a
user-defined function's result, MOVE between a cell-backed formal and a record struct; a pointer in a dynamic-capacity
table's element), `pb3139_strong_vlg_objref_invoke` (INVOKE BY REFERENCE / BY CONTENT / RETURNING; an object reference in
a table element), negative `pb3139-strong-vlg-pointer-at-2002` (reject-at 85 2002, COBOLNET0900). Expected values derived
from the rules above in each golden's header.

**Rows:** none claimed by the note (closes_rows_reason stays: no inventory row).

**Remaining (SPLIT):** the .NET host entry still withholds a unit with such a formal (PB3273's restriction,
`PublishedRecordEmitter.Renders`): the published record composes no area holding components beside slots. The note's
Progress section names the steps.
