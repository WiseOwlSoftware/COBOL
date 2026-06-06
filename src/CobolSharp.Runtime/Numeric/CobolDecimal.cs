// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Numerics;

namespace CobolSharp.Runtime.Numeric;

/// <summary>
/// An exact base-10 fixed-point number: <c>value = <see cref="Unscaled"/> × 10^(−<see cref="Scale"/>)</c>
/// with <see cref="Scale"/> ≥ 0, backed by <see cref="BigInteger"/>.
///
/// <para>This is the canonical numeric value carrier for the .NET-native data model
/// (<c>docs/DATA_MODEL_ARCHITECTURE.md</c>). It exists because COBOL mandates 1–31 significant digit
/// positions (ISO §13) plus intermediate results that can exceed that, while .NET <see cref="decimal"/>
/// holds only 28–29 digits and silently loses precision (or returns zero) above that. The owner-gated
/// decision (ADR §12 Open Question #1, settled DEVLOG 393) is that the numeric substrate for the
/// 19–31-digit range and all arithmetic intermediates is <see cref="BigInteger"/>, not
/// <see cref="decimal"/> — which is exactly what this type provides.</para>
///
/// <para>Value semantics are by mathematical value, normalized across scale: <c>1.0</c> (Unscaled 10,
/// Scale 1) equals <c>1</c> (Unscaled 1, Scale 0). It is deliberately a plain <see cref="readonly"/>
/// struct (not a <c>record struct</c>) so that <see cref="Equals(CobolDecimal)"/> compares by value
/// rather than field-wise on the (Unscaled, Scale) pair.</para>
/// </summary>
public readonly struct CobolDecimal : IEquatable<CobolDecimal>, IComparable<CobolDecimal>
{
    /// <summary>The scaled integer significand. The represented value is <c>Unscaled × 10^(−Scale)</c>.</summary>
    public BigInteger Unscaled { get; }

    /// <summary>The number of fractional digit positions (always ≥ 0).</summary>
    public int Scale { get; }

    /// <summary>Constructs a fixed-point value <c>unscaled × 10^(−scale)</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">if <paramref name="scale"/> is negative.</exception>
    public CobolDecimal(BigInteger unscaled, int scale)
    {
        if (scale < 0)
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "CobolDecimal scale must be ≥ 0.");
        Unscaled = unscaled;
        Scale = scale;
    }

    /// <summary>The value zero (Unscaled 0, Scale 0).</summary>
    public static CobolDecimal Zero => new(BigInteger.Zero, 0);

    /// <summary>True when the represented value is exactly zero.</summary>
    public bool IsZero => Unscaled.IsZero;

    /// <summary>The sign of the value: −1, 0, or +1.</summary>
    public int Sign => Unscaled.Sign;

    /// <summary>Constructs a value from a 64-bit integer (scale 0).</summary>
    public static CobolDecimal FromInt64(long value) => new(value, 0);

    /// <summary>Constructs a value from a <see cref="BigInteger"/> at the given (default 0) scale.</summary>
    public static CobolDecimal FromBigInteger(BigInteger value, int scale = 0) => new(value, scale);

    /// <summary>
    /// Constructs a value from a .NET <see cref="decimal"/>, preserving its exact value and scale.
    /// Lossless: a <see cref="decimal"/> is itself a base-10 fixed-point number.
    /// </summary>
    public static CobolDecimal FromDecimal(decimal value)
    {
        // decimal.GetBits → [lo, mid, hi, flags]; bits 16-23 of flags = scale (0-28), bit 31 = sign.
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);
        int scale = (bits[3] >> 16) & 0x7F;
        bool negative = (bits[3] & unchecked((int)0x80000000)) != 0;

        // Assemble the 96-bit magnitude (lo|mid|hi) as an unsigned BigInteger.
        BigInteger magnitude =
            (new BigInteger((uint)bits[2]) << 64) |
            (new BigInteger((uint)bits[1]) << 32) |
             new BigInteger((uint)bits[0]);

        return new CobolDecimal(negative ? -magnitude : magnitude, scale);
    }

    /// <summary>
    /// Converts to a .NET <see cref="decimal"/>. Exact when the value fits decimal's 28–29 digit range.
    /// </summary>
    /// <exception cref="OverflowException">when the value exceeds the range/precision of <see cref="decimal"/>.</exception>
    public decimal ToDecimal()
    {
        if (!TryToDecimal(out decimal result))
            throw new OverflowException(
                $"CobolDecimal {ToString()} exceeds the range/precision of System.Decimal.");
        return result;
    }

    /// <summary>
    /// Attempts a lossless conversion to <see cref="decimal"/>. Returns false (without throwing) when the
    /// value does not fit decimal's 28–29 digit range — the case the <see cref="BigInteger"/> substrate
    /// exists to handle.
    /// </summary>
    public bool TryToDecimal(out decimal result)
    {
        // decimal supports scales 0..28. A scale beyond 28 cannot be represented even if the magnitude is small.
        if (Scale <= 28)
        {
            BigInteger magnitude = BigInteger.Abs(Unscaled);
            if (magnitude <= MaxDecimalMagnitude)
            {
                int lo = (int)(uint)(magnitude & 0xFFFFFFFF);
                int mid = (int)(uint)((magnitude >> 32) & 0xFFFFFFFF);
                int hi = (int)(uint)((magnitude >> 64) & 0xFFFFFFFF);
                result = new decimal(lo, mid, hi, Unscaled.Sign < 0, (byte)Scale);
                return true;
            }
        }
        result = 0m;
        return false;
    }

    /// <summary>The largest unsigned 96-bit magnitude a <see cref="decimal"/> can hold (2^96 − 1).</summary>
    private static readonly BigInteger MaxDecimalMagnitude = (BigInteger.One << 96) - 1;

    /// <summary>Absolute value.</summary>
    public CobolDecimal Abs() => new(BigInteger.Abs(Unscaled), Scale);

    /// <summary>Arithmetic negation.</summary>
    public CobolDecimal Negate() => new(-Unscaled, Scale);

    /// <summary>The number of significant decimal digits in the unscaled significand (0 → 1 digit).</summary>
    public int DigitCount => CountDigits(Unscaled);

    /// <summary>
    /// Returns this value with its scale changed to <paramref name="targetScale"/>. Increasing the scale is
    /// exact (pads with zeros); decreasing it drops the excess low-order fractional digits using the given
    /// rounding mode. <see cref="CobolRounding.Prohibited"/> rounds toward zero here (like
    /// <see cref="CobolRounding.Truncation"/>); the inexact-result SIZE ERROR it implies is the caller's
    /// responsibility (see <c>CobolNum.TryStore</c>), mirroring the legacy split between
    /// <c>RoundToIntegerByMode</c> and <c>StoreArithmeticResult</c>.
    /// </summary>
    public CobolDecimal RescaleTo(int targetScale, CobolRounding mode)
    {
        if (targetScale < 0)
            throw new ArgumentOutOfRangeException(nameof(targetScale), targetScale, "Target scale must be ≥ 0.");

        if (targetScale == Scale)
            return this;

        if (targetScale > Scale)
            return new CobolDecimal(Unscaled * Pow10(targetScale - Scale), targetScale);

        // targetScale < Scale: drop (Scale - targetScale) fractional digits with rounding.
        int drop = Scale - targetScale;
        BigInteger divisor = Pow10(drop);
        BigInteger rounded = RoundedQuotient(Unscaled, divisor, mode);
        return new CobolDecimal(rounded, targetScale);
    }

    /// <summary>
    /// True when this value cannot be represented at <paramref name="targetScale"/> without dropping a
    /// nonzero fractional digit (i.e., rounding to that scale would change it). Used to detect the
    /// EC-SIZE-TRUNCATION condition under ROUNDED MODE PROHIBITED (ISO §14.9.4).
    /// </summary>
    public bool IsInexactAtScale(int targetScale)
    {
        if (targetScale >= Scale)
            return false;
        BigInteger divisor = Pow10(Scale - targetScale);
        return !(Unscaled % divisor).IsZero;
    }

    // ── arithmetic (exact; never overflows — the whole point of the BigInteger substrate) ──

    /// <summary>Exact addition. The result scale is the larger of the two operand scales.</summary>
    public static CobolDecimal operator +(CobolDecimal a, CobolDecimal b)
    {
        (BigInteger ua, BigInteger ub, int scale) = AlignScales(a, b);
        return new CobolDecimal(ua + ub, scale);
    }

    /// <summary>Exact subtraction. The result scale is the larger of the two operand scales.</summary>
    public static CobolDecimal operator -(CobolDecimal a, CobolDecimal b)
    {
        (BigInteger ua, BigInteger ub, int scale) = AlignScales(a, b);
        return new CobolDecimal(ua - ub, scale);
    }

    /// <summary>Exact multiplication. The result scale is the sum of the two operand scales.</summary>
    public static CobolDecimal operator *(CobolDecimal a, CobolDecimal b)
        => new(a.Unscaled * b.Unscaled, a.Scale + b.Scale);

    /// <summary>Unary negation.</summary>
    public static CobolDecimal operator -(CobolDecimal a) => a.Negate();

    // ── value equality / comparison (normalized across scale) ──

    /// <inheritdoc/>
    public bool Equals(CobolDecimal other)
    {
        (BigInteger ua, BigInteger ub, _) = AlignScales(this, other);
        return ua == ub;
    }

    /// <inheritdoc/>
    public int CompareTo(CobolDecimal other)
    {
        (BigInteger ua, BigInteger ub, _) = AlignScales(this, other);
        return ua.CompareTo(ub);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CobolDecimal other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // Hash on the scale-normalized (trailing-zero-stripped) form so equal values hash equally.
        BigInteger u = Unscaled;
        int s = Scale;
        while (s > 0 && !u.IsZero && (u % 10).IsZero) { u /= 10; s--; }
        return HashCode.Combine(u, u.IsZero ? 0 : s);
    }

    public static bool operator ==(CobolDecimal a, CobolDecimal b) => a.Equals(b);
    public static bool operator !=(CobolDecimal a, CobolDecimal b) => !a.Equals(b);
    public static bool operator <(CobolDecimal a, CobolDecimal b) => a.CompareTo(b) < 0;
    public static bool operator >(CobolDecimal a, CobolDecimal b) => a.CompareTo(b) > 0;
    public static bool operator <=(CobolDecimal a, CobolDecimal b) => a.CompareTo(b) <= 0;
    public static bool operator >=(CobolDecimal a, CobolDecimal b) => a.CompareTo(b) >= 0;

    /// <summary>Renders the value in plain decimal notation (e.g. <c>-12.340</c>), preserving trailing zeros.</summary>
    public override string ToString()
    {
        if (Scale == 0)
            return Unscaled.ToString(CultureInfo.InvariantCulture);

        bool negative = Unscaled.Sign < 0;
        string digits = BigInteger.Abs(Unscaled).ToString(CultureInfo.InvariantCulture);
        if (digits.Length <= Scale)
            digits = digits.PadLeft(Scale + 1, '0');
        string integerPart = digits[..^Scale];
        string fractionPart = digits[^Scale..];
        return (negative ? "-" : "") + integerPart + "." + fractionPart;
    }

    // ── helpers ──

    /// <summary>Brings two values to a common scale (the larger), returning their aligned significands.</summary>
    private static (BigInteger a, BigInteger b, int scale) AlignScales(CobolDecimal x, CobolDecimal y)
    {
        if (x.Scale == y.Scale)
            return (x.Unscaled, y.Unscaled, x.Scale);
        if (x.Scale < y.Scale)
            return (x.Unscaled * Pow10(y.Scale - x.Scale), y.Unscaled, y.Scale);
        return (x.Unscaled, y.Unscaled * Pow10(x.Scale - y.Scale), x.Scale);
    }

    /// <summary>
    /// Divides <paramref name="dividend"/> by the positive power-of-ten <paramref name="divisor"/>, rounding
    /// the dropped fraction to an integer per <paramref name="mode"/>. This is the single point that
    /// implements all eight ISO rounding methods; it operates on the exact rational
    /// <c>dividend / divisor</c>, so it agrees bit-for-bit with the legacy decimal-based
    /// <c>RoundToIntegerByMode</c> for every value within decimal's range.
    /// </summary>
    private static BigInteger RoundedQuotient(BigInteger dividend, BigInteger divisor, CobolRounding mode)
    {
        int sign = dividend.Sign;
        if (sign == 0)
            return BigInteger.Zero;

        BigInteger absDividend = BigInteger.Abs(dividend);
        BigInteger q = BigInteger.DivRem(absDividend, divisor, out BigInteger r); // q ≥ 0, 0 ≤ r < divisor
        if (r.IsZero)
            return sign < 0 ? -q : q;

        // twoR vs divisor decides the half for the nearest-* modes.
        BigInteger twoR = r * 2;
        bool roundAbsUp = mode switch
        {
            CobolRounding.Truncation        => false,
            CobolRounding.Prohibited        => false, // toward zero here; SIZE ERROR handled by the caller
            CobolRounding.AwayFromZero      => true,  // any nonzero fraction → magnitude up
            CobolRounding.NearestAwayFromZero => twoR >= divisor,            // tie → up
            CobolRounding.NearestEven       => twoR > divisor || (twoR == divisor && !(q % 2).IsZero),
            CobolRounding.NearestTowardZero => twoR > divisor,              // tie → toward zero (down)
            CobolRounding.TowardGreater     => sign > 0,                    // ceiling: positive → up, negative → down
            CobolRounding.TowardLesser      => sign < 0,                    // floor: negative → up (in magnitude)
            _ => false,
        };

        BigInteger absResult = roundAbsUp ? q + 1 : q;
        return sign < 0 ? -absResult : absResult;
    }

    /// <summary>10 raised to a non-negative integer power, as a <see cref="BigInteger"/>.</summary>
    internal static BigInteger Pow10(int exponent)
    {
        if (exponent < 0)
            throw new ArgumentOutOfRangeException(nameof(exponent), exponent, "Power-of-ten exponent must be ≥ 0.");
        return BigInteger.Pow(10, exponent);
    }

    /// <summary>Counts decimal digits in a <see cref="BigInteger"/> magnitude (0 counts as 1 digit).</summary>
    internal static int CountDigits(BigInteger value)
    {
        BigInteger magnitude = BigInteger.Abs(value);
        if (magnitude.IsZero)
            return 1;
        int count = 0;
        while (!magnitude.IsZero)
        {
            magnitude /= 10;
            count++;
        }
        return count;
    }
}
