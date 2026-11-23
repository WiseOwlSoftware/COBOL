// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// Selection-by-VALUE intrinsic bodies over (unscaled, scale) argument pairs — MAX / MIN / ORD-MAX /
/// ORD-MIN without any argument alignment (fix-queue PB65).
/// </summary>
/// <remarks>
/// The aligned forms (<c>MaxScaled</c> and siblings) receive arguments pre-widened to the common scale, and
/// that widening wrapped silently past the Int128 intermediate: <c>FUNCTION MIN(BIGV SMLV)</c> over a
/// <c>PIC 9(24)</c> and a <c>PIC 9V9(15)</c> — 39 aligned digits — returned a NEGATIVE value, the content of
/// NO argument (§2023:15.63.4 RV1 is violated on its face). Selection needs no alignment at all: the §2023:8.8.4.2
/// comparison is <see cref="CobolNum.Compare"/>'s exact non-widening compare, the tie keeps the LEFTMOST
/// argument (§2023:15.59.4 RV2 / §2023:15.63.4 RV2, first-ordinal for §2023:15.71.4/§2023:15.72.4), and only the ONE selected
/// value rescales to the caller's scale — through <see cref="CobolNum.RescaleEscape"/>, so a result that
/// genuinely cannot be represented at that scale inside the intermediate is the size-error condition, never
/// a wrap. The aligned forms remain for all-fitting argument lists (the emitter's capacity choice); the
/// arithmetic-bearing statistics (SUM / RANGE / MEAN / MEDIAN / MIDRANGE) genuinely need aligned addition
/// and now ride the escape-checked alignment instead of the silent one.
/// </remarks>
public static partial class CobolIntrinsics
{
    /// <summary>§2023:15.59 MAX by value selection — the leftmost greatest argument, rescaled to
    /// <paramref name="toScale"/>. A RECEIVER-BOUND result (<paramref name="store"/>) rescales with the
    /// store-cap (§2023:14.9.25.4 GR6 high-order truncation semantics — the receiver's own capacity mod finishes
    /// the job); a receiverless result stays LOUD on the escape check, because a comparison consuming a
    /// capped value would silently compare the wrong number.</summary>
    public static Int128 MaxAt(int toScale, bool store, ReadOnlySpan<Int128> v, ReadOnlySpan<int> s)
    {
        int i = ExtremeIndex(new ScaledOrder(v, s), true, "MAX");
        return store ? CobolNum.RescaleStoreCap(v[i], s[i], toScale, CobolRounding.Truncation)
                     : CobolNum.RescaleEscape(v[i], s[i], toScale, CobolRounding.Truncation);
    }

    /// <summary>§2023:15.63 MIN by value selection — the leftmost least argument (rescale semantics as
    /// <see cref="MaxAt"/>).</summary>
    public static Int128 MinAt(int toScale, bool store, ReadOnlySpan<Int128> v, ReadOnlySpan<int> s)
    {
        int i = ExtremeIndex(new ScaledOrder(v, s), false, "MIN");
        return store ? CobolNum.RescaleStoreCap(v[i], s[i], toScale, CobolRounding.Truncation)
                     : CobolNum.RescaleEscape(v[i], s[i], toScale, CobolRounding.Truncation);
    }

    /// <summary>§2023:15.71 ORD-MAX — the 1-based ordinal of the first greatest argument. Pure selection: no
    /// rescale ever, so every legal argument list has its defined answer.</summary>
    public static long OrdMaxAt(ReadOnlySpan<Int128> v, ReadOnlySpan<int> s) => ExtremeIndex(new ScaledOrder(v, s), true, "ORD-MAX") + 1;

    /// <summary>§2023:15.72 ORD-MIN — the 1-based ordinal of the first least argument.</summary>
    public static long OrdMinAt(ReadOnlySpan<Int128> v, ReadOnlySpan<int> s) => ExtremeIndex(new ScaledOrder(v, s), false, "ORD-MIN") + 1;
}
