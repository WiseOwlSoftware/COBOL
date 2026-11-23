## Entry 4004 — 2026-11-23 13:09 PST — PB2746: a variable-length group's carrier rules are one runtime operation each, for the declared and the cell-backed lane

**What was wrong.** A variable-length group receives its §2023:8.5.1.12 carrier in two lanes: a declared group's emitted
`FromVarImage`, and a group of a shared storage cell (EXTERNAL, ADDRESS OF, a LINKAGE formal's fresh area) through
`StorageCell.StoreVarGroupAt`. Three rules were written once in each: the recreation of a carried dynamic-capacity table
(§2023:14.6.9.2) against the space fill of one the sender did not carry (§2023:14.6.9.4, the GR9 excess part, PB393), the
receiving truncation of a dynamic-length component (§2023:8.5.1.10.4, with the formal copy-in's whole-content exception of
PB1937), and the units and maxima of the record layout, OCCURS DEPENDING tail included. `StorageCell` described its own
methods as twins of the generated ones, "arm for arm". The re-probe at 27faa4c114 found every pair still in place; a probe
of both lanes on the same MOVEs printed the same lines.

**The fix.** Each rule is one runtime operation, and both lanes call it: `CobolVarGroup.ReceivedDyn`,
`StoreTableComponent` and `StoreGroupTableComponent` for the component stores, and `CobolContiguousLayout.Of` for the
layout, built from a component list (offset, maximum, layout code, table element width) plus the group's `OdoTail`. The
emitted `FromVarImage` now calls the stores instead of spelling the `HasDyn` branches, and the emitted layout field calls
`Of`; `StorageCell.StoreComponentAt` and `LayoutOf` call the same. The emitter's layout walk no longer lists the OCCURS
DEPENDING tail itself: it records where it found it and throws when that is not where `Of` puts it. The cell lane's formal
copy-in now passes `storage: true` at every level instead of `int.MaxValue` maxima at the top, the one form the store reads.

**Behavior neutrality.** `compare_oracle.py --base 27faa4c114`: 151 of 23,549 cases differ, C# only, no diagnostic
change. A script rewrote each baseline file by the expected forms and matched 123 of them exactly; the other 28 differ only
in the formal copy-in's arguments. All 359 changed layout fields were recomputed from `Of`'s rule and equal the old
constructor arguments.

**Tests.** The golden `pb2746_carrier_store_both_lanes` (§2023:14.9.25 MOVE, accept 2014) runs one MOVE into a
WORKING-STORAGE group and into an EXTERNAL group, the excess part and a carried pair, and the lanes print the same lines.
`VariableLengthComponentStoreTests` pins each operation to its rule, and `VariableLengthCarrierStoreDriftTests` refuses a
product file that decides a table's recreation or space fill, or constructs a layout, itself.

**Left open.** PB2080 (the cell lane's WRITE arms' gate) is a PlaceRenderer arm choice this change does not touch. A lead
for the fix lane: a declared group's layout gives a table of national elements the layout code 0 where the cell's gives it
the national unit, so the fixed form of a fixed-length file pads that table in a different space in each lane.

