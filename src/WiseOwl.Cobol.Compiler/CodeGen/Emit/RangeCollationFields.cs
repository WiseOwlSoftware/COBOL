// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.CodeGen.Emit;

/// <summary>
/// ⛔ THE ONE reader of a THROUGH range's <c>IN alphabet-name-1</c> carrier for the unit being emitted (ISO §2023:14.7.8 rule 2):
/// the C# expression of the <c>CobolCollation</c> field a <see cref="RangeCollationId"/> names, as seen from this unit's type.
/// <para>A carrier is declared in the type of the unit whose binder made it (<c>ObjectComputerEmit</c>), and the node that
/// names it crosses units: a container's GLOBAL condition-name is rendered in every contained program that references it
/// (§2023:13.18.27.4 GR1, §2023:13.18.27.4 GR2), and a clone of a GLOBAL TYPEDEF's condition-name keeps the template's phrase. So
/// the lookup is by the node's identity, in this unit's own carriers first and then in each container's, outward; a container's
/// field is reached through the container's type, which encloses this one (a contained program is a nested class, and a
/// carrier is a static field). A position read against the rendering unit's list named a different carrier or none (kb/Work
/// PB5932, review round 1 of step 6b slice 4).</para>
/// <para>An identity no unit of the chain made is a compiler defect and fails loudly: the backend never guesses a sequence.</para>
/// </summary>
internal sealed class RangeCollationFields
{
    private readonly Dictionary<RangeCollationId, string?> _fields;

    private RangeCollationFields(Dictionary<RangeCollationId, string?> fields) => _fields = fields;

    /// <summary>The fields seen from the type of the unit whose data is <paramref name="own"/>, contained in
    /// <paramref name="container"/> (null for an outermost program, a class or an interface).</summary>
    public static RangeCollationFields Of(DataBindResult own, BoundUnit? container)
    {
        var fields = new Dictionary<RangeCollationId, string?>();
        foreach (var id in own.RangeCollationIds) fields.Add(id, ObjectComputerEmit.RangeCollationField(id));
        for (var u = container; u is not null; u = u.Parent)
            foreach (var id in u.Data.RangeCollationIds)
                fields.TryAdd(id, ObjectComputerEmit.RangeCollationField(id) is { } f ? $"{u.ClassName}.{f}" : null);
        return new(fields);
    }

    /// <summary>The field's expression, or null for an identity alphabet (the native two-argument comparison IS its sequence).</summary>
    public string? FieldOf(RangeCollationId id) => _fields.TryGetValue(id, out var f) ? f
        : throw new InvalidOperationException($"range collation '{id.Name}' was made by no unit this one is contained in");
}
