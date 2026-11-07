// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;

using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// The data-pointer half of the data binder (Phase-4b increment 2 — ISO §2023:13.18.5 BASED / §2023:8.4.3.11 ADDRESS OF;
/// the PHASE4_RECONCILIATION "M2-DATA-5 / M2-PROC-5 — increment 2" design). Both jobs are instances of the ONE
/// cell-backing mechanism (<see cref="ForceStringCanonical"/>, the factored EXTERNAL re-basing): a BASED 01/77
/// becomes a storage TEMPLATE whose backing is a pointer-deref bridge (<c>ref CobolPtr.Deref(__addr_X, w).Ref</c>
/// — every reference windows the ADDRESSED cell at the pointer's runtime offset); an ADDRESS-OF-taken item's
/// record moves onto a per-instance <see cref="WiseOwl.Cobol.Runtime.StorageCell"/> so a
/// <see cref="WiseOwl.Cobol.Runtime.CellPointer"/> can alias its storage with structural (§2023:8.8.4.2) equality.
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>The group's one walk of the parse tree for storage-claim operands (<see cref="BindSession.ClaimScans"/>; kb/Work
    /// PB4302), shared by every unit's binder because the units share the tree — a unit's question about the programs it
    /// contains is then a slice of the container-independent answer, not a re-walk. A binder made outside a session (direct
    /// test construction) walks for itself.</summary>
    internal StorageClaimScan ClaimScans { get; init; } = new();

    /// <summary>⛔ WHO ENDS A CELL'S LIFE, ONE CLASSIFICATION (kb/Work PB1216). ISO §2023:13.18.5.4 GR4 raises EC-BOUND-PTR for a
    /// reference through an address that is "not a valid address of storage", and §2023:8.6.5 says the association "may cease
    /// to exist because the actual data no longer exists, as specified 8.6.4" — so every cell-backed record's storage
    /// duration (§2023:8.6.4) decides which event ENDS the cell, and this is the one place the three are told apart:
    /// <list type="bullet">
    /// <item><see cref="CellLifetime.Static"/> — a RECURSIVE unit's static WORKING-STORAGE and a method's WORKING-STORAGE
    /// (<see cref="StorageTierSlice.StaticAddressableCells"/>): one copy, re-seeded IN PLACE by <c>__ResetStatics</c> at a CANCEL, which
    /// ends the old life (<c>StorageCell.Reinitialize</c>).</item>
    /// <item><see cref="CellLifetime.Activation"/> — LOCAL-STORAGE and non-formal LINKAGE (a program's, a method's):
    /// "persists while that instance of the runtime element is in active state", so the cell ends at the activation's exit
    /// — inside <c>Call</c> for a program, in the method's <c>finally</c> (<c>OoEmitter.ActivationPointerSeeds</c>).</item>
    /// <item><see cref="CellLifetime.Instance"/> — a program's WORKING-STORAGE cell owned by its instance: an INITIAL
    /// program's items end at its activation's exit, any other program's at the CANCEL that drops the instance, both
    /// through the emitted <c>EndStorage</c> (<c>ICobolProgram.EndStorage</c>).</item>
    /// </list>
    /// A cell added to <see cref="StorageTierSlice.AddressableBackings"/> tomorrow is classified here and so ended by the matching
    /// event without an edit elsewhere; <c>CellLifetimeDriftTests</c> reads the generated program and fails if a cell
    /// field is named by none of them.</summary>
    private CellLifetime LifetimeOfCell(string cellField, DataItem canonical) =>
        State.StorageTier.StaticAddressableCells.Contains(cellField) ? CellLifetime.Static
        : Oo.MethodScopedRoots.Contains(canonical) || State.Forest.LocalStorageRoots.Contains(canonical)
          || Linkage.Roots.Contains(canonical) ? CellLifetime.Activation
        : CellLifetime.Instance;

    /// <summary>Each cell of <see cref="StorageTierSlice.AddressableBackings"/> with its storage duration (<see cref="LifetimeOfCell"/>),
    /// the classification <see cref="DataBindResult.CellLifetimes"/> carries to the backend (kb/Work PB2361).</summary>
    private Dictionary<string, CellLifetime> CellLifetimes() =>
        State.StorageTier.AddressableBackings.ToDictionary(b => b.CellField, b => LifetimeOfCell(b.CellField, b.Canonical), StringComparer.Ordinal);

    /// <summary>Claim <paramref name="root"/> onto a cell when it is a group whose values ride managed slots (a
    /// strongly-typed group with an object-reference or pointer leaf), which a program activation carries only as a cell
    /// AREA (kb/Work PB1940): a program's RETURNING item, claimed by <c>PtrBindBasedAndAddressablesPass</c> with the other cell
    /// claims, in the data phase. The user-defined function's result temporary is the same claim made LATE, by
    /// <see cref="ClaimTemporarySlotCarriedCell"/>.</summary>
    internal void ClaimSlotCarriedCell(DataItem root, string what)
    {
        if (SlotCarriedCellClass(root, what) is { } cls)
            State.StorageTier.ClaimAddressableCell(cls);
    }

    /// <summary>⛔ THE LATE DOOR OF <see cref="StorageTierSlice"/> (design §8.3 (1)): the same claim for a user-defined
    /// function's caller-side result temporary (§2023:8.4.3.2.4 GR1 — the receiver of the function's RETURNING delivery), which
    /// exists only once the function-identifier binds, in the PROCEDURE phase, after the cell claims were made and the static
    /// channels routed. The temporary is the procedure binders' own and per-activation, so the claim never reaches a static
    /// channel; <see cref="StorageTierSlice.ClaimTemporaryCell"/> says so, and <c>UdfBinder</c> is its one caller.</summary>
    internal void ClaimTemporarySlotCarriedCell(DataItem temp, string what)
    {
        if (SlotCarriedCellClass(temp, what) is { } cls)
            State.StorageTier.ClaimTemporaryCell(cls);
    }

    /// <summary>The class <paramref name="root"/> is forced onto a cell as when it is a slot-carried group no cell holds yet and
    /// one can carry; null otherwise (the common test of both claims above).</summary>
    private RedefinesClass? SlotCarriedCellClass(DataItem root, string what) =>
        ActivationCrossing.LeafCarried(root) && !AlreadyInACell(root) && CellCanCarry(root)
            ? ForceStringCanonical(root, what) : null;

    /// <summary>True when <paramref name="root"/>'s storage is ALREADY a cell (or the pointer-routed window over one), so
    /// no claim may re-base it: a BASED item or an area formal (its implicit data-address pointer — §2023:8.6.5), a record
    /// another claim already put on a per-instance cell, or an EXTERNAL record (the run-unit <c>ExternalStore</c> cell).</summary>
    internal bool AlreadyInACell(DataItem root) =>
        root.IsBased
        || root.Class is { BasedPointerField: not null }
        || root.Class is { } existing && State.StorageTier.AddressableCellOf.ContainsKey(existing)
        || root.Class is { Tier: RedefinesTier.StringCanonical } ext && Linkage.ExternalBackings.Any(b => b.BackingClrName == ext.BackingClrName);

    /// <summary>The ONE cell-backing forcer (increment-2 factoring of the proven EXTERNAL re-basing —
    /// feedback_one_mechanism_per_job): make <paramref name="item"/>'s class Tier-B StringCanonical with NO stored
    /// member, so a heap-cell-backed <c>ref</c>-property with the class's <see cref="RedefinesClass.BackingClrName"/>
    /// can supply the storage (EXTERNAL records → the run-unit <c>ExternalStore</c> cell; ADDRESS-OF-taken items →
    /// a per-instance <c>StorageCell</c>; BASED items → the pointer-deref bridge). Every byte-form numeric
    /// leaf rides the cell since the Step D arm-1 dissolution (the gate reads THE ONE image predicate);
    /// a national or pointer-class leaf fails it — the class goes Rejected and every reference fails loud
    /// (the caller skips its bridge registration). Returns the forced class, or null on rejection.
    /// <para>The admitted/refused population is <see cref="ByteWindowResidueOf"/>'s — THE ONE byte-window
    /// carriage gate, shared with the REDEFINES classifier (kb/Work PB231).</para></summary>
    internal RedefinesClass? ForceStringCanonical(DataItem item, string what)
    {
        // ⛔ ONLY THE DECLARING ELEMENT DECIDES WHERE ITS STORAGE LIVES (§2023:13.18.27.4 GR2: a contained program references a
        // container's GLOBAL item "without describing it again"). An inherited item's class is the container's, already
        // classified and emitted there; re-basing it from here would re-classify the container's storage and declare its
        // cell a second time in this unit's type (merge group 41183181188). Every caller asks with storage it owns.
        if (State.Forest.DeclaringDepth(item) != 0)
            throw new InvalidOperationException($"{what} '{item.CobolName}': a container's GLOBAL item reached the "
                + "cell-backing forcer of a contained unit; only its declaring element may re-base its storage");
        var cls = item.Class;
        if (cls is null)
        {
            cls = new RedefinesClass { Canonical = item };
            cls.Members.Add(item);
            item.Class = cls;
        }

        var leaves = cls.Members.SelectMany(LeavesOf).ToList();
        // ⛔ THE SAME gate as the main classifier — literally, now: ByteWindowResidueOf, ONE predicate for
        // both surfaces (kb/Work PB231). This site was a hand-written ALLOW-list facing ComputeTier's
        // hand-written DENY-list, and every widening had to be applied twice. It was not: the Step D arm-1
        // dissolution widened both (the eighth two-arm instance — ComputeTier admitted COMP/PACKED while this
        // gate refused every non-DISPLAY leaf), but PB203's USAGE BIT widening reached only ComputeTier, so a
        // BASED / EXTERNAL / ADDRESS-OF record with a bit leaf still rejected while its REDEFINES twin ran.
        // §2023:13.18.5.3 SR1/SR2 restrict a BASED subject to "shall not be of class object" and "shall not be a
        // dynamic-length elementary item or a variable-length group"; §2023:14.9.3.3 SR1 asks only for the BASED
        // clause; §2023:13.18.22 conditions EXTERNAL on nothing subordinate at all. A bit leaf is admitted by all
        // three, so the reject was rejects-legal-source — the same argument that retired the negative fixture
        // pb151-based-comp-leaf at the PB164 landing. Its residue clause now names the ACTUAL leaf.
        if (CellResidueOf(leaves) is { } residue)
        {
            cls.Classify(RedefinesTier.Rejected, cls.Width,
                $"{what} '{item.CobolName}' has {residue} — the shared byte cell cannot carry it");
            return null;
        }

        // The sanctioned RE-classification (see RedefinesClass.Classify): the cell re-base overrides any prior
        // stored-member verdict with the cell-backed Tier-B form (§2023:13.18.22.4 GR5).
        // The cell's area is the members' maximum STORAGE extent, in BYTES — §2023:14.9.3.4 GR3's "the amount of
        // storage to be allocated is the number of BYTES required to hold an item as described by data-name-1",
        // and §2023:13.18.44.4 GR1's larger-of-the-two for a multi-member class. Character positions are NOT that
        // number for a NATIONAL leaf (two bytes per position, §2023:13.18.60.4 GR8 / D-N1) — kb/Work PB231.
        cls.Classify(RedefinesTier.StringCanonical, cls.Members.Max(m => m.ByteWidth * (m.Occurs ?? 1)),
            rejectReason: null);
        // ⛔ THE CELL CLAIM, made HERE because this method IS the one cell-backing forcer and its three callers
        // ARE the three cell surfaces: from now on the class has a StorageCell behind its backing
        // (RedefinesClass.BackingCellClrName), which is what a pointer-class member's MANAGED SLOT addresses
        // (kb/Work PB231 — the pointer third; the implementor-defined D-SLOT representation, DOC-A.1-210 and DOC-A.1-216). A REDEFINES class never reaches this line and
        // never gets one, which is exactly why ComputeTier still refuses a pointer leaf.
        cls.IsCellBacked = true;
        foreach (var member in cls.Members)
        {
            AssignClassOffsets(member, 0, cls);
            member.IsCanonical = false;   // NO local stored field — the backing is the cell bridge
        }
        // ⛔ THE CELL'S VARIABLE-LENGTH HALF (kb/Work PB1026, PB1042): each COMPONENT — a dynamic-length item or a
        // dynamic-capacity table (Place.CellComponents) — rides a managed slot of the cell numbered in storage order,
        // one per occurrence of every enclosing fixed table, and a table's element is a scope of its own
        // (AssignCellComponents). A dynamic-length item occupies zero bytes of the backing — its ByteWidth — and a
        // table reserves its one-element ByteWidth, so the class walk above has already laid out the FIXED RUN every
        // variable-length group view of the class composes over. §2023:8.5.1.9.1 3) — "it may be defined in any place,
        // other than the file section, in which a fixed-capacity table may be defined" — is why an EXTERNAL or
        // ADDRESS-OF-taken record carries the table rather than refusing it.
        int dynOrdinal = 0;
        foreach (var member in cls.Members) dynOrdinal = AssignCellComponents(member, dynOrdinal);
        // ⛔ EVERY byte-form numeric leaf windowed over the string backing decodes/encodes its IMAGE — the
        // same rule ClassifyRedefinesClasses applies to a Tier-B class, applied here for the synth class.
        // The predicate MUST be the one CellCapable admits above (THE ONE image predicate): the Step D
        // widening opened the GATE to COMP/PACKED/COMP-5/float/INDEX leaves while this consequence still
        // read the pre-wave DISPLAY-only union, so an admitted non-DISPLAY leaf got a NATIVE storage form
        // over a STRING window — the accessors then spliced a raw long/float into the cell and read window
        // TEXT back into CobolNum.Store, i.e. UNCOMPILABLE generated C# (CS1503/CS0019) on legal source,
        // strictly worse than the bind diagnostic it replaced. The two-arm shape, nine lines apart.
        foreach (var leaf in leaves)
            if (leaf.Pic is { Category: PicCategory.Numeric, HasImageByteForm: true })
                State.ImageForcedItems.Add(leaf);   // the collected image fact
        return cls;
    }

    /// <summary>The first leaf of <paramref name="leaves"/> the shared byte cell cannot carry, as its residue clause, or
    /// null — <see cref="ByteWindowResidueOf"/> over a whole storage area, the ONE gate <see cref="ForceStringCanonical"/>
    /// applies.</summary>
    private static string? CellResidueOf(IEnumerable<DataItem> leaves) =>
        leaves.Select(ByteWindowResidueOf).FirstOrDefault(r => r is not null);

    /// <summary>⛔ CAN <paramref name="root"/>'s STORAGE AREA MOVE ONTO A CELL WITHOUT A DIAGNOSTIC (kb/Work PB2087)? The
    /// question an OPTIONAL claim asks before calling <see cref="ForceStringCanonical"/>, which classifies a refused
    /// class Rejected (every reference then fails loud) — right for a surface that cannot exist without the cell
    /// (EXTERNAL, BASED, ADDRESS OF), wrong for one that only gains aliasing from it (a boundary-aliased area). True
    /// when every leaf of the area rides the cell.
    /// <para>⛔ A VARIABLE-LENGTH GROUP RIDES THE CELL TOO (kb/Work PB2094). Its dynamic-length items and dynamic-capacity
    /// tables are the cell's components (<see cref="AssignCellComponents"/>), and an area formal laid over its argument
    /// numbers its components from the area's component ordinal as it displaces its characters by the area's offset
    /// (<c>CellPointer.DynBase</c>, <c>PlaceBuilder.CellOrdinalBase</c>), so §2023:14.2.3 GR8 — "the activated runtime
    /// element operates as if the formal parameter occupies the same storage area as the argument" — holds for it as
    /// for any group. §2023:13.18.5.3 SR2 bars a variable-length group only as the subject of a WRITTEN BASED clause.</para></summary>
    internal static bool CellCanCarry(DataItem root)
    {
        var members = root.Class?.Members ?? [root];
        return CellResidueOf(members.SelectMany(LeavesOf)) is null;
    }

    /// <summary>Number <paramref name="node"/>'s variable-length COMPONENTS in its scope from ordinal
    /// <paramref name="next"/> and return the next free ordinal (kb/Work PB1042) — the ONE writer of
    /// <see cref="DataItem.ClassDynOrdinal"/>, walking exactly the order <see cref="CellComponents.Of"/> enumerates: a
    /// component takes one ordinal; a group's children follow in storage order, a fixed table's occurrence k taking
    /// the k-th run of its per-occurrence components (so a subscript displaces an ordinal by
    /// <see cref="CellComponents.PerOccurrence"/>); a non-component's ordinal is that of its first component. A
    /// dynamic-capacity table's ELEMENT is a scope of its own — each occurrence is an element cell
    /// (<c>StorageCell.DynTableAt</c>) — so the element's members are re-laid out from offset zero and numbered from
    /// zero, which is what makes a table, or a dynamic-length item, inside a table element the same mechanism one
    /// level down (§2023:8.5.1.9.1 3): "may be nested in any combination to the same number of levels as a fixed-capacity
    /// table").</summary>
    private static int AssignCellComponents(DataItem node, int next)
    {
        node.ClassDynOrdinal = next;
        if (node.IsDynamicTable)
        {
            (int at, int bit) = (node.ClassOffset, node.ClassBitOffset);
            AssignClassOffsets(node, 0, node.Class!);   // the element's own layout, relative to its element cell
            (node.ClassOffset, node.ClassBitOffset) = (at, bit);   // the table keeps its reservation in its parent scope
            int inner = 0;
            foreach (var c in node.Children.Where(c => c.RedefinesTargetName is null))
                inner = AssignCellComponents(c, inner);
            return next + 1;
        }
        if (DynSlotWindow.CarriedBySlot(node)) return next + 1;
        int k = next;
        foreach (var c in node.Children.Where(c => c.RedefinesTargetName is null))
            k += (AssignCellComponents(c, k) - k) * (c.Occurs ?? 1);
        return k;
    }
}
