// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolNet.Runtime;

/// <summary>
/// Selection-by-VALUE intrinsic bodies over (unscaled, scale) argument pairs — MAX / MIN / ORD-MAX /
/// ORD-MIN without any argument alignment (fix-queue PB65).
/// </summary>
/// <remarks>
/// The aligned forms (<c>MaxScaled</c> and siblings) receive arguments pre-widened to the common scale, and
/// that widening wrapped silently past the Int128 intermediate: <c>FUNCTION MIN(BIGV SMLV)</c> over a
/// <c>PIC 9(24)</c> and a <c>PIC 9V9(15)</c> — 39 aligned digits — returned a NEGATIVE value, the content of
/// NO argument (§15.63.4 r1 is violated on its face). Selection needs no alignment at all: the §8.8.4.2
/// comparison is <see cref="CobolNum.Compare"/>'s exact non-widening compare, the tie keeps the LEFTMOST
/// argument (§15.59.4 r2 / §15.63.4 r2, first-ordinal for §15.71.4/§15.72.4), and only the ONE selected
/// value rescales to the caller's scale — through <see cref="CobolNum.RescaleEscape"/>, so a result that
/// genuinely cannot be represented at that scale inside the intermediate is the size-error condition, never
/// a wrap. The aligned forms remain for all-fitting argument lists (the emitter's capacity choice); the
/// arithmetic-bearing statistics (SUM / RANGE / MEAN / MEDIAN / MIDRANGE) genuinely need aligned addition
/// and now ride the escape-checked alignment instead of the silent one.
/// </remarks>
public static partial class CobolIntrinsics
{
    private static int SelectMax(Int128[] v, int[] s)
    {
        int best = 0;
        for (int i = 1; i < v.Length; i++)
            if (CobolNum.Compare(v[i], s[i], v[best], s[best]) > 0) best = i;
        return best;
    }

    private static int SelectMin(Int128[] v, int[] s)
    {
        int best = 0;
        for (int i = 1; i < v.Length; i++)
            if (CobolNum.Compare(v[i], s[i], v[best], s[best]) < 0) best = i;
        return best;
    }

    /// <summary>§15.59 MAX by value selection — the leftmost greatest argument, rescaled to
    /// <paramref name="toScale"/>. A RECEIVER-BOUND result (<paramref name="store"/>) rescales with the
    /// store-cap (§14.9.25.4 GR6 high-order truncation semantics — the receiver's own capacity mod finishes
    /// the job); a receiverless result stays LOUD on the escape check, because a comparison consuming a
    /// capped value would silently compare the wrong number.</summary>
    public static Int128 MaxAt(int toScale, bool store, Int128[] v, int[] s)
    {
        int i = SelectMax(v, s);
        return store ? CobolNum.RescaleStoreCap(v[i], s[i], toScale, CobolRounding.Truncation)
                     : CobolNum.RescaleEscape(v[i], s[i], toScale, CobolRounding.Truncation);
    }

    /// <summary>§15.63 MIN by value selection — the leftmost least argument (rescale semantics as
    /// <see cref="MaxAt"/>).</summary>
    public static Int128 MinAt(int toScale, bool store, Int128[] v, int[] s)
    {
        int i = SelectMin(v, s);
        return store ? CobolNum.RescaleStoreCap(v[i], s[i], toScale, CobolRounding.Truncation)
                     : CobolNum.RescaleEscape(v[i], s[i], toScale, CobolRounding.Truncation);
    }

    /// <summary>§15.71 ORD-MAX — the 1-based ordinal of the first greatest argument. Pure selection: no
    /// rescale ever, so every legal argument list has its defined answer.</summary>
    public static long OrdMaxAt(Int128[] v, int[] s) => SelectMax(v, s) + 1;

    /// <summary>§15.72 ORD-MIN — the 1-based ordinal of the first least argument.</summary>
    public static long OrdMinAt(Int128[] v, int[] s) => SelectMin(v, s) + 1;
}
