## PB3273: a strong group holding a variable-length component and a reference has its .NET host entry

A unit whose group formal or RETURNING item held both a variable-length component (a dynamic-length item or a
dynamic-capacity table, §8.5.1.12) and a pointer or object reference published no host entry. ISO §14.2.3 13) only
obliges the implementor to state the restrictions of a non-COBOL activator; it imposes none, so the withheld entry was
an implementation gap. Its cause was the published record, not the boundary: PB3139 gave the COBOL boundary its form for
the shape (the cell area holding references and components together), and the record composed no such area.

**Re-probe.** `E:/COBOL-coord/scratch/w1105h/BOTH.cob` (strong `TB` = `PIC 9(2)`, `PIC X DYNAMIC LENGTH LIMIT 8`,
`USAGE POINTER`, the formal of `H3BOTH`) had entry classes for `H3MAIN` and `H3OK` only. On this branch it has `H3BOTH` too.

**Fix shape.** The record builds the area and reads it back. `__Area` starts from the description's fresh cell. The host
class declares it as `__hs<n>`, from `DataEmitter.FreshArea`, the factory the program's own area formal takes, with the
description's `_P_` profiles. So a prototype-only unit needs no program class, and a table the area holds is created and
initialized by the description (§8.5.1.9.5). The carrier is then distributed into that cell (`StorageCell.StoreVarGroupAt`),
and the references are set in their managed slots, the element cells of a dynamic-capacity table included. The argument
states the group's §8.5.1.12 atoms, so the activated program's formal is laid over the area (§14.2.3 8)). `__TryGetArea`
reads the area back by the inverse walk. `Renders` asks `DataItem.CurrentExtentAreaCapable`, the one member law with a
reference treated as a fixed placeholder. `CellComponents.ShapeOf` became the one window-shape construction, and
`DataEmitter.FreshArea` the one fresh-cell factory, both shared with the program lane. The finishing pass routed the two
element-cell reads through `RuntimeApi.CellDynTable` and `TableCurrentOccurrences`. It also corrected the stale
restriction text in `HostEntryEmitter.Published`, DESIGN-external-repository §4.3 (the variable-length host-type row now
records the fresh-cell choice) and both copies of the DOC-A.1-141 determination.

**Witnesses.** `HostEntryTests.AHost_PassesAStrongVariableLengthGroupWithReferences_AsTheCellAreaOfItsComponentsAndReferences`
has a C# host pass and receive the group. Its expected values come from §8.5.1.10.4 (truncation at the LIMIT), §8.5.1.9.3
and §8.5.1.9.5 (a created occurrence is initialized under INITIALIZED) and §13.18.38.4 16) (the minimum capacity refused
before activation). `HostEntryShapeDriftTests.AGroupHoldingAComponentAndAReference_PublishesItsEntry_AsTheDescriptionsOwnCellArea`
replaces the restriction pin. The finishing pass added
`HostEntryTests.AHost_PassesNestedComponentsAndReferences_ThroughTheCellArea_AndReadsThemBack` for the nested arms the
first test leaves unexercised: a subordinate group with its own dynamic item and pointer, a dynamic table under
INITIALIZED whose elements hold a nested dynamic table of pointers, and a RETURNING group with a fixed table of
variable-length elements. Scratch host probes of numeric-edited, packed, COMP-5, float, national and program-pointer
members also returned the values the standard gives.

**Rows.** None: no inventory row tracked the withheld entry, and DOC-A.1-141 was already OK (`closes_rows: []`).

**Self-review.** The four lenses and the four agent checklists: one law, one shape construction, one fresh-cell factory.
No second recipe for an element seed lives in the record. A pointer slot is read only from the cell that holds it. The
`Published` guard stays for shapes the law refuses, which the binder already refuses (COBOLNET0853/0854). A small
duplicate remains: the host class declares a number's `_P_` profile beside its `__hm` member profile.

**Gate.** Run `20261018T170337Z-6f5a3f`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` (Conformance 11,296, Unit 33,695, Characterization 5 cases ran; both host tests and the drift pin passed).
