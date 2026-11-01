// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Linq;

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>The access direction of a subscripted OCCURS DYNAMIC element (data-model D9): a SENDING read (benign
/// scratch on out-of-range) vs a RECEIVING write (grows-and-seeds the table past its current capacity). A fixed
/// table ignores this — its <c>CobolTable.At</c> <c>ref T</c> serves both directions. It is resolved at RENDER time
/// (the backend picks the accessor from whether it is reading or writing), NOT baked into the segment.</summary>
public enum AccessDir { Sending, Receiving }

/// <summary>
/// A structural access path to a storage location (P7 Step 11 — structural <see cref="Place"/>): an ordered chain of
/// <see cref="AccessSegment"/>s the BACKEND renders to a C# lvalue expression (a field chain, table accessors), so a
/// <see cref="MemberPlace"/>/<see cref="RedefViewPlace"/>/<see cref="CapacityRegisterPlace"/> carries STRUCTURE, not
/// C# text. A table segment's subscript INDEX is a typed <see cref="Position"/> (kb/Work PB2151;
/// DESIGN-binder-bound-tree.md §3.9). The rendering lives in <c>CodeGen.PlaceRenderer.RenderPath</c>.
/// </summary>
public sealed record AccessPath(IReadOnlyList<AccessSegment> Segments)
{
    /// <summary>Extend the path with one trailing segment (used by the cursor walks in the binder).</summary>
    public AccessPath Add(AccessSegment seg) => new([.. Segments, seg]);

    /// <summary>Re-anchor the path in a containing program (ISO §12.4.5.8.4 GR1 NOTE 1 — an inherited GLOBAL file's
    /// FILE STATUS item, resolved in the container that declares it): the ROOT segment gains the
    /// <see cref="ContainerAnchor"/> <paramref name="anchorOf"/> gives for its member. The anchor is asked PER ROOT because
    /// a container's static member is reached through its class and an instance member through its instance
    /// (<c>BoundUnit.AnchorOf</c>; kb/Work PB1133). The member name is unchanged: the anchor is structure each backend
    /// spells, never text joined onto the name (kb/Work PB4320).</summary>
    public AccessPath Reroot(Func<string, ContainerAnchor> anchorOf)
    {
        if (Segments.Count == 0 || Segments[0] is not RootFieldSegment root) return this;
        // The guard's presence member is Uid-keyed and bridged under the same name (kb/Work PB971), so only the
        // storage is re-anchored; the guard travels unchanged.
        var segs = new List<AccessSegment>(Segments.Count) { root with { Anchor = anchorOf(root.CsField) } };
        for (int i = 1; i < Segments.Count; i++) segs.Add(Segments[i]);
        return new AccessPath(segs);
    }

    /// <summary>True when any segment evaluates a subscript (a table access) — the structural replacement for the
    /// former <c>path.Contains("CobolTable.At(")</c> string-sniff (CORRESPONDING ref-vs-value anchoring).</summary>
    public bool HasIndex => Segments.Any(s => s is FixedTableSegment or DynTableSegment or CellTableSegment);
}

/// <summary>One step of an <see cref="AccessPath"/>.</summary>
public abstract record AccessSegment;

/// <summary>The root of the path — a static or instance member of the program class, or a statement local (a
/// CORRESPONDING anchor, a hoisted cell). <paramref name="CsField"/> is always a bare name, never an access expression
/// (kb/Work PB4320). <paramref name="Guard"/> is non-null when the root is a FORMAL PARAMETER whose reference can raise
/// its *-ARG-OMITTED condition (kb/Work PB971): the backend then renders the root through the runtime guard.
/// <paramref name="Anchor"/> is non-null when the member belongs to a CONTAINING program (<see cref="AccessPath.Reroot"/>)
/// and says how the referencing program reaches it; null for the referencing program's own member or local.</summary>
public sealed record RootFieldSegment(string CsField, OmittedFormalGuard? Guard = null, ContainerAnchor? Anchor = null)
    : AccessSegment;

/// <summary>The storage a reference-type CARRIER designates: the step from a carrier-resident formal parameter's
/// <c>ManagedPointer&lt;T&gt;</c> carrier — the path's root, <see cref="DataItem.ResidentCarrier"/> — to the argument
/// storage it aliases on every access (ISO §14.2.3 GR8: the activated element operates as if the formal parameter
/// occupies the same storage area as the argument). A structural step, so each backend lowers the access itself and
/// no name on the model holds the access text (kb/Work PB4320; DESIGN-backend-abstraction §1.3 L7).</summary>
public sealed record CarrierValueSegment : AccessSegment
{
    /// <summary>The one instance: the step carries no data.</summary>
    public static readonly CarrierValueSegment Instance = new();
    private CarrierValueSegment() { }
}

/// <summary>⛔ HOW A CONTAINED PROGRAM REACHES A MEMBER OF A CONTAINING PROGRAM. ISO §13.18.27.4 GR2: "A statement in a
/// program contained directly or indirectly within a program that describes a global name may reference that name
/// without describing it again" — the storage stays the container's. Decided once per member by
/// <c>BoundUnit.AnchorOf</c> (kb/Work PB1133) and carried as structure on a root segment and on a GLOBAL bridge
/// (<c>CallBridge</c>), so no model string holds the access text (kb/Work PB4320; DESIGN-backend-abstraction §1.3
/// L7); each backend spells it.</summary>
public abstract record ContainerAnchor
{
    private ContainerAnchor() { }

    /// <summary>An INSTANCE member of the <paramref name="Depth"/>-th containing program (1 = the directly containing
    /// one), reached through the chain of containing instances every contained program holds.</summary>
    public sealed record Instance(int Depth) : ContainerAnchor;

    /// <summary>A STATIC member of a containing program — a RECURSIVE unit's WORKING-STORAGE is "static data" (ISO
    /// §13.5.4 GR1), one copy on the class, which no instance names — reached through that program's class,
    /// <paramref name="ClassClrName"/>.</summary>
    public sealed record Static(string ClassClrName) : ContainerAnchor;
}

/// <summary>A <c>.Member</c> access on the accumulated path (a nested <c>record struct</c> member).</summary>
public sealed record MemberSegment(string CsMember) : AccessSegment;

/// <summary>A FIXED OCCURS subscript — the accumulated path is wrapped in <c>CobolTable.At(path, index)</c>
/// (ISO §8.4.2.3.4 GR2, benign out-of-range). <paramref name="OneBasedIndex"/> is the typed occurrence number.
/// <paramref name="Odo"/>, when present, is the level's OCCURS DEPENDING bound that the reference itself must test
/// (<see cref="OdoReferenceCheck"/>).</summary>
public sealed record FixedTableSegment(Position OneBasedIndex, OdoReferenceCheck? Odo = null) : AccessSegment;

/// <summary>⛔ §13.18.38.4 GR7 AT AN ELEMENT REFERENCE (kb/Work PB1268): "At the time the subject of entry is
/// referenced or any data item subordinate or superordinate to the subject of entry is referenced, the value of the
/// data item referenced by data-name-1 shall fall within the bounds from integer-1 through integer-2. If the value of
/// the data item does not fall within the specified bounds, the EC-BOUND-ODO exception condition is set to exist."
/// The SUPERORDINATE reference was already checked where a group's current extent is computed
/// (<c>CobolTable.OdoExtent</c>); a reference to the SUBJECT or to an item SUBORDINATE to it passes through this
/// table level's subscript, so the check rides the segment: <c>CobolTable.At(path, index, count, min, max)</c>.
/// Attached only when the compilation group can enable EC-BOUND-ODO at all (<c>DataBinder.OdoReferenceChecking</c>,
/// the zero-scaffolding invariant): with no such >>TURN the raise could never fire and the reference renders as
/// before. <paramref name="Depending"/> is data-name-1's place, resolved in the referencing unit's context.</summary>
public sealed record OdoReferenceCheck(Place Depending, int MinOccurs, int MaxOccurs);

/// <summary>An OCCURS DYNAMIC subscript (§8.5.1.9.2/.9.3, D9) — the accessor is direction-specific
/// (<c>RefSending</c> on a read, <c>RefReceiving</c> on a write), chosen at RENDER time from the operation.
/// <paramref name="OneBasedIndex"/> is the typed occurrence number.</summary>
public sealed record DynTableSegment(Position OneBasedIndex) : AccessSegment;

/// <summary>A CELL-BACKED area's dynamic-capacity table (kb/Work PB1042; <see cref="CellComponents"/>): the
/// accumulated path is the area's <c>StorageCell</c> (or an element cell of an enclosing such table) and this step
/// reaches its component <paramref name="Ordinal"/> — <c>StorageCell.DynTableAt</c>, a <c>CobolDynTable</c> whose
/// occurrences are element cells. <paramref name="Ordinal"/> is the typed component ordinal (a
/// <see cref="PositionOffset"/> carrying a subscript term for each enclosing fixed table level). <paramref name="Min"/> and <paramref name="ElementWidth"/>
/// are the REFERENCING description's OCCURS facts — its FROM minimum and its element's storage width — which the
/// runtime asks the shared table to agree with (ISO §14.6.13.2 rule 6).</summary>
public sealed record CellTableSegment(Position Ordinal, int Min, int ElementWidth) : AccessSegment
{
    /// <summary>The step to <paramref name="table"/>'s component at <paramref name="ordinal"/>, carrying the table's
    /// own description.</summary>
    public static CellTableSegment Of(DataItem table, Position ordinal) =>
        new(ordinal, table.OccursSpec?.InitialCap ?? 0, table.ByteWidth);
}
