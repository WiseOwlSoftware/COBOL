// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Passes;

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// The STORAGE-TIER facts of ONE bound unit (ISO §13.5.4 GR1 static data, §14.6.2.3.2 initial state, §13.18.5 BASED and
/// ADDRESS OF; docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1); kb/Work PB2296 M6): the four STATIC channels that
/// say which emitted class members are ONE copy per class, and the cell backings and bridges the pointer pass forces. A value
/// slice of <see cref="DataBindState"/> with two producers, each in its own phase and neither reading the other's output
/// before it is done: <see cref="PtrBindBasedAndAddressablesPass"/> (the BASED bridges, the ADDRESS-OF cell claims) and
/// <see cref="RouteStaticUnitStoragePass"/> (the static channels, which read the claims so a claimed class is routed once).
/// Its one LATE writer is <see cref="ClaimTemporaryCell"/>: the cell of a user-defined function's result temporary, minted by
/// the PROCEDURE binders, so the slice is frozen only when they are done. <c>DataBindStateBoundaryDriftTests</c> pins each door.
/// </summary>
public sealed class StorageTierSlice
{
    private readonly HashSet<string> _staticRootFields = new(StringComparer.Ordinal);
    private readonly HashSet<string> _staticBasedBridgeAddrs = new(StringComparer.Ordinal);
    private readonly HashSet<string> _staticAddressableCells = new(StringComparer.Ordinal);
    private readonly HashSet<string> _staticIndexCells = new(StringComparer.Ordinal);
    private readonly List<(string Backing, string CellField, DataItem Canonical, int Width)> _addressableBackings = [];
    private readonly List<(string Backing, string CellProp, string AddrField, int Width)> _basedBridges = [];
    private readonly Dictionary<RedefinesClass, string> _addressableCellOf = [];

    /// <summary>The STATIC-field channel: every emitted root field name whose storage is ONE per-class copy — consumed by
    /// <c>RecordStructEmitter</c> to add the <c>static</c> modifier. ONE producer: a RECURSIVE-and-not-INITIAL unit's (incl.
    /// every FUNCTION's, §8.6.6) WORKING-STORAGE roots + Tier-B backings (<see cref="RouteStaticUnitStoragePass.Run"/> —
    /// §13.5.4 GR1 static data, ONE last-used copy across all activations, §14.6.2.3.3). A method has no WORKING-STORAGE to put
    /// here (§13.5.3 SR1, kb/Work PB1308).</summary>
    public IReadOnlySet<string> StaticRootFields => _staticRootFields;

    /// <summary>The BASED bridge address fields of a RECURSIVE unit's static WORKING-STORAGE (kb/Work PB154 — §14.6.2.3.2
    /// action 5): the `__addr_X` field emits STATIC and `__ResetStatics` sets it back to NULL, where the old routing REJECTED
    /// legal `01 X BASED.` in a RECURSIVE program outright.</summary>
    public IReadOnlySet<string> StaticBasedBridgeAddrs => _staticBasedBridgeAddrs;

    /// <summary>The ADDRESS-OF cell fields of a RECURSIVE unit's static WORKING-STORAGE (kb/Work PB234): the record's
    /// <c>StorageCell</c> — its ONE storage, which <c>ManagedPointer.At</c> aliases — emits STATIC (§13.5.4 GR1: one copy
    /// shared by every activation) and <c>__ResetStatics</c> re-seeds it IN PLACE (§14.6.2.3.2 action 2), where the old routing
    /// REJECTED `ADDRESS OF` a static item outright (COBOL0899 — legal source refused).</summary>
    public IReadOnlySet<string> StaticAddressableCells => _staticAddressableCells;

    /// <summary>The STATIC index-cell channel — INDEXED BY cells emitted as class-level STATIC <c>long</c> fields because their
    /// table's storage is static: a RECURSIVE unit's WS table cells (<see cref="RouteStaticUnitStoragePass.Run"/> — the cell
    /// rides its table's §13.5.4 GR1 static storage; these DO also appear in <see cref="ForestSlice.IndexNames"/>, so the
    /// emitter's instance-cell loop consults this set for the modifier). A method table's cell is instead a per-activation
    /// method local, emitted in <c>OoEmitMethod</c>, and never appears here.</summary>
    public IReadOnlySet<string> StaticIndexCells => _staticIndexCells;

    /// <summary>⛔ THE ONE QUESTION "IS THIS CLASS MEMBER STATIC?" — true when <paramref name="csMember"/> is on any of the four
    /// static channels (<see cref="StaticRootFields"/>, <see cref="StaticBasedBridgeAddrs"/>, <see cref="StaticAddressableCells"/>,
    /// <see cref="StaticIndexCells"/>), i.e. it is ONE copy per class (§13.5.4 GR1) and C# reaches it through the CLASS name,
    /// never through an instance (CS0176). A contained program's reference to a container's member asks it, through
    /// <see cref="AnchorOf"/>, so a container with static working-storage composes with its containees' GLOBAL bridges
    /// (kb/Work PB1133). A new static channel joins THIS predicate and nothing else.</summary>
    public bool IsStaticMember(string csMember) =>
        _staticRootFields.Contains(csMember) || _staticBasedBridgeAddrs.Contains(csMember)
        || _staticAddressableCells.Contains(csMember) || _staticIndexCells.Contains(csMember);

    /// <summary>⛔ THE ONE ANCHOR OF A CONTAINER MEMBER (kb/Work PB1133): how a program <paramref name="depth"/>
    /// containment levels INSIDE the unit this slice belongs to reaches that unit's class member <paramref name="csMember"/> —
    /// through the chain of containing instances for an instance member, through the unit's CLASS
    /// (<paramref name="classClrName"/>) for a static one (<see cref="IsStaticMember"/>: ISO §13.5.4 GR1's static data is
    /// one copy on the class, which no instance names). Every place a contained program reaches into a container's data asks
    /// here: the GLOBAL bridges (<c>LinkageBinder.GlobalBridgesOf</c>, through <c>InheritStep.Merge</c>). The answer is
    /// structure, which each backend spells (kb/Work PB4320).</summary>
    public ContainerAnchor AnchorOf(string classClrName, string csMember, int depth) =>
        IsStaticMember(csMember) ? new ContainerAnchor.Static(classClrName) : new ContainerAnchor.Instance(depth);

    /// <summary>True when any static channel holds a member: the storage half of the condition for emitting
    /// <c>__ResetStatics</c> (<see cref="DataBindResult.EmitsStaticReset"/> adds the unit-scoped file-registration flag).</summary>
    public bool HasStaticMembers =>
        _staticRootFields.Count > 0 || _staticBasedBridgeAddrs.Count > 0 || _staticAddressableCells.Count > 0 || _staticIndexCells.Count > 0;

    /// <summary>The per-instance cell backings the emitter renders for ADDRESS-OF-taken records:
    /// <c>private readonly StorageCell {CellField} = new StorageCell {{ Ref = «ImageInitOf(Canonical)» }};</c> +
    /// <c>private ref string {Backing} =&gt; ref {CellField}.Ref;</c>. The seed honors the record's VALUE clauses exactly like
    /// the Tier-B stored-backing initializer (the emitter computes it via the ONE <c>FieldEmitter.ImageInitOf</c> — the
    /// based_pointer first-run lesson: a default image loses VALUE).</summary>
    public IReadOnlyList<(string Backing, string CellField, DataItem Canonical, int Width)> AddressableBackings => _addressableBackings;

    /// <summary>The BASED bridges the emitter renders: the implicit data-address pointer field
    /// (<c>private ManagedPointer {AddrField} = ManagedPointer.Null;</c> — initially NULL, §13.18.5 GR2) + the deref bridge
    /// (<c>private ref string {Backing} =&gt; ref CobolPtr.Deref({AddrField}, {Width}).Ref;</c> — GR3/GR4 loud at every
    /// reference).</summary>
    public IReadOnlyList<(string Backing, string CellProp, string AddrField, int Width)> BasedBridges => _basedBridges;

    /// <summary>ADDRESS-OF-forced classes → their cell FIELD name (the emitter's <c>ManagedPointer.At({cell}, {offset})</c>
    /// source; bind-time validation checks membership).</summary>
    public IReadOnlyDictionary<RedefinesClass, string> AddressableCellOf => _addressableCellOf;

    internal void AddStaticRootField(string csMember) => _staticRootFields.Add(csMember);
    internal void AddStaticBasedBridgeAddr(string csMember) => _staticBasedBridgeAddrs.Add(csMember);
    internal void AddStaticAddressableCell(string csMember) => _staticAddressableCells.Add(csMember);
    internal void AddStaticIndexCell(string csMember) => _staticIndexCells.Add(csMember);

    /// <summary>Record a BASED bridge the emitter renders.</summary>
    internal void AddBasedBridge(string backing, string cellProp, string addrField, int width) =>
        _basedBridges.Add((backing, cellProp, addrField, width));

    /// <summary>Record a forced class's per-instance cell. ⛔ THE CLASS NAMES ITS OWN CELL (kb/Work PB231): the field used to
    /// carry an ad-hoc <c>_cell_{NAME}</c> spelling that only this list knew, so a place builder could not reach the cell to
    /// address the area's MANAGED SLOTS. It is now <see cref="RedefinesClass.BackingCellClrName"/>, the ONE name every cell
    /// surface uses and <c>PlaceBuilder.BuildCellPath</c> resolves.</summary>
    internal void ClaimAddressableCell(RedefinesClass cls) => Claim(cls);

    /// <summary>⛔ THE LATE DOOR: record the cell of a compiler temporary a PROCEDURE binder minted (a user-defined function's
    /// result temporary, <c>DataBinder.ClaimTemporarySlotCarriedCell</c>) after the claims of
    /// <see cref="PtrBindBasedAndAddressablesPass"/> were made and <see cref="RouteStaticUnitStoragePass"/> routed the static
    /// channels. A temporary is per-activation and never static, so it joins <see cref="AddressableBackings"/> and
    /// <see cref="AddressableCellOf"/> without a channel; the slice is complete only when the procedure binders are done (the
    /// group tail, when <c>DataBindResult</c> is built).</summary>
    internal void ClaimTemporaryCell(RedefinesClass cls) => Claim(cls);

    private void Claim(RedefinesClass cls)
    {
        string cell = cls.BackingCellClrName;
        _addressableCellOf[cls] = cell;
        _addressableBackings.Add((cls.BackingClrName, cell, cls.Canonical, cls.Width));
    }
}
