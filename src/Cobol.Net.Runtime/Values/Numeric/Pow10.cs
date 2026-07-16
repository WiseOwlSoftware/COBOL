// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The ONE power-of-ten source for the runtime (DESIGN-runtime-library §2.3): three type-shaped views over
/// tables computed once at type initialization. Replaces the six identical multiply-loop copies that recomputed
/// these compile-time constants on every numeric store/rescale/format (<c>CobolNum.Pow10/Pow10Wide</c>,
/// <c>CobolDec.Pow10</c>, <c>CobolDate.Pow10</c>, <c>CobolIntrinsics.Pow10D/Pow10I</c>, <c>CobolFloat.Pow10</c>).
/// The tables are built by the same cumulative ×10 recurrence the deleted loops used, so every value in range is
/// bit-identical to what the loops produced; an out-of-table exponent falls back to that same loop (and a
/// negative exponent returns 1 — zero loop iterations — exactly as before).
/// </summary>
internal static class Pow10
{
    private static readonly long[] L = BuildLong();      // 10^0 .. 10^18 — the long-safe range
    private static readonly Int128[] W = BuildWide();    // 10^0 .. 10^38 — the wide-intermediate range (SSOT §18 #4)
    private static readonly double[] D = BuildDouble();  // 10^0 .. 10^38 as double

    /// <summary>10^<paramref name="n"/> as a <see cref="long"/> (n in 0..18 — within long's range).</summary>
    public static long AsLong(int n) => L[n];

    /// <summary>10^<paramref name="n"/> as an <see cref="Int128"/> (n in 0..38 — the wide intermediate range,
    /// COBOLNET_DESIGN §18 #4; an out-of-range n falls back to the loop the table replaced).</summary>
    public static Int128 AsWide(int n)
    {
        if ((uint)n < (uint)W.Length) return W[n];
        Int128 r = 1;
        for (int i = 0; i < n; i++) r *= 10;
        return r;
    }

    /// <summary>10^<paramref name="n"/> as a <see cref="double"/> (n ≥ 0; an out-of-range n falls back to the
    /// loop the table replaced).</summary>
    public static double AsDouble(int n)
    {
        if ((uint)n < (uint)D.Length) return D[n];
        double r = 1;
        for (int i = 0; i < n; i++) r *= 10;
        return r;
    }

    private static long[] BuildLong()
    {
        var t = new long[19];
        long r = 1;
        for (int i = 0; i < t.Length; i++) { t[i] = r; if (i < t.Length - 1) r *= 10; }
        return t;
    }

    private static Int128[] BuildWide()
    {
        var t = new Int128[39];
        Int128 r = 1;
        for (int i = 0; i < t.Length; i++) { t[i] = r; if (i < t.Length - 1) r *= 10; }
        return t;
    }

    private static double[] BuildDouble()
    {
        var t = new double[39];
        double r = 1;
        for (int i = 0; i < t.Length; i++) { t[i] = r; if (i < t.Length - 1) r *= 10; }
        return t;
    }
}
