// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// ⛔ WHICH OCCURS DEPENDING ON OBJECTS DOES IDENTIFYING A <see cref="Place"/> READ? — the ONE answer, for every place kind
/// (kb/Work PB5119 review R1-1). Identifying a data item evaluates "length evaluation for an occurs-depending group item"
/// as its step 6, before reference modification (ISO §2023:14.6.4), and "the identifiers within a statement are evaluated
/// in left to right order as the first operation of the execution of that statement"; the part of the table area an
/// occurs-depending group operand uses is fixed "by the value of the data item referenced by data-name-1 at the start of
/// the operation" (§2023:13.18.38.4 GR8 a and b), and a reference to the subject of the entry or to an item subordinate
/// to it tests data-name-1's bounds at the time it is referenced (§2023:13.18.38.4 GR7). A statement whose operand is
/// read, or stored, AFTER the statement itself has stored into data-name-1 (a format 3 INSPECT whose TALLYING counter is
/// the object; a reference modifier whose function changes it) must therefore read the value the object had when the
/// operand was identified, whatever the operand's FORM: the plain occurs-depending group, a reference-modified one with
/// or without a length, the record above it, an element of the table whose reference carries the bound test.
/// <para><b>One rewrite, one roster.</b> <see cref="Rebind"/> hands every DEPENDING ON place an identification reads to
/// the caller's <c>pin</c> (the binder's statement-local copy of the object) and returns the place reading the pins; the
/// question <see cref="ReadsDependingObject"/> is the same walk with an identity pin, so the two cannot disagree. How
/// each place kind carries the object is the <see cref="Roles"/> roster, asserted complete against the assembly by
/// <c>OdoDependenceRosterDriftTests</c>: a new <see cref="Place"/> kind fails that test (and <see cref="Rebind"/> fails
/// loud) until its row states how its identification reads an occurs-depending object. A new
/// <see cref="PlaceDecorator"/> is automatic: its inner is walked.</para>
/// </summary>
internal static class OdoDependence
{
    /// <summary>How one place kind carries an occurs-depending object.</summary>
    internal enum Role
    {
        /// <summary>The occurs-depending group operand: its <see cref="OdoGroupPlace.Depending"/> sizes it (GR8), and
        /// its inner is walked.</summary>
        GroupExtent,

        /// <summary>A decoration over one inner place (a reference modifier, an image view, a table(ALL) element): the
        /// decoration reads no object of its own, the inner is walked.</summary>
        Decoration,

        /// <summary>A storage leaf addressed by an <see cref="AccessPath"/> whose table steps may carry the GR7 bound
        /// test (<see cref="FixedTableSegment.Odo"/>).</summary>
        AccessPath,

        /// <summary>A leaf whose identification reads no occurs-depending object.</summary>
        NoObject,
    }

    /// <summary>⛔ THE ROSTER — every concrete <see cref="Place"/> kind, with the role its identification plays; the
    /// comment on each row is why. Adding a kind is an adjudication: name how its identification reads an occurs-depending
    /// object, or the rule that says it reads none. (Only the role is data: a reason is documentation, and the backend
    /// input carries no text the contract has not classified, <c>BackendContractDriftTests</c>.)</summary>
    internal static readonly IReadOnlyDictionary<Type, Role> Roles = new Dictionary<Type, Role>
    {
        // An occurs-depending group operand: §2023:13.18.38.4 GR8 sizes it from data-name-1 at the start of the operation.
        [typeof(OdoGroupPlace)] = Role.GroupExtent,
        // Reference modification is step 7 of §2023:14.6.4, after step 6's length: its own positions are hoisted by the code
        // generator (PlaceIdentification.Freeze), and an omitted length reads the inner's extent.
        [typeof(RefModPlace)] = Role.Decoration,
        // A table(ALL) argument (§2023:15.3) or a whole table: the element is walked; its Counts are read once, by the one
        // enumeration that evaluates the function or the debugging image, so no statement re-reads them.
        [typeof(TableAllPlace)] = Role.Decoration,
        // Codings of the inner item: the numeric character image (§2023:8.4.3.3.4 GR6), a national cell's bytes
        // (§2023:13.18.45.4 GR2), the whole-group image, the as-if-elementary boolean and national views (§2023:13.18.29.4
        // GR1 b, GR2 b).
        [typeof(NumericImagePlace)] = Role.Decoration,
        [typeof(NationalBytesPlace)] = Role.Decoration,
        [typeof(GroupImagePlace)] = Role.Decoration,
        [typeof(BitImagePlace)] = Role.Decoration,
        [typeof(NatImagePlace)] = Role.Decoration,
        // Storage leaves reached through table steps that may carry the §2023:13.18.38.4 GR7 bound test: a record struct's
        // member, an OCCURS DYNAMIC element (the fixed-table steps above it), a Tier-B window (its backing and cell paths).
        [typeof(MemberPlace)] = Role.AccessPath,
        [typeof(DynTablePlace)] = Role.AccessPath,
        [typeof(RedefViewPlace)] = Role.AccessPath,
        // A level-66 alias: §2023:13.18.45.3 SR3 keeps its operands out of any OCCURS and SR8 keeps an occurs-depending
        // table out of its range.
        [typeof(RenamesPlace)] = Role.NoObject,
        // The OCCURS DYNAMIC capacity register: a dynamic-capacity table has no DEPENDING ON object.
        [typeof(CapacityRegisterPlace)] = Role.NoObject,
        // Engine state and predefined references, not data-division table occurrences: a REPORT SECTION sum counter, a
        // report's PAGE-COUNTER, the DEBUG-ITEM register family, EXCEPTION-OBJECT.
        [typeof(ReportSumCounterPlace)] = Role.NoObject,
        [typeof(ReportPageCounterPlace)] = Role.NoObject,
        [typeof(DebugRegisterPlace)] = Role.NoObject,
        [typeof(ExceptionObjectPlace)] = Role.NoObject,
    };

    /// <summary>True when identifying <paramref name="place"/> reads an occurs-depending object — the same walk as
    /// <see cref="Rebind"/>.</summary>
    public static bool ReadsDependingObject(Place place)
    {
        bool reads = false;
        Rebind(place, depending => { reads = true; return depending; });
        return reads;
    }

    /// <summary>The place with every DEPENDING ON place its identification reads replaced by <paramref name="pin"/>'s
    /// answer, in the order identification reads them (the group's own inner path before its extent).</summary>
    public static Place Rebind(Place place, Func<Place, Place> pin) => (RoleOf(place), place) switch
    {
        (Role.GroupExtent, OdoGroupPlace o) => o with { Inner = Rebind(o.Inner, pin), Depending = pin(o.Depending) },
        (Role.Decoration, PlaceDecorator d) => d with { Inner = Rebind(d.Inner, pin) },
        (Role.AccessPath, MemberPlace m) => m with { Path = RebindPath(m.Path, pin) },
        (Role.AccessPath, DynTablePlace t) => t with { Path = RebindPath(t.Path, pin) },
        (Role.AccessPath, RedefViewPlace v) => v with
        {
            Backing = RebindPath(v.Backing, pin),
            Cell = v.Cell is { } cell ? RebindPath(cell, pin) : null,
        },
        (Role.NoObject, _) => place,
        var (role, _) => throw new InvalidOperationException(
            $"OdoDependence: {place.GetType().Name} is rostered as {role} but no arm of Rebind takes it (kb/Work PB5119)."),
    };

    private static Role RoleOf(Place place) =>
        Roles.TryGetValue(place.GetType(), out var role)
            ? role
            : throw new InvalidOperationException(
                $"OdoDependence: the place kind {place.GetType().Name} has no row in OdoDependence.Roles; state how its "
                + "identification reads an occurs-depending object (kb/Work PB5119).");

    private static AccessPath RebindPath(AccessPath path, Func<Place, Place> pin)
    {
        if (!path.Segments.Any(s => s is FixedTableSegment { Odo: not null })) return path;
        return new AccessPath([.. path.Segments.Select(s => s is FixedTableSegment { Odo: { } check } f
            ? f with { Odo = check with { Depending = pin(check.Depending) } }
            : s)]);
    }
}
