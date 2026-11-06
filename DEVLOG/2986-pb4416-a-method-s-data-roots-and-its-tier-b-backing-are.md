## Entry 2986 — 2026-11-01 23:34 PST — PB4416: a method's data roots and its Tier-B backing are rooted at a local's identity, not at a CLR name

**The mechanism.** A METHOD's LOCAL-STORAGE and LINKAGE roots, and the one string backing of a method's Tier-B REDEFINES
class, are locals of the generated method (ISO §8.6.4: "Their storage is allocated and set to initial state each time the
runtime element containing them is activated"), but their access paths were rooted at `RootFieldSegment`, the segment of
a program-class MEMBER: `ReferenceResolver.AddRoot` at `RootFieldSegment(DataItem.ClrName)`,
`ReferenceResolver.BuildBackingPath` at `RootFieldSegment(RedefinesClass.BackingClrName)`, and `OoEmitter.MethodRootPlace`
built one itself. A CIL backend addresses a local by slot and a member by field, so it would have had to infer which from
the item's section and owner, and DESIGN-backend-abstraction §1.2 and L8's DONE mark claimed more than the code did. It was
the root kind PB4380 did not reach (PB4380 converted the carrier local and the backend's two cell locals).

**Re-probe (base 4a51cd367).** A unit probe printed each method root's place for the fixture now in
`ModelAccessStructureTests`: `LS-G` and `L-R` at `RootFieldSegment { MemberClrName = LS_G }` / `L_R`, the Tier-B class
`LS-R`/`LS-V` a window over `RootFieldSegment { MemberClrName = _redef_LS_R }`, each one a local in the emitted method.

**The fix.** The local's identity is a fact on the model. `DataBinder.OoBindMethodData` gives every method root
`DataItem.ActivationLocal = new MethodDataLocal(root)`; `RedefinesClass.BackingLocal` derives a
`MethodRedefinesBackingLocal(class)` for a stored Tier-B class whose canonical is such a root (a cell-backed class's backing
is a member bridge, and a nested canonical's backing is a member of its root's storage). One function,
`ReferenceResolver.StorageRoot`, chooses the root kind for `AddRoot` and `BuildBackingPath`: a `LocalRootSegment` over the
identity, else the member's `RootFieldSegment`. `OoEmitter.MethodRootPlace` roots at the identity, and every declaration and
read of the method's data in `OoEmitter` spells it through `LocalNames.Of` (`ActivationLocalOf`, `MethodBackingDecl`), whose
two new arms spell the storage's allocated CLR name, so the declaration and every path cannot name two locals and the
emitted C# is unchanged: `scripts/arch/compare_oracle.py` against the base capture reports
`=== ARCH-ORACLE: IDENTICAL — 15853 cases`. DESIGN-backend-abstraction §1.2, §1.3 L8 and §5 name the method's data.

**The witness.** `ModelAccessStructureTests.AMethodsDataRoots_AreRootedAtTheirLocalsIdentity` holds every root of a class
whose method has a LOCAL-STORAGE group, a LOCAL-STORAGE Tier-B class, an AREA formal, an unattached LINKAGE record and a
RETURNING item: each carries its `MethodDataLocal`, the plain roots and a member are rooted at it, the Tier-B class is a
window over its `MethodRedefinesBackingLocal`, and the area formal and the object datum stay member roots. Planted back to
a member root it failed on the segment type. No golden: the change is behavior-neutral and no ISO rule's outcome moves.

**Self-review.** Architecture: the identity is model structure and its spelling is the Roslyn backend's alone; the
member-or-local choice is one function. Code: `BackingLocal` and `RecordStructEmitter.MethodRedefinesBackingDecl` state the
same condition from two sides, and `OoEmitter.MethodBackingDecl` throws if they part, as `ActivationLocalOf` does for a
root with no identity. Performance: one small record per resolution of a method Tier-B member, nothing on any other path.
Duplication: none added. Found on the way and reported, not fixed (a different leak row, L4): a method's INDEX-NAME cells
are method locals too (`long {idx.Cell} = 1;` in `OoEmitter.EmitMethod`) while the model names them by text
(`IndexDeclaration.Cell`, `PositionIndexCell.Cell`).

**Gate.** See the report: the verdict line of the gate run at this branch's head.

