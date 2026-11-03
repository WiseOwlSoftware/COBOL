// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;

using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// The data-pointer half of the data binder (Phase-4b increment 2 — ISO §13.18.5 BASED / §8.4.3.11 ADDRESS OF;
/// the PHASE4_RECONCILIATION "M2-DATA-5 / M2-PROC-5 — increment 2" design). Both jobs are instances of the ONE
/// cell-backing mechanism (<see cref="ForceStringCanonical"/>, the factored EXTERNAL re-basing): a BASED 01/77
/// becomes a storage TEMPLATE whose backing is a pointer-deref bridge (<c>ref CobolPtr.Deref(__addr_X, w).Ref</c>
/// — every reference windows the ADDRESSED cell at the pointer's runtime offset); an ADDRESS-OF-taken item's
/// record moves onto a per-instance <see cref="WiseOwl.Cobol.Runtime.StorageCell"/> so a
/// <see cref="WiseOwl.Cobol.Runtime.CellPointer"/> can alias its storage with structural (§8.8.4.2) equality.
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The group's one walk of the parse tree for storage-claim operands (<see cref="BindSession.ClaimScans"/>; kb/Work
    /// PB4302), shared by every unit's binder because the units share the tree — a unit's question about the programs it
    /// contains is then a slice of the container-independent answer, not a re-walk. A binder made outside a session (direct
    /// test construction) walks for itself.</summary>
    internal StorageClaimScan ClaimScans { get; init; } = new();

    /// <summary>The per-instance cell backings the emitter renders for ADDRESS-OF-taken records:
    /// <c>private readonly StorageCell {CellField} = new StorageCell {{ Ref = «ImageInitOf(Canonical)» }};</c> +
    /// <c>private ref string {Backing} =&gt; ref {CellField}.Ref;</c>. The seed honors the record's VALUE
    /// clauses exactly like the Tier-B stored-backing initializer (the emitter computes it via the ONE
    /// <c>FieldEmitter.ImageInitOf</c> — the based_pointer first-run lesson: a default image loses VALUE).
    /// (READ-ONLY view — P6 Step 5.)</summary>
    internal IReadOnlyList<(string Backing, string CellField, DataItem Canonical, int Width)> PtrAddressableBackings => _ptrAddressableBackings;
    private readonly List<(string Backing, string CellField, DataItem Canonical, int Width)> _ptrAddressableBackings = [];

    /// <summary>The BASED bridges the emitter renders: the implicit data-address pointer field
    /// (<c>private ManagedPointer {AddrField} = ManagedPointer.Null;</c> — initially NULL, §13.18.5 GR2) +
    /// the deref bridge (<c>private ref string {Backing} =&gt; ref CobolPtr.Deref({AddrField}, {Width}).Ref;</c>
    /// — GR3/GR4 loud at every reference). (READ-ONLY view — P6 Step 5.)</summary>
    internal IReadOnlyList<(string Backing, string CellProp, string AddrField, int Width)> PtrBasedBridges => _ptrBasedBridges;
    internal readonly List<(string Backing, string CellProp, string AddrField, int Width)> _ptrBasedBridges = [];

    /// <summary>ADDRESS-OF-forced classes → their cell FIELD name (the emitter's
    /// <c>ManagedPointer.At({cell}, {offset})</c> source; bind-time validation checks membership).
    /// (READ-ONLY view — P6 Step 5.)</summary>
    internal IReadOnlyDictionary<RedefinesClass, string> PtrAddressableCellOf => _ptrAddressableCellOf;
    private readonly Dictionary<RedefinesClass, string> _ptrAddressableCellOf = [];

    /// <summary>⛔ WHO ENDS A CELL'S LIFE, ONE CLASSIFICATION (kb/Work PB1216). ISO §13.18.5.4 GR4 raises EC-BOUND-PTR for a
    /// reference through an address that is "not a valid address of storage", and §8.6.5 says the association "may cease
    /// to exist because the actual data no longer exists, as specified 8.6.4" — so every cell-backed record's storage
    /// duration (§8.6.4) decides which event ENDS the cell, and this is the one place the three are told apart:
    /// <list type="bullet">
    /// <item><see cref="CellLifetime.Static"/> — a RECURSIVE unit's static WORKING-STORAGE and a method's WORKING-STORAGE
    /// (<see cref="StaticAddressableCells"/>): one copy, re-seeded IN PLACE by <c>__ResetStatics</c> at a CANCEL, which
    /// ends the old life (<c>StorageCell.Reinitialize</c>).</item>
    /// <item><see cref="CellLifetime.Activation"/> — LOCAL-STORAGE and non-formal LINKAGE (a program's, a method's):
    /// "persists while that instance of the runtime element is in active state", so the cell ends at the activation's exit
    /// — inside <c>Call</c> for a program, in the method's <c>finally</c> (<c>OoEmitter.ActivationPointerSeeds</c>).</item>
    /// <item><see cref="CellLifetime.Instance"/> — a program's WORKING-STORAGE cell owned by its instance: an INITIAL
    /// program's items end at its activation's exit, any other program's at the CANCEL that drops the instance, both
    /// through the emitted <c>EndStorage</c> (<c>ICobolProgram.EndStorage</c>).</item>
    /// </list>
    /// A cell added to <see cref="PtrAddressableBackings"/> tomorrow is classified here and so ended by the matching
    /// event without an edit elsewhere; <c>CellLifetimeDriftTests</c> reads the generated program and fails if a cell
    /// field is named by none of them.</summary>
    internal CellLifetime LifetimeOfCell(string cellField, DataItem canonical) =>
        StaticAddressableCells.Contains(cellField) ? CellLifetime.Static
        : OoMethodScopedRoots.Contains(canonical) || LocalStorageRoots.Contains(canonical)
          || LinkageRoots.Contains(canonical) ? CellLifetime.Activation
        : CellLifetime.Instance;

    /// <summary>The cell fields of <see cref="PtrAddressableBackings"/> whose life ends with <paramref name="lifetime"/>'s
    /// event, in declaration order.</summary>
    internal IEnumerable<string> CellFieldsOf(CellLifetime lifetime) =>
        _ptrAddressableBackings.Where(b => LifetimeOfCell(b.CellField, b.Canonical) == lifetime).Select(b => b.CellField);

    /// <summary>Claim <paramref name="root"/> onto a cell when it is a group whose values ride managed slots (a
    /// strongly-typed group with an object-reference or pointer leaf), which a program activation carries only as a cell
    /// AREA (kb/Work PB1940): a program's RETURNING item, and a user-defined function's caller-side result temporary
    /// (§8.4.3.2.4 GR1 — the receiver of the function's RETURNING delivery, created as the function-identifier binds).</summary>
    internal void ClaimSlotCarriedCell(DataItem root, string what)
    {
        if (WiseOwl.Cobol.Compiler.Oo.OoClassTable.LeafCarried(root) && !AlreadyInACell(root) && CellCanCarry(root)
            && ForceStringCanonical(root, what) is { } cls)
            ClaimAddressableCell(cls);
    }

    /// <summary>True when <paramref name="root"/>'s storage is ALREADY a cell (or the pointer-routed window over one), so
    /// no claim may re-base it: a BASED item or an area formal (its implicit data-address pointer — §8.6.5), a record
    /// another claim already put on a per-instance cell, or an EXTERNAL record (the run-unit <c>ExternalStore</c> cell).</summary>
    internal bool AlreadyInACell(DataItem root) =>
        root.IsBased
        || root.Class is { BasedPointerField: not null }
        || root.Class is { } existing && PtrAddressableCellOf.ContainsKey(existing)
        || root.Class is { Tier: RedefinesTier.StringCanonical } ext && CallExternalBackings.Any(b => b.BackingClrName == ext.BackingClrName);

    /// <summary>Record a forced class's per-instance cell. ⛔ THE CLASS NAMES ITS OWN CELL (kb/Work PB231): the field used
    /// to carry an ad-hoc <c>_cell_{NAME}</c> spelling that only this list knew, so a place builder could not reach the
    /// cell to address the area's MANAGED SLOTS. It is now <see cref="RedefinesClass.BackingCellClrName"/>, the ONE name
    /// every cell surface uses and <c>PlaceBuilder.BuildCellPath</c> resolves.</summary>
    internal void ClaimAddressableCell(RedefinesClass cls)
    {
        string cell = cls.BackingCellClrName;
        _ptrAddressableCellOf[cls] = cell;
        _ptrAddressableBackings.Add((cls.BackingClrName, cell, cls.Canonical, cls.Width));
    }
}
