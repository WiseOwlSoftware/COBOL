// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;

namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE THIRD FORM OF AN EXACT NATIVE VALUE: AN ARBITRARY-PRECISION INTEGER AT A SCALE, USED ONLY WHERE THE 256-BIT FORM FAILS
/// (kb/Work PB4592; owner decision of 2026-11-04, "Exact, lift the ban"; docs/DESIGN.md §1.2 invariant 2; docs/CONFORMANCE.md DOC-A.1-123).
/// An exact native intermediate is a scaled <see cref="Int128"/>, an expression of 39 to 77 digits on <see cref="CobolWide"/>, or an
/// SDIDI significand at its exponent (<see cref="CobolExact"/>); the value a step of the work leaves those forms is THIS: its exact
/// integer and the scale it is written at, nothing rounded. GnuCOBOL 3.2's <c>cob_decimal</c> sum, difference, product, truncated
/// quotient and integer power are exact over an arbitrary-precision integer at any size (CLAUDE.md rule 1 sends the latitude of
/// ISO §2023:8.8.1.3 and §2023:11.9.11.2 1) to it), and so is this form: <c>(P*1.0E0) ** 2 / P - P</c> of a 39-digit P is 0, where the 34-digit
/// image of the 78-digit P² left 99001.
/// <para>THE PUBLISHED BOUND (§2023:14.7.5 5): "if native arithmetic is in effect and the implementor defines that the range of values allowed
/// for the intermediate data item is to be checked" — the range is the SDIDI's: <c>|value| &lt; 10^6145</c> (<see cref="MaxIntegerDigits"/>;
/// decimal128's largest magnitude is 9.999…E+6144), plus a working-size cap of <see cref="MaxBits"/> bits on the integer, which holds the
/// cost of <c>X ** N</c> finite. A value past either raises EC-SIZE-OVERFLOW at the operation that formed it; a <see cref="Working"/> value, a
/// number an intrinsic's body forms that is no value of the program, is held to <see cref="MaxBits"/> alone.</para>
/// <para>NOTHING CHANGES ON THE FAST PATH: <see cref="CobolExact"/> reaches this form only where the <see cref="CobolWide"/> operation
/// it tries first fails, and normalizes a result that fits the 256-bit form back onto it, so a lane within 77 digits keeps its code.
/// This file is the ONE file of the runtime's value model that holds an arbitrary-precision integer for the exact path
/// (<c>scripts/semgrep/invariants.yml</c>, rule <c>wiseowl-cobol-no-biginteger</c>); every consumer reads it through <see cref="CobolExact"/>.</para>
/// </summary>
internal readonly struct CobolBig
{
    /// <summary>The largest integer-digit count a value may have: <c>|value| &lt; 10^6145</c>, the decimal128 range (§2023:8.8.1.5.2 NOTE 2).</summary>
    internal const int MaxIntegerDigits = 6145;

    /// <summary>The largest integer a value may hold, in bits (about 1.26 million digits): the working-size cap on a power or a product.</summary>
    internal const long MaxBits = 1L << 22;

    private static readonly BigInteger LowMask = (BigInteger.One << 128) - 1;

    /// <summary>10^38: the low-order digits a store can use of a value past the carrier.</summary>
    private static readonly UInt128 TenTo38 = (UInt128)BigInteger.Pow(10, 38);

    /// <summary>The exact integer; the value is <c>Value × 10^-Scale</c>.</summary>
    internal BigInteger Value { get; }

    /// <summary>The decimal scale the integer is written at (negative for an integer with trailing zeros).</summary>
    internal int Scale { get; }

    /// <summary>⛔ A WORKING VALUE (kb/Work PB4797): a number a body forms on the way to its result — the numerator <c>n·Σdᵢ² − (Σdᵢ)²</c> of
    /// VARIANCE, the discount's power <c>(1 + rate)^n</c> of PRESENT-VALUE — which is n² times the variance, or the value times the
    /// power, and so may pass the <see cref="MaxIntegerDigits"/> of a program's value while the result they form is in range. It is
    /// bounded by the working size <see cref="MaxBits"/> alone, and so is every sum, difference, product and power it takes part in;
    /// the RESULT is range-checked where a consumer lands it (<see cref="CobolDec"/>'s carrier), as every value of the program is.</summary>
    internal bool Working { get; }

    private CobolBig(BigInteger value, int scale, bool working = false) => (Value, Scale, Working) = (value, scale, working);

    /// <summary>This value as a working value (<see cref="Working"/>).</summary>
    internal CobolBig AsWorking() => Working ? this : new(Value, Scale, working: true);

    internal int Sign => Value.Sign;

    /// <summary>The decimal digits of the unscaled magnitude.</summary>
    internal string MagnitudeDigits() => BigInteger.Abs(Value).ToString(System.Globalization.CultureInfo.InvariantCulture);

    // ── construction ────────────────────────────────────────────────────────────────────────────────────────

    private static CobolSizeError Overflow() => new("a native intermediate exceeds the exact range "
        + "(|value| >= 10^6145 or an integer past 2^22 bits; ISO §2023:14.7.5 case 5 — the implementor-defined intermediate range is checked, "
        + "A.1 item 179: EC-SIZE-OVERFLOW)", "EC-SIZE-OVERFLOW");

    /// <summary>A power of a base below one in magnitude whose integer would pass the working cap: far below the decimal128 range's
    /// smallest magnitude, 10^-6176 (ISO §2023:8.8.1.5.2 r2: a nonzero value too small to be contained is EC-SIZE-UNDERFLOW).</summary>
    private static CobolSizeError Underflow() => new("a native power is too small to be contained "
        + "(|value| below 10^-6176; ISO §2023:8.8.1.5.2 r2, §2023:14.7.5 case 5: EC-SIZE-UNDERFLOW)", "EC-SIZE-UNDERFLOW");

    /// <summary>The value <c>value × 10^-scale</c>, range-checked: EC-SIZE-OVERFLOW past <see cref="MaxIntegerDigits"/> integer digits (a
    /// <paramref name="working"/> value is exempt) or <see cref="MaxBits"/> bits.</summary>
    private static CobolBig Of(BigInteger value, long scale, bool working = false)
    {
        if (scale is < int.MinValue or > int.MaxValue) throw Overflow();
        if (!value.IsZero)
        {
            if (value.GetBitLength() > MaxBits) throw Overflow();
            if (!working && Digits(BigInteger.Abs(value)) - scale > MaxIntegerDigits) throw Overflow();
        }
        return new(value, (int)scale, working);
    }

    /// <summary>A wide value at its scale, exactly.</summary>
    internal static CobolBig From(CobolWide wide, int scale)
    {
        var (negative, hi, lo) = wide.Parts;
        BigInteger magnitude = ((BigInteger)hi << 128) | lo;
        return new(negative ? -magnitude : magnitude, scale);
    }

    /// <summary>An SDIDI value exactly, at its own exponent.</summary>
    internal static CobolBig FromDec(CobolDec value) => new(value.Sig, -value.Exp);

    /// <summary>⛔ A BINARY64'S EXACT VALUE (kb/Work PB4730): <c>m × 2^e</c> with <c>m</c> odd, as <c>m × 5^k</c> at scale <c>k</c> for a
    /// negative <c>e = -k</c> (0.1 is 3602879701896397 × 5^55 at scale 55, every digit of its 55-place expansion), else the integer
    /// <c>m × 2^e</c>. Finite only: the caller screens a NaN or an infinity.</summary>
    internal static CobolBig FromBinary64(double d)
    {
        var (negative, mantissa, exponent) = SplitBinary64(d);
        if (mantissa == 0) return new(BigInteger.Zero, 0);
        BigInteger magnitude = mantissa;
        if (exponent >= 0) return new(negative ? -(magnitude << exponent) : magnitude << exponent, 0);
        int k = -exponent;
        magnitude *= BigInteger.Pow(5, k);
        return new(negative ? -magnitude : magnitude, k);
    }

    /// <summary>A finite binary64 as <c>(-1)^negative × mantissa × 2^exponent</c> with the mantissa ODD (a zero has mantissa 0): the one
    /// decomposition both exact forms write a float item's content from (<see cref="FromBinary64"/>, <c>CobolExact.FromBinary64</c>'s wide arm).</summary>
    internal static (bool Negative, long Mantissa, int Exponent) SplitBinary64(double d)
    {
        long bits = BitConverter.DoubleToInt64Bits(d);
        bool negative = bits < 0;
        int exponent = (int)((bits >> 52) & 0x7FF);
        long mantissa = bits & 0xFFFFFFFFFFFFFL;
        if (exponent == 0) exponent = 1; else mantissa |= 1L << 52;
        if (mantissa == 0) return (negative, 0, 0);
        exponent -= 1075;
        int trailing = BitOperations.TrailingZeroCount(mantissa);
        return (negative, mantissa >> trailing, exponent + trailing);
    }

    /// <summary>The value on the 256-bit form at its own scale when its integer fits, else false (the form this struct exists for).</summary>
    internal bool TryToWide(out CobolWide wide)
    {
        BigInteger magnitude = BigInteger.Abs(Value);
        if (magnitude.GetBitLength() > 256)
        {
            wide = default;
            return false;
        }
        wide = CobolWide.FromParts(Value.Sign < 0, (UInt128)(magnitude >> 128), (UInt128)(magnitude & LowMask));
        return true;
    }

    // ── powers of ten and digit counts ──────────────────────────────────────────────────────────────────────

    /// <summary>10^<paramref name="n"/> for 0 ≤ n &lt; 128, from an immutable table: the scales that recur inside one expression.</summary>
    private static readonly BigInteger[] SmallPowers = Enumerable.Range(0, 128).Select(i => BigInteger.Pow(10, i)).ToArray();

    /// <summary>10^<paramref name="n"/>: the table for a small power, else formed.</summary>
    internal static BigInteger Pow10(int n) => n < SmallPowers.Length ? SmallPowers[n] : BigInteger.Pow(10, n);

    /// <summary>The decimal digits of a positive integer: the logarithm's estimate, verified against the powers of ten around it.</summary>
    internal static int Digits(BigInteger magnitude)
    {
        if (magnitude < 10) return 1;
        int digits = (int)Math.Floor(BigInteger.Log10(magnitude)) + 1;
        if (magnitude < Pow10(digits - 1)) digits--;
        else if (magnitude >= Pow10(digits)) digits++;
        return digits;
    }

    private static int Guard(long digits) => digits > MaxBits ? throw Overflow() : (int)digits;

    // ── the operations ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>This value written at <paramref name="scale"/> (at least <see cref="Scale"/>): only zeros appended.</summary>
    internal CobolBig At(int scale) =>
        scale == Scale ? this : Of(Value * Pow10(Guard((long)scale - Scale)), scale, Working);

    /// <summary>The exact sum or difference, at the larger of the two scales.</summary>
    internal static CobolBig Add(CobolBig a, CobolBig b, bool subtract)
    {
        int scale = Math.Max(a.Scale, b.Scale);
        BigInteger left = a.At(scale).Value, right = b.At(scale).Value;
        return Of(subtract ? left - right : left + right, scale, a.Working || b.Working);
    }

    internal static CobolBig Negate(CobolBig a) => new(-a.Value, a.Scale, a.Working);

    /// <summary>The exact product, at the sum of the scales.</summary>
    internal static CobolBig Mul(CobolBig a, CobolBig b)
    {
        if (a.Value.GetBitLength() + b.Value.GetBitLength() > MaxBits + 1) throw Overflow();
        return Of(a.Value * b.Value, (long)a.Scale + b.Scale, a.Working || b.Working);
    }

    /// <summary>⛔ THE EXACT POWER <c>b ** n</c> (n ≥ 1; kb/Work PB4587): <c>mpz_pow_ui</c> with the scale times n, GnuCOBOL's
    /// <c>cob_decimal_pow</c>. The base's trailing zero digits move into the scale first (<c>1000 ** 30</c> stays small); a base of
    /// magnitude one is closed-form, and a power whose integer would pass <see cref="MaxBits"/> is EC-SIZE-OVERFLOW before it is formed.</summary>
    internal static CobolBig Pow(CobolBig b, long n)
    {
        BigInteger value = b.Value;
        long scale = b.Scale;
        if (value.IsZero) return new(BigInteger.Zero, (int)Math.Clamp(scale * n, int.MinValue, int.MaxValue), b.Working);
        while (value.IsEven && (value % 10).IsZero)
        {
            value /= 10;
            scale--;
        }
        if (value.IsOne || (-value).IsOne)
            return Of(value.Sign < 0 && (n & 1) != 0 ? BigInteger.MinusOne : BigInteger.One, scale * n, b.Working);
        if ((double)value.GetBitLength() * n > MaxBits)
            throw Digits(BigInteger.Abs(value)) - scale > 0 ? Overflow() : Underflow();   // |base| above one grows past the range, below one shrinks past it
        return Of(BigInteger.Pow(value, (int)n), scale * n, b.Working);
    }

    /// <summary>The quotient TRUNCATED toward zero at <paramref name="scale"/> fraction digits (<c>trunc(a × 10^(bScale + scale − aScale) / b)</c>).
    /// A zero divisor raises <see cref="CobolSizeError.ZeroDivide"/> (ISO §2023:14.7.5 case 2).</summary>
    internal static CobolBig DivTruncated(CobolBig a, CobolBig b, int scale)
    {
        if (b.Value.IsZero) throw CobolSizeError.ZeroDivide();
        long exp = (long)b.Scale + scale - a.Scale;
        BigInteger numerator = a.Value, denominator = b.Value;
        if (exp >= 0) numerator *= Pow10(Guard(exp)); else denominator *= Pow10(Guard(-exp));
        return Of(BigInteger.Divide(numerator, denominator), scale);
    }

    /// <summary>⛔ THE EXACT QUOTIENT OF TWO VALUES REDUCED TO THE SDIDI'S 34 DIGITS ONCE (<see cref="CobolWide.QuotientDec(int, CobolWide, int, CobolRounding, bool)"/>'s
    /// rule over this form): the dividend scaled until the integer quotient has 36 to 37 digits, the true remainder the sticky bit, and
    /// <see cref="CobolDec.Round34Wide"/> the one rounding — ROUND-TO-ODD with <paramref name="jam"/> (a native final-transfer quotient),
    /// else in <paramref name="mode"/> (MEAN's and MEDIAN's single division). An exact quotient keeps the preferred exponent, the dividend's less
    /// the divisor's (ISO/IEC 60559 5.4.1). A zero divisor raises <see cref="CobolSizeError.ZeroDivide"/>.</summary>
    internal static CobolDec QuotientDec(CobolBig a, CobolBig b, CobolRounding mode, bool jam)
    {
        if (b.Value.IsZero) throw CobolSizeError.ZeroDivide();
        long preferred = (long)b.Scale - a.Scale;
        if (a.Value.IsZero) return CobolDec.Zero(preferred);
        BigInteger magnitude = BigInteger.Abs(a.Value), divisor = BigInteger.Abs(b.Value);
        int up = Math.Max(0, 36 + Digits(divisor) - Digits(magnitude));
        BigInteger quotient = BigInteger.DivRem(magnitude * Pow10(up), divisor, out BigInteger remainder);
        bool inexact = !remainder.IsZero;
        long steps = up;
        int digits = Digits(quotient);
        if (digits > 37)
        {
            quotient = BigInteger.DivRem(quotient, Pow10(digits - 37), out remainder);
            inexact |= !remainder.IsZero;
            steps -= digits - 37;
        }
        int exp = (int)Math.Clamp(preferred - steps, int.MinValue, int.MaxValue);
        UInt128 low = (UInt128)quotient;
        if (!inexact && steps > 0) low = CobolDec.ToPreferredExponent(low, ref exp, (int)Math.Clamp(preferred, int.MinValue, int.MaxValue));
        return CobolDec.Round34Wide(0, low, (a.Value.Sign < 0) != (b.Value.Sign < 0), exp, mode, extraSticky: inexact, jam: jam);
    }

    /// <summary>⛔ THE EXACT QUOTIENT LANDED ONCE AT <paramref name="toScale"/> (kb/Work PB4701): the final transfer of a native
    /// <c>COMPUTE R = a / b</c> over exact operands, rounded ONCE in the receiver's <paramref name="mode"/> from the true remainder and
    /// landed as <see cref="ToUnscaled"/> lands a value — a quotient the receiver cannot hold keeps the exact quotient's own low-order
    /// digits (GnuCOBOL's <c>cob_decimal_div</c> then <c>cob_decimal_get_field</c>), not the low-order digits of a 34-digit image.</summary>
    internal static Int128 QuotientAtScale(CobolBig a, CobolBig b, int toScale, CobolRounding mode, bool checkedTransfer)
    {
        if (b.Value.IsZero) throw CobolSizeError.ZeroDivide();
        long exp = (long)b.Scale + toScale - a.Scale;
        BigInteger numerator = a.Value, denominator = b.Value;
        if (exp >= 0) numerator *= Pow10(Guard(exp)); else denominator *= Pow10(Guard(-exp));
        if (denominator.Sign < 0) (numerator, denominator) = (-numerator, -denominator);
        return Land(CobolNum.RoundDiv(numerator, denominator, mode), checkedTransfer);
    }

    /// <summary>The truncated remainder (<c>a − b × trunc(a / b)</c>, REM; the sign of the dividend) or the floored modulus (MOD; the sign
    /// of the divisor) of two values, exact, at their larger scale. A zero divisor is the caller's own rule.</summary>
    internal static CobolBig Remainder(CobolBig a, CobolBig b, bool floored)
    {
        int scale = Math.Max(a.Scale, b.Scale);
        BigInteger dividend = a.At(scale).Value, divisor = b.At(scale).Value;
        BigInteger remainder = BigInteger.Remainder(dividend, divisor);
        if (floored && !remainder.IsZero && remainder.Sign != divisor.Sign) remainder += divisor;
        return new(remainder, scale);
    }

    /// <summary>The integer part (truncation toward zero, or the floor) at scale 0, and whether a fraction was dropped.</summary>
    internal CobolBig IntegerPart(bool floor, out bool hadFraction)
    {
        if (Scale <= 0)
        {
            hadFraction = false;
            return new(Value * Pow10(Guard(-(long)Scale)), 0);
        }
        BigInteger integer = BigInteger.DivRem(Value, Pow10(Guard(Scale)), out BigInteger remainder);
        hadFraction = !remainder.IsZero;
        if (floor && hadFraction && Value.Sign < 0) integer -= 1;
        return new(integer, 0);
    }

    /// <summary>The value less its integer part (<c>argument − FUNCTION INTEGER-PART (argument)</c>), at the value's own scale: the
    /// fraction, with the sign of the value.</summary>
    internal CobolBig FractionPart() =>
        Scale <= 0 ? new(BigInteger.Zero, 0) : new(BigInteger.Remainder(Value, Pow10(Guard(Scale))), Scale);

    /// <summary>The algebraic comparison (−1/0/+1), exact for every pair.</summary>
    internal static int Compare(CobolBig a, CobolBig b)
    {
        int sa = a.Sign, sb = b.Sign;
        if (sa != sb) return sa < sb ? -1 : 1;
        if (sa == 0) return 0;
        int scale = Math.Max(a.Scale, b.Scale);
        BigInteger left = a.Value * (a.Scale == scale ? BigInteger.One : Pow10(Guard((long)scale - a.Scale)));
        BigInteger right = b.Value * (b.Scale == scale ? BigInteger.One : Pow10(Guard((long)scale - b.Scale)));
        return BigInteger.Compare(left, right);
    }

    // ── landing in a binary64 ───────────────────────────────────────────────────────────────────────────────

    /// <summary>The magnitude of this value, for <see cref="FloatResultant"/>'s binary64 rounding (kb/Work PB4730).</summary>
    internal Binary64Magnitude Magnitude() => new(BigInteger.Abs(Value), Scale);

    /// <summary>⛔ THE EXACT MAGNITUDE <c>Mag × 10^-Scale</c> AND THE FOUR QUESTIONS THE BINARY64 ROUNDING ASKS OF IT, answered in integer
    /// arithmetic (<see cref="IBinary64Magnitude"/>; the 128-bit carrier's twin is <c>FloatResultant.ExactMagnitude</c>): every comparison is
    /// <c>m × 2^e</c> against the value with both sides cleared of their negative powers, so no step rounds.</summary>
    internal readonly struct Binary64Magnitude(BigInteger magnitude, int scale) : IBinary64Magnitude
    {
        /// <summary>The correctly rounded (ties-to-even) binary64: .NET's parse is IEEE-correct for any digit count and exponent, and
        /// returns the infinity a magnitude past the range rounds to.</summary>
        public double NearestBinary64() => double.Parse(
            magnitude.ToString(System.Globalization.CultureInfo.InvariantCulture) + "E" + (-(long)scale).ToString(System.Globalization.CultureInfo.InvariantCulture),
            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>sign(m × 2^e − V): <c>m × 2^e × 10^scale</c> against <c>Mag</c>, each side multiplied up until neither has a negative power.</summary>
        private int SignOfDyadicMinusValue(BigInteger m, int e)
        {
            BigInteger left = m, right = magnitude;
            if (e > 0) left <<= e; else if (e < 0) right <<= -e;
            if (scale > 0) left *= Pow10(Guard(scale)); else if (scale < 0) right *= Pow10(Guard(-(long)scale));
            return left.CompareTo(right);
        }

        /// <summary>sign(c − V) for a finite binary64 <paramref name="c"/> ≥ 0, read as its integer significand and binary exponent.</summary>
        public int SignAgainst(double c)
        {
            (BigInteger m, int e) = Decompose(c);
            return SignOfDyadicMinusValue(m, e);
        }

        public bool AtLeastTwoTo1024() => SignOfDyadicMinusValue(BigInteger.One, 1024) <= 0;

        /// <summary>V is a midpoint exactly when it equals <c>(a + b) / 2</c> for two adjacent binary64 values a &lt; b: each is an integer multiple
        /// of 2^-1074, so <c>(A + B) × 2^-1075</c> with A and B their integers is the midpoint, and the nearest binary64 is one of its two ends.
        /// MaxValue and the virtual 2^1024 have the midpoint <c>(2^54 - 1) × 2^970</c>.</summary>
        public bool IsBinary64Tie()
        {
            double near = NearestBinary64();
            if (double.IsPositiveInfinity(near))
                return SignOfDyadicMinusValue((BigInteger.One << 54) - 1, 970) == 0;
            BigInteger nearUnits = Units(near);
            if (near < double.MaxValue && SignOfDyadicMinusValue(nearUnits + Units(Math.BitIncrement(near)), -1075) == 0) return true;
            return near > 0 && SignOfDyadicMinusValue(nearUnits + Units(Math.BitDecrement(near)), -1075) == 0;
        }

        /// <summary>A finite binary64 of at least 0 as an integer count of its least unit, 2^-1074.</summary>
        private static BigInteger Units(double c)
        {
            (BigInteger m, int e) = Decompose(c);
            return m << (e + 1074);
        }

        /// <summary>A finite binary64 of at least 0 as <c>m × 2^e</c>: the significand with its hidden bit, and its exponent (-1074 for a subnormal).</summary>
        private static (BigInteger Significand, int Exponent) Decompose(double c)
        {
            long bits = BitConverter.DoubleToInt64Bits(c);
            int exponent = (int)((bits >> 52) & 0x7FF);
            long fraction = bits & 0xFFFFFFFFFFFFFL;
            return exponent == 0 ? (fraction, -1074) : (fraction | (1L << 52), exponent - 1075);
        }
    }

    // ── settling ────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The value reduced to the SDIDI's 34 significant digits once: ROUND-TO-ODD with <paramref name="jam"/> (the lowering a
    /// consumer with no exact form takes, <see cref="CobolWide.ToDec"/>'s rule), else in <paramref name="mode"/>.</summary>
    internal CobolDec ToDec(CobolRounding mode, bool jam)
    {
        if (Value.IsZero) return CobolDec.Zero(-(long)Scale);
        BigInteger magnitude = BigInteger.Abs(Value);
        int digits = Digits(magnitude);
        bool sticky = false;
        long exp = -(long)Scale;
        // 35 digits are kept — the 34 of the image and the ROUND DIGIT — because CobolDec.Round34Wide reads the round digit from the one
        // digit it drops itself: cut to exactly 34 here, the digit is gone and a nearest mode never rounds up (review R1-1).
        if (digits > 35)
        {
            magnitude = BigInteger.DivRem(magnitude, Pow10(digits - 35), out BigInteger dropped);
            sticky = !dropped.IsZero;
            exp += digits - 35;
        }
        return CobolDec.Round34Wide(0, (UInt128)magnitude, Value.Sign < 0, (int)Math.Clamp(exp, int.MinValue, int.MaxValue), mode, sticky, jam);
    }

    /// <summary>⛔ THE FINAL TRANSFER (<see cref="CobolWide.ToUnscaled"/>'s rule over this form): moved to <paramref name="toScale"/> and
    /// rounded ONCE in the receiver's <paramref name="mode"/> (PROHIBITED raises on an inexact value), then landed as <see cref="Land"/>.</summary>
    internal Int128 ToUnscaled(int toScale, CobolRounding mode, bool checkedTransfer)
    {
        if (Value.IsZero) return 0;
        long shift = (long)Scale - toScale;
        BigInteger landed = Value;
        if (shift > 0)
        {
            // A shift past the value's own digits leaves a fraction below one hundredth of a unit whatever its size: the clamp keeps
            // the divisor small and the rounding decision (the remainder against the half) the same.
            int drop = (int)Math.Min(shift, (long)Digits(BigInteger.Abs(Value)) + 2);
            landed = CobolNum.RoundDiv(Value, Pow10(drop), mode);
        }
        else if (shift < 0) landed = Value * Pow10(Guard(-shift));
        return Land(landed, checkedTransfer);
    }

    /// <summary>An integer landed in the <see cref="Int128"/> carrier: itself when it fits (the carrier's magnitude limit is
    /// <see cref="Int128.MaxValue"/> on both signs, <c>CobolDec.TransferWide</c>'s), else EC-SIZE-TRUNCATION under a checked transfer
    /// (§2023:14.7.5 case 3) and the value's low-order 38 digits — what a store can use — otherwise.</summary>
    private static Int128 Land(BigInteger integer, bool checkedTransfer)
    {
        if (BigInteger.Abs(integer) <= (BigInteger)Int128.MaxValue) return (Int128)integer;
        if (checkedTransfer) throw PastTheCarrier();
        return (Int128)BigInteger.Remainder(integer, Pow10(38));
    }

    /// <summary><see cref="Land(BigInteger, bool)"/> for an integer the 128-bit magnitude <paramref name="magnitude"/> holds (the wide form's
    /// quotient, <c>CobolExact.QuotientAtScale</c>): the one landing rule, spelled once for both magnitudes.</summary>
    internal static Int128 Land(UInt128 magnitude, bool negative, bool checkedTransfer)
    {
        if (magnitude <= (UInt128)Int128.MaxValue) return negative ? -(Int128)magnitude : (Int128)magnitude;
        if (checkedTransfer) throw PastTheCarrier();
        Int128 low = (Int128)(magnitude % TenTo38);
        return negative ? -low : low;
    }

    private static CobolSizeError PastTheCarrier() =>
        new("the value is further from zero than any fixed-point receiver permits (ISO §2023:14.7.5 "
            + "case 3 — EC-SIZE-TRUNCATION; the receiver is left unchanged)", "EC-SIZE-TRUNCATION");
}
