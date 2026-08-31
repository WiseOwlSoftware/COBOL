// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;
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
        // A NULL table is itself an out-of-range chain: a multi-dimension reference whose OUTER subscript was
        // out of range continues through the zeroed scratch struct, whose nested OCCURS arrays are null
        // (NC401M's 5-deep FAIL-path read) — every further level resolves benignly too.
        if (table is not null && occurrence >= 1 && occurrence <= table.Length) return ref table[(int)(occurrence - 1)];
        // §8.4.2.3.4 GR2 — "If the value of the subscript is not a positive integer or is less than one or is
        // greater than the highest permissible occurrence number, the EC-BOUND-SUBSCRIPT exception condition is
        // set to exist." Raised BEFORE the scratch fallback so a CHECKING-ON reference reports the condition;
        // with checking off the helper returns and the scratch read below stands unchanged.
        ExceptionState.SubscriptError(
            $"subscript {occurrence} is outside 1..{(table?.Length ?? 0)} (ISO 8.4.2.3.4 GR2)");
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

    /// <summary>A SCALED subscript expression's occurrence number (ISO §8.4.2.3.4 GR1b; fix-queue PB41): "the
    /// subscript is the result of the evaluation of arithmetic-expression-1. If the evaluation of
    /// arithmetic-expression-1 does not result in an integer, the EC-BOUND-SUBSCRIPT exception condition is set to
    /// exist." A COBOL.NET numeric item stores UNSCALED, so <c>PIC 9V9 VALUE 2.0</c> is the field <c>20L</c> at
    /// scale 1 — the VALUE is 2 and the STORAGE is 20. The scale-less overloads above are the scale-0 fast path and
    /// stay byte-identical; these carry the scale the item's PICTURE declares.
    /// <para>The three arities exist for the same reason the scale-less pair does: a subscript item's storage form
    /// (native <c>long</c>, the character image a post-bind whole-group analysis may select, or the
    /// <see cref="Int128"/> wide tier a D18 function-subscript temp uses) is decided AFTER the bind-time
    /// expression text is produced, so C# overload resolution picks the conversion at backend-compile time.</para>
    /// <para>With EC-BOUND-SUBSCRIPT checking OFF the fractional position truncates toward zero and the reference
    /// continues — the same lenient posture <see cref="At{T}"/> takes for an out-of-range occurrence, and
    /// conforming for the same reason: the standard names the condition, and leaves the checking-off outcome to
    /// the implementor.</para></summary>
    public static long Occ(long unscaled, int scale) => OccScaled(unscaled, scale);

    /// <inheritdoc cref="Occ(long,int)"/>
    public static long Occ(string image, int scale) => OccScaled(CobolNum.FromAlphanumeric(image), scale);

    /// <inheritdoc cref="Occ(long,int)"/>
    public static long Occ(Int128 unscaled, int scale) => OccScaled(unscaled, scale);

    private static long OccScaled(Int128 unscaled, int scale)
    {
        if (CobolNum.HasFraction(unscaled, scale))
            ExceptionState.SubscriptError(
                $"subscript value {CobolNum.PlainValue(unscaled, scale)} is not an integer (ISO 8.4.2.3.4 GR1b)");
        return CobolNum.PositionOf(unscaled, scale);
    }

    /// <summary>The CURRENT character extent of an occurs-depending GROUP operand (ISO/IEC 1989:2023 §13.18.38
    /// GR8): the fixed prefix plus data-name-1's value clamped to [0, max] occurrences, times the per-occurrence
    /// width. A count outside integer-1..integer-2 makes the excess content undefined (GR7); the benign clamp is
    /// the COBOL-85 policy (no exception conditions) and the 2002+ default until EC-BOUND-ODO checking lands.</summary>
    /// <param name="min">integer-1 of the OCCURS DEPENDING clause — the LOWER bound §13.18.38.4 GR7 requires the
    /// control value to fall within. It was previously absent and the floor hardcoded to 0, so a below-minimum
    /// DEPENDING value silently clamped instead of raising.</param>
    public static int OdoExtent(long count, int min, int max, int fixedUnits, int elemUnits)
    {
        // §13.18.38.4 GR7 — the value "shall fall within the bounds from integer-1 through integer-2. If the
        // value of the data item does not fall within the specified bounds, the EC-BOUND-ODO exception condition
        // is set to exist." Both ends matter; checking off keeps the clamp, whose result GR7's closing sentence
        // makes undefined content and therefore a conforming implementor choice.
        if (count < min || count > max)
            ExceptionState.OdoError(
                $"OCCURS DEPENDING value {count} is outside {min}..{max} (ISO 13.18.38.4 GR7)");
        long c = count < 0 ? 0 : count > max ? max : count;
        return fixedUnits + (int)c * elemUnits;
    }

    /// <summary>The current CHARACTER extent of an occurs-depending GROUP operand whose positions are counted in
    /// <paramref name="positionsPerChar"/> units (1 for a character subtree, 8 for one holding USAGE BIT leaves —
    /// ISO/IEC 1989:2023 §8.5.1.6.3, kb/Work PB173): <see cref="OdoExtent"/> rounded UP to whole characters. The
    /// ceiling is where §8.5.1.6.3's trailing-filler rule puts the group's own partial byte, and it is the same
    /// rounding §15.50.4 r9 requires of every other bit width. With <paramref name="positionsPerChar"/> = 1 it is
    /// the identity, so the character channel has ONE arm, never a units branch.</summary>
    public static int OdoExtentChars(long count, int min, int max, int fixedUnits, int elemUnits, int positionsPerChar)
    {
        int units = OdoExtent(count, min, max, fixedUnits, elemUnits);
        return positionsPerChar <= 1 ? units : (units + positionsPerChar - 1) / positionsPerChar;
    }

    // ── The table(ALL) intrinsic-argument enumeration (ISO §15.3; kb/Work PB62) ─────────────────────────────

    /// <summary>
    /// The argument list a <c>table(… ALL …)</c> intrinsic argument stands for (ISO §15.3 — "When ALL is specified
    /// as a subscript, the effect is as if each table element associated with that subscript position were
    /// specified. The order … is from left to right, with the first … specification being the identifier with
    /// each subscript specified by the word ALL replaced by one, the next … the rightmost subscript specified by
    /// the word ALL incremented by one …"): a row-major enumeration over the ALL levels' RANGES, outermost first.
    /// Each level's range is read when the level is ENTERED, as a function of the outer indices — a fixed OCCURS
    /// count, an OCCURS DEPENDING table's current count, or a dynamic-capacity table's current capacity ("from 1 to
    /// the current capacity of the table"), which for a table nested inside another depends on the outer occurrence.
    /// <para>"The evaluation of an ALL subscript shall result in at least one argument, otherwise the result of the
    /// reference to the function-identifier is undefined." COBOL.NET defines the undefined case as
    /// EC-ARGUMENT-FUNCTION (set when checking is on) and terminates the reference with that name either way — a
    /// zero-argument list is never handed to a body whose result over nothing is itself undefined.</para>
    /// </summary>
    public static T[] AllArgs<T>(Func<long[], long>[] counts, Func<long[], T> element)
    {
        var idx = new long[counts.Length];
        var list = new List<T>();
        void Walk(int level)
        {
            if (level == counts.Length) { list.Add(element(idx)); return; }
            long n = counts[level](idx);
            for (long i = 1; i <= n; i++) { idx[level] = i; Walk(level + 1); }
        }
        Walk(0);
        if (list.Count == 0)
        {
            const string why = "a table(ALL) intrinsic argument ranged over no occurrences — the ALL subscript shall result in at least one argument (ISO §15.3)";
            ExceptionState.ArgumentError(why);                          // EC-ARGUMENT-FUNCTION when checking is on (fatal, USE-dispatchable)
            throw new CobolFatalException("EC-ARGUMENT-FUNCTION", why);  // the undefined case is defined LOUD, checking or not
        }
        return list.ToArray();
    }

    /// <summary>The intrinsic argument list assembled from written operands and <see cref="AllArgs{T}"/> enumerations,
    /// in source order — the ONE array a <c>params T[]</c> body receives.</summary>
    public static T[] ArgConcat<T>(params T[][] parts)
    {
        int n = 0;
        foreach (var p in parts) n += p.Length;
        var all = new T[n];
        int at = 0;
        foreach (var p in parts) { Array.Copy(p, 0, all, at, p.Length); at += p.Length; }
        return all;
    }

    /// <summary>Bind an evaluated argument list to a name inside an EXPRESSION (the C# analogue of <c>let</c>): the
    /// intrinsic renderers use it when a body needs the SAME enumerated list twice — MEAN's sum and its count, a
    /// leading positional argument and the tail — so a runtime table(ALL) enumeration is evaluated exactly once.</summary>
    public static R With<T, R>(T value, Func<T, R> body) => body(value);
}
