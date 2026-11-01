## Entry 2930 — 2026-11-01 01:17 PST — PB4320: the model states an access as structure, never as C# text built from names (leak L7)

kb/Work PB4320 (campaign PB2293, DESIGN-backend-abstraction §1.3 L7). PB4265 made the model's names CLR names; four
binder sites then composed C# member-access text from them, so the model held expressions a CIL backend would have to
parse back.

**Re-probe (base 2e48863d5).** All four stood: a carrier-resident formal's `DataItem.ClrName` was overwritten with
`__lnk{Uid}.Value` (`CallBindLinkage`, `OoBindMethodData`); `AccessPath.Reroot` joined the `__outer.` chain or a
container's class name onto `RootFieldSegment.CsField`; `GlobalBridgesOf` stored each bridge's `anchor + member` text;
`SortMemberPath` joined a table-sort key's member names with `.`. The sibling sweep (`+ "."`, `string.Join(".")`,
`.Value"`, `__outer` over `Binding/`) found a fifth on the model: `BoundUnit.ClassRef` joined the nested class names.

**Fix shape.** A carrier-resident formal keeps its name and carries its carrier (`DataItem.ResidentCarrier`); its one
root builder, `ReferenceResolver.AddRoot`, roots the path at the carrier and steps through a new
`CarrierValueSegment` (ISO §14.2.3 GR8). The omitted-argument guard's carrier pass-through became a flag
(`OmittedFormalGuard.PassesCarrier`) instead of a prefix `GuardedRoot` searched for in the root text. A containing
program's member carries a `ContainerAnchor`, `Instance(depth)` or `Static(class)`, decided once by
`BoundUnit.AnchorOf` (§13.18.27.4 GR2; §13.5.4 GR1 makes a RECURSIVE unit's working-storage static data) and set on
the root segment by `Reroot`; `CallBridge` carries the member name and its anchor, and the bridge list has one
unanchored source (`GlobalBridgeMembersOf`) that the member-name reservation reads too. A sort key carries its member
chain of items (`BoundTableSortKey.MemberChain`). The Roslyn backend spells every one: `PlaceRenderer.RootStorage`,
`PlaceRenderer.Anchored`, `ProgramEmitter.ClassRef`, the sort comparer. `BoundUnit.AnchorOf` no longer reaches into
`CodeGen.RuntimeApi`, which was the last `Binding.Model → CodeGen` use: that is kb/Work PB2397's extract wave, landed
here with a census record at b8e4a3b58 and the tolerated row deleted from `architecture-edges.json` by the ratchet.
The inventory rows that named the renamed members (GR-14.9.4.4-12 `RootOf`, SR-14.9.40.3-14 `SortMemberPath`) carry
the new names through a `record_verdicts` batch that retires the old witnesses (GAP 19 → 19).

**Behavior neutrality.** `scripts/arch/compare_oracle.py` against the base commit's capture: IDENTICAL over all 15712
cases (emitted C# and diagnostics). The corpus holds 304 programs with a carrier-resident formal, 5 with a guarded
one, 6 with a bridged one, static-anchor bridges (`_PRG_FCONT1133.FS`) and nested sort keys.

**Witness.** `ModelAccessStructureTests` binds a group with every former leak (a GLOBAL carrier-resident formal used
by a contained program, a RECURSIVE container's static GLOBAL item, a table SORT whose key lies two members down) and
asserts every `ClrName` is an identifier and every access is its structural form; with the first leak put back it
failed with `L-X: '__lnk4.Value'`. No golden: the change is behavior-neutral and no inventory row describes the model's
internal shape (`closes_rows: []`). DESIGN-backend-abstraction §1.2 and §1.3 L7 (now DONE), INTERPROGRAM_DESIGN and
OO_DESIGN describe the new shape.

**Self-review.** Architecture: one anchor decision (`AnchorOf`), one anchor spelling (`Anchored`), one root builder
(`AddRoot`), one bridge list. Code: the guard renders byte-identically (the carrier was always the root's whole
reference-type part). Performance: `CarrierValueSegment` is a singleton; the bridge list allocates one small struct per
member. Duplication: the ten `anchorOf(x) + x` copies became one.

**Gate.** `build-local.ps1 -Priority BelowNormal`, leg 1 at the branch head; its run directory and verdict line are
in the wave report (`w20261800d-PB4320-report.md`). `semgrep/verify.py`: PASS, no rule's count rose.

