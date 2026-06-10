// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// Table-element access (ISO §8.4.2.3 subscripting). Every emitted subscripted reference routes through
/// <see cref="At{T}"/> — the ONE occurrence-number→element mapping.
/// </summary>
public static class CobolTable
{
    /// <summary>
    /// The table element at a 1-based <paramref name="occurrence"/> number, as a writable reference.
    /// <para><b>Out-of-range</b> (ISO §8.4.2.3.4 GR2): with subscript CHECKING off — the COBOL-85 semantics, since
    /// COBOL-85 has no exception conditions, and the 2002+ default until the EC model lands — the reference
    /// continues benignly per the implementor-defined rule the NIST-85 golden requires: reads see a fresh
    /// default-valued element (spaces-equivalent for alphanumeric), writes are absorbed. The element is a per-type
    /// scratch slot, re-defaulted on every out-of-range access. (When EC checking arrives, CHECKING ON maps this
    /// to EC-BOUND-SUBSCRIPT instead.) Caveat: a GROUP element's scratch is a zeroed struct — its string members
    /// are null, so group-level use of an out-of-range element may still fail loudly; acceptable until a real
    /// corpus case demands per-type construction.</para>
    /// </summary>
    public static ref T At<T>(T[] table, long occurrence)
    {
        if (occurrence >= 1 && occurrence <= table.Length) return ref table[(int)(occurrence - 1)];
        Scratch<T>.Slot = typeof(T) == typeof(string) ? (T)(object)string.Empty : default!;
        return ref Scratch<T>.Slot;
    }

    private static class Scratch<T>
    {
        public static T Slot = default!;
    }

    /// <summary>A subscript data item's occurrence-number value. The two overloads let the compiler emit ONE
    /// bind-time expression for a subscript read whose backing field's storage form (native <c>long</c> vs the
    /// character image a post-bind whole-group analysis selects) is decided later — C# overload resolution picks
    /// the right conversion at backend-compile time.</summary>
    public static long Occ(long value) => value;

    /// <inheritdoc cref="Occ(long)"/>
    public static long Occ(string image) => (long)CobolNum.FromAlphanumeric(image);
}
