// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// Which range-collation carrier a THROUGH range's <c>IN alphabet-name-1</c> phrase named (ISO §2023:14.7.8 rule 2). The unit
/// whose binder resolved the phrase makes one per alphabet it names (its <see cref="DataBindResult.RangeCollations"/>), and the
/// range's node carries it (<see cref="Bound.BoundRangeMembership.Collation"/>, <see cref="Condition88.Collation"/>), so the
/// backend looks no alphabet-name up (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, slice 4": a name table is the
/// symbol table's, and the backend cannot read the binder's).
/// <para>⛔ AN IDENTITY, NEVER A POSITION. A level-88 node crosses units: a container's GLOBAL condition-name (§2023:13.18.27.4 GR1,
/// §2023:13.18.27.4 GR2) is rendered in every contained program that references it, and a clone of a GLOBAL TYPEDEF's
/// condition-name keeps the template's phrase. A position in the DEFINING unit's carrier list read against the RENDERING unit's
/// list names a different carrier or none (kb/Work PB5932, review round 1 of step 6b slice 4: a contained program answered a
/// container's GLOBAL <c>88 OK VALUE "B" THRU "A" IN AL</c> in the wrong sequence, or the compiler failed). The backend finds the
/// carrier by this object's identity in the rendering unit's own carriers and then in its containers'
/// (<c>CodeGen.Emit.RangeCollationFields</c>).</para>
/// <para>It states the carrier as STRUCTURE (which sequence it is, and its ordinal) and the backend spells the field
/// (<c>ObjectComputerEmit.RangeCollationField</c>), so no C# name rides the model (DESIGN-backend-abstraction section 1.2).</para>
/// </summary>
/// <param name="name">alphabet-name-1 as first written, for a diagnostic and the carrier's declaration comment.</param>
/// <param name="sequence">Which sequence the carrier is, as the unit that made it sees it.</param>
/// <param name="ordinal">The carrier's ordinal among its unit's carriers, in the order first named.</param>
public sealed class RangeCollationId(string name, RangeCollationSequence sequence, int ordinal)
{
    /// <summary>alphabet-name-1 as first written.</summary>
    public string Name { get; } = name;

    /// <summary>Which sequence the carrier is, as the unit that made it sees it.</summary>
    public RangeCollationSequence Sequence { get; } = sequence;

    /// <summary>The carrier's ordinal among its unit's carriers, in the order first named. A field of its own is named by it, never by
    /// the alphabet-name: a user-defined word admits both '-' and '_' (§2023:8.3.2.1), so any sanitizing map could collide two
    /// distinct alphabet-names onto one field.</summary>
    public int Ordinal { get; } = ordinal;

    /// <inheritdoc/>
    public override string ToString() => Name;
}

/// <summary>What a range-collation carrier is to the unit that made it (ISO §2023:14.7.8 rule 2: "the collating sequence used for
/// range evaluation is the collating sequence defined by that alphabet").</summary>
public enum RangeCollationSequence
{
    /// <summary>The native order (an identity alphabet): no field, and the native two-argument comparison IS the sequence.</summary>
    Native,

    /// <summary>The unit's alphanumeric PROGRAM COLLATING SEQUENCE, whose field the unit already declares.</summary>
    ProgramCollating,

    /// <summary>The unit's national PROGRAM COLLATING SEQUENCE, whose field the unit already declares.</summary>
    NationalProgramCollating,

    /// <summary>A sequence of its own, whose field the unit declares for it.</summary>
    Own,
}
