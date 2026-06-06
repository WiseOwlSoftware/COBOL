// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;

namespace CobolSharp.Runtime.Numeric;

/// <summary>
/// The value-level numeric store for the .NET-native data model: scale → round (eight ISO modes) →
/// bound-check against the receiver's capacity → SIZE ERROR, all on the exact <see cref="CobolDecimal"/>
/// / <see cref="BigInteger"/> substrate.
///
/// <para>This is the extracted-and-corrected successor to <c>PicRuntime</c>'s
/// <c>ApplyScalingAndRounding</c> + <c>WouldOverflow</c> + the PROHIBITED guard in
/// <c>StoreArithmeticResult</c> (see <c>docs/DATA_MODEL_ARCHITECTURE.md</c> §5/§13). It is purely
/// value-level — byte encode/decode stays in the byte codec — and never throws: a result that does not
/// fit the receiver is reported through the <see cref="TryStore"/> boolean (ON SIZE ERROR), never an
/// exception.</para>
///
/// <para>"Corrected" means it fixes the two precision defects the legacy decimal path is blind to: it
/// uses <see cref="BigInteger"/> throughout, so 19–31-digit values and intermediates neither overflow
/// nor decode to zero (the legacy <c>(long)</c> / <see cref="decimal"/> caps), and it never silently
/// throws an <see cref="OverflowException"/> before a SIZE ERROR can fire. For every value within
/// <see cref="decimal"/>'s range it is bit-for-bit identical to the legacy path (the Stage-1 differential
/// oracle); above that range it is validated against an independent high-precision reference, because the
/// decimal-based legacy path is common-mode blind there.</para>
/// </summary>
public static class CobolNum
{
    /// <summary>
    /// Rounds <paramref name="value"/> to the receiver's representable grid — its fraction scale (with
    /// leading-P) or its trailing-P multiple — using <paramref name="mode"/>. The exact analogue of the
    /// legacy <c>ApplyScalingAndRounding</c>. Performs no capacity check (see <see cref="TryStore"/>).
    /// </summary>
    public static CobolDecimal ScaleAndRound(CobolDecimal value, in NumProfile profile, CobolRounding mode)
    {
        int trailingP = profile.TrailingScaleDigits;
        if (trailingP > 0)
        {
            // Reduce to an integer count of 10^trailingP units, round, then re-scale up — i.e.
            // round(value / 10^trailingP) × 10^trailingP. Dividing by 10^trailingP is a scale increase.
            var shifted = new CobolDecimal(value.Unscaled, value.Scale + trailingP);
            CobolDecimal roundedUnits = shifted.RescaleTo(0, mode);
            return new CobolDecimal(roundedUnits.Unscaled * CobolDecimal.Pow10(trailingP), 0);
        }

        return value.RescaleTo(profile.FractionScale, mode);
    }

    /// <summary>
    /// Stores <paramref name="value"/> into the receiver described by <paramref name="profile"/>, rounding
    /// with <paramref name="mode"/>. Returns <c>true</c> and the stored value on success; returns
    /// <c>false</c> — the ON SIZE ERROR condition (ISO §14.9.4) — when the result will not fit the
    /// receiver's capacity, or when <paramref name="mode"/> is
    /// <see cref="CobolRounding.Prohibited"/> and the result is inexact at the receiver's scale. On
    /// <c>false</c> the receiver must be left unchanged (the COBOL rule); <paramref name="stored"/> is then
    /// the unmodified input and should be ignored. Never throws.
    /// </summary>
    public static bool TryStore(CobolDecimal value, in NumProfile profile, CobolRounding mode, out CobolDecimal stored)
    {
        // ROUNDED MODE PROHIBITED: an inexact result raises SIZE ERROR; the receiver is left unchanged.
        if (mode == CobolRounding.Prohibited && IsInexactForProfile(value, profile))
        {
            stored = value;
            return false;
        }

        CobolDecimal rounded = ScaleAndRound(value, profile, mode);
        if (ExceedsCapacity(StoredDigits(rounded, profile), profile))
        {
            stored = value;
            return false;
        }

        // Return the signed rounded value. The unsigned-magnitude rule (ISO §14.9.25 GR8 — a negative result
        // moved to an unsigned item stores its magnitude) is a property of the receiver's *representation*,
        // applied by the encoder: the byte codecs drop the sign for an unsigned field, while a numeric-edited
        // receiver renders the sign through its edit pattern and therefore needs the signed value. The capacity
        // check above already bounds the magnitude, so an unsigned receiver still correctly accepts a negative
        // value whose magnitude fits.
        stored = rounded;
        return true;
    }

    /// <summary>
    /// The integer formed by the receiver's stored decimal digits for <paramref name="rounded"/> (which is
    /// already at the receiver's scale): the value's significand for a V/leading-P field, or the trailing-P
    /// unit count for a trailing-P field. Its digit count / range is what the capacity check bounds.
    /// </summary>
    private static BigInteger StoredDigits(CobolDecimal rounded, in NumProfile profile)
    {
        if (profile.TrailingScaleDigits > 0)
            return rounded.Unscaled / CobolDecimal.Pow10(profile.TrailingScaleDigits); // exact: a multiple
        return rounded.Unscaled; // rounded.Scale == FractionScale, so Unscaled == value × 10^FractionScale
    }

    /// <summary>True when <paramref name="storedDigits"/> exceeds the receiver's capacity discipline.</summary>
    private static bool ExceedsCapacity(BigInteger storedDigits, in NumProfile profile)
    {
        BigInteger magnitude = BigInteger.Abs(storedDigits);
        return profile.Truncation switch
        {
            NumericTruncation.DigitCount => CobolDecimal.CountDigits(magnitude) > profile.Digits,
            NumericTruncation.PackedDecimal => CobolDecimal.CountDigits(magnitude) > (2 * profile.StorageLength - 1),
            NumericTruncation.BinaryCapacity => ExceedsBinaryCapacity(storedDigits, magnitude, profile),
            _ => true,
        };
    }

    /// <summary>
    /// True when the value leaves a binary field's capacity. A signed field bounds the two's-complement
    /// value; an unsigned field stores the magnitude, so it bounds the magnitude against the unsigned max
    /// (COMP-5 / BINARY-* — <c>PIC S9(4) COMP-5</c> = −32768..32767, <c>PIC 9(4) COMP-5</c> = 0..65535).
    ///
    /// <para>This is the full, correct native-binary range (ISO §8.5.1.2). It is a deliberate correction
    /// over the legacy byte codec at two symmetric points the legacy decimal/long path cannot represent, so
    /// both are validated against an independent reference rather than the legacy oracle: the signed
    /// negative extreme (e.g. −2^15 for a 2-byte field, which the legacy magnitude check mis-flags), and the
    /// 8-byte <em>unsigned</em> range (long.Max, ulong.Max] (which the legacy signed-long codec cannot hold).</para>
    /// </summary>
    private static bool ExceedsBinaryCapacity(BigInteger value, BigInteger magnitude, in NumProfile profile)
    {
        if (profile.Signed)
        {
            return profile.StorageLength switch
            {
                1 => value < SByteMin || value > SByteMax,
                2 => value < Int16Min || value > Int16Max,
                4 => value < Int32Min || value > Int32Max,
                8 => value < Int64Min || value > Int64Max,
                _ => true,
            };
        }
        return profile.StorageLength switch
        {
            1 => magnitude > ByteMax,
            2 => magnitude > UInt16Max,
            4 => magnitude > UInt32Max,
            8 => magnitude > UInt64Max,
            _ => true,
        };
    }

    /// <summary>
    /// True when <paramref name="value"/> cannot be represented exactly at the receiver's scale — the
    /// condition that makes <see cref="CobolRounding.Prohibited"/> raise SIZE ERROR. Mirrors the legacy
    /// <c>IsInexactAtScale</c> (trailing-P uses the 10^trailingP grid; otherwise the fraction scale).
    /// </summary>
    private static bool IsInexactForProfile(CobolDecimal value, in NumProfile profile)
    {
        if (profile.TrailingScaleDigits > 0)
        {
            // Inexact unless value is a whole multiple of 10^trailingP, i.e. value/10^trailingP is integral.
            var shifted = new CobolDecimal(value.Unscaled, value.Scale + profile.TrailingScaleDigits);
            return shifted.IsInexactAtScale(0);
        }
        return value.IsInexactAtScale(profile.FractionScale);
    }

    // ── two's-complement bounds (cached as BigInteger) ──
    private static readonly BigInteger SByteMin = sbyte.MinValue, SByteMax = sbyte.MaxValue, ByteMax = byte.MaxValue;
    private static readonly BigInteger Int16Min = short.MinValue, Int16Max = short.MaxValue, UInt16Max = ushort.MaxValue;
    private static readonly BigInteger Int32Min = int.MinValue, Int32Max = int.MaxValue, UInt32Max = uint.MaxValue;
    private static readonly BigInteger Int64Min = long.MinValue, Int64Max = long.MaxValue, UInt64Max = ulong.MaxValue;
}
