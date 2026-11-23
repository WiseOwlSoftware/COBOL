// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;

namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE ONE SELECTION RULE OF MAX / MIN / ORD-MAX / ORD-MIN (kb/Work PB2635): "the content of the argument-1 having
/// the greatest value", the leftmost on a tie (§2023:15.59.4 RV1/RV2, §2023:15.63.4), and ORD-MAX / ORD-MIN the ordinal of
/// that same argument (§2023:15.71.4, §2023:15.72.4). <see cref="ExtremeIndex{TOrder}"/> owns the empty-list assertion,
/// the strict-update leftmost rule and the first-ordinal rule; each carrier supplies only its §2023:8.8.4.2 comparison as
/// an <see cref="ISelectionOrder"/> — the aligned <see cref="Int128"/> and the binary64 by their own operators
/// (<see cref="ValueOrder{T}"/>), the (unscaled, scale) pair through <see cref="CobolNum.Compare"/>
/// (<see cref="ScaledOrder"/>), the SDIDI through <see cref="CobolDec.Compare"/> (<see cref="DecOrder"/>), and an
/// alphanumeric or national argument through the program collating sequence (<see cref="StringOrder"/>), the exact argument values through <see cref="CobolExact.Compare"/> (<see cref="ExactOrder"/>), and the aligned
/// 256-bit values through <see cref="CobolWide.Compare"/> (<see cref="WideOrder"/>). Before it the
/// loop was written once per carrier, and the binary64 MAX / MIN had drifted onto <c>Enumerable.Max</c> / <c>Min</c>,
/// whose NaN rule differs from the loop's: a NaN argument made MAX and ORD-MAX name different arguments.
/// </summary>
public static partial class CobolIntrinsics
{
    /// <summary>A carrier's §2023:8.8.4.2 comparison of two of a selection's arguments, by position.</summary>
    internal interface ISelectionOrder
    {
        /// <summary>The number of arguments.</summary>
        int Count { get; }

        /// <summary>Negative, zero or positive as argument <paramref name="i"/> is less than, equal to or greater than
        /// argument <paramref name="j"/> (both 0-based).</summary>
        int Compare(int i, int j);
    }

    /// <summary>The 0-based position of the greatest (<paramref name="max"/>) or least argument under
    /// <paramref name="order"/>, the LEFTMOST on a tie (a later argument replaces the selection only when it is strictly
    /// greater, or strictly less). An empty list is the §2023:15.3 assertion <see cref="RequireArguments"/> raises.</summary>
    internal static int ExtremeIndex<TOrder>(in TOrder order, bool max, string function) where TOrder : struct, ISelectionOrder, allows ref struct
    {
        RequireArguments(order.Count, function);
        int best = 0;
        for (int i = 1; i < order.Count; i++)
        {
            int c = order.Compare(i, best);
            if (max ? c > 0 : c < 0) best = i;
        }
        return best;
    }

    /// <summary>Arguments compared by their own ordering operators — the aligned <see cref="Int128"/> lane and binary64.
    /// An unordered pair (a binary64 NaN against anything) compares equal, so a NaN is selected only when it stands first
    /// and is never moved onto; MAX and ORD-MAX therefore name the same argument whatever the list holds.</summary>
    internal readonly ref struct ValueOrder<T>(ReadOnlySpan<T> xs) : ISelectionOrder where T : IComparisonOperators<T, T, bool>
    {
        private readonly ReadOnlySpan<T> _xs = xs;
        public int Count => _xs.Length;
        public int Compare(int i, int j) => _xs[i] > _xs[j] ? 1 : _xs[i] < _xs[j] ? -1 : 0;
    }

    /// <summary>Unaligned (unscaled, scale) pairs, compared by value without widening (<see cref="CobolNum.Compare"/>,
    /// fix-queue PB65).</summary>
    internal readonly ref struct ScaledOrder(ReadOnlySpan<Int128> values, ReadOnlySpan<int> scales) : ISelectionOrder
    {
        private readonly ReadOnlySpan<Int128> _values = values;
        private readonly ReadOnlySpan<int> _scales = scales;
        public int Count => _values.Length;
        public int Compare(int i, int j) => CobolNum.Compare(_values[i], _scales[i], _values[j], _scales[j]);
    }

    /// <summary>Standard-decimal intermediates (<see cref="CobolDec.Compare"/>).</summary>
    internal readonly ref struct DecOrder(ReadOnlySpan<CobolDec> xs) : ISelectionOrder
    {
        private readonly ReadOnlySpan<CobolDec> _xs = xs;
        public int Count => _xs.Length;
        public int Compare(int i, int j) => CobolDec.Compare(_xs[i], _xs[j]);
    }

    /// <summary>Alphanumeric or national arguments under the program collating sequence: the native one
    /// (<see cref="CobolString.Compare"/>, §2023:8.8.4.2.7 GR2's space padding) when <paramref name="collation"/> is null,
    /// else the alphanumeric or national PCS the emitter passes (CA23).</summary>
    internal readonly ref struct StringOrder(ReadOnlySpan<string> xs, CobolCollation? collation) : ISelectionOrder
    {
        private readonly ReadOnlySpan<string> _xs = xs;
        private readonly CobolCollation? _collation = collation;
        public int Count => _xs.Length;
        public int Compare(int i, int j) => _collation is null ? CobolString.Compare(_xs[i], _xs[j]) : _collation.Compare(_xs[i], _xs[j]);
    }

    /// <summary>Exact argument values on the native SDIDI lane, compared by algebraic value (<see cref="CobolExact.Compare"/>,
    /// §2023:8.8.4.2.4).</summary>
    internal readonly ref struct ExactOrder(ReadOnlySpan<CobolExact> xs) : ISelectionOrder
    {
        private readonly ReadOnlySpan<CobolExact> _xs = xs;
        public int Count => _xs.Length;
        public int Compare(int i, int j) => CobolExact.Compare(_xs[i], _xs[j]);
    }

    /// <summary>Aligned 256-bit values (the emitter brought every argument to one scale), compared by value
    /// (<see cref="CompareAligned"/>).</summary>
    internal readonly ref struct WideOrder(ReadOnlySpan<CobolWide> xs) : ISelectionOrder
    {
        private readonly ReadOnlySpan<CobolWide> _xs = xs;
        public int Count => _xs.Length;
        public int Compare(int i, int j) => CompareAligned(_xs[i], _xs[j]);
    }
}
