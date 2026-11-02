## PB4380: no access path is rooted at a C# method local's name (leak L8's residue)

kb/Work PB4380 (campaign PB2293, wave 20261814 group C). DESIGN-backend-abstraction §1.2 forbids the name of a C#
method local on a neutral node, because a CIL method addresses its locals by slot. PB4321 gave the binder's statement
locals a `MethodLocal` identity, but three access paths still started at a `RootFieldSegment` whose `CsField` was a
local's name, and the §6 contract test tolerated `RootFieldSegment.CsField` and `DataItem.ResidentCarrier` as its L8
leak row.

**Re-probe (base 51a9ba52f).** All three stood. `DataBinder.OoBindMethodData` set `DataItem.ResidentCarrier` to the
string `__lnk{Uid}` (`OoFormal.CarrierLocal`) and `ReferenceResolver.AddRoot` rooted the method formal at it; the
base's emitted C# for `suite/2002/oo_method_args` declares it as a local, `ManagedPointer<string> __lnk1 = ...`.
`PlaceIdentification.FreezeView` re-rooted a hoisted cell at `new RootFieldSegment(local, ...)`, and
`ViewCursor.AtElementCell` took the `__le{depth}` lambda parameter as a string and rooted the element cell at it.

**The fix.** Every root is a `RootSegment`, the one type that carries a formal's *-ARG-OMITTED guard.
`RootFieldSegment` holds only a CLR name (`MemberClrName`), and `LocalRootSegment(MethodLocal, Guard)` is a local
by identity, now guarded too. The guard's rendering, `PlaceIdentification.PathEvaluates` and `PlaceRenderer.Received`
all ask `RootSegment`, so a new kind of root needs no change in any of them. `DataItem.ResidentCarrier` is now a fact,
a `FormalCarrier`: `Member` (the program class's carrier member) or `Local` (the method's carrier local, the new
`StatementLocalRole.MethodFormalCarrier`). Each arm makes its own root, so `AddRoot` asks the carrier and never the
arm. `OoFormal` splits into `Carrier`, the resident formal's local, which `OoEmitter` declares through `LocalNames.Of`,
and `AreaPointerClrName`, the area formal's per-activation pointer member. The backend's hoisted cell and a
recreation's element cell are `CSharpLocal`s on a `LocalRootSegment`.

**The emitted C# changed in one class.** The method carrier's stem is `__mlnk`, not `__lnk`. `__lnk{Uid}` is also the
program's carrier MEMBER, which the binder names, and `MethodLocalIdentityDriftTests` refuses a binder literal that
holds a local's stem. The two shared a spelling, never a name. `compare_oracle.py --base 51a9ba52f` found 104 of 15829
cases different. Each is an OO method with a resident formal, and a script checked that each is byte-identical once
`__mlnk` reads as `__lnk`. The `FreezeView` and `AtElementCell` changes emit identical C#.

**Witness.** `ModelAccessStructureTests.AMethodsResidentFormal_IsRootedAtItsCarrierLocalsIdentity` binds a class whose
method has a resident formal and checks the path `[LocalRootSegment(MethodFormalCarrier, uid), CarrierValueSegment]`
and the formal's guard. Planted back to a `RootFieldSegment`, the test failed on the segment types. The string-member
theory now covers both root kinds and both `FormalCarrier` arms. The L8 row is gone from `BackendContract.Table`, and
§1.3 L8 is DONE with no residue. No inventory row moves (`closes_rows: []`, a behavior-neutral seam change).

**Found on the way.** The §14.7.6 citation on `LocalRootSegment` and `StatementLocalRole.CorrespondingSendingAnchor`
quoted words the standard does not have; both now quote §14.7.6's own sentence ("Any item identification associated
with a corresponding pair of operands is done at the start of the execution of the statement containing the
CORRESPONDING phrase"). A method's LOCAL-STORAGE and non-formal LINKAGE roots are C# locals the Roslyn backend names by
the item's `ClrName`, still rooted at a `RootFieldSegment`; that is an allocated CLR name, not an invented one, and is
left to the report as a lead.

**Gates.** Windows: `=== BUILD-LOCAL GATE: GREEN — Conformance 20,540/20,540 · Unit 39,155/39,155 · Characterization 37/37 cases ran (skipped 0)` (run 000001-20261102T034805Z, at ebbe81afd; the new `AMethodsResidentFormal_IsRootedAtItsCarrierLocalsIdentity` Passed in `Unit.trx`). Linux: not part of the per-change gate (PB4327). `python scripts/semgrep/verify.py`: PASS, no count raised.
