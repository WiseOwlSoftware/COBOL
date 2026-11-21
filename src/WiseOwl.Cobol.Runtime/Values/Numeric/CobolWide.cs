// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE EXACT WIDE INTERMEDIATE OF NATIVE ARITHMETIC (kb/Work PB1900): a signed 256-bit integer — sign and a
/// <see cref="UInt128"/> pair of magnitude — that carries an UNSCALED value whose decimal scale the compiler tracks
/// (exactly as it tracks the scale of the <see cref="Int128"/> lane). It exists so that a native product past the
/// <see cref="Int128"/> carrier, NESTED in a larger expression, keeps every digit until the one final transfer
/// (§2023:14.7.7 rule 3 NOTE 1: ROUNDED governs only that transfer): <c>COMPUTE R = A * B - C * D</c> over
/// <c>PIC 9(21)</c> operands forms two 41-digit products and subtracts them EXACTLY. The 34-digit SDIDI
/// (a product reduced by <see cref="CobolDec.FromWideToOdd"/>) it replaces rounded each product first, and the cancellation of the two nearly
/// equal products left only the rounding error.
/// <para>The implementor's choice (§2023:8.8.1.3 — "Native arithmetic is an implementor-defined method of evaluating an
/// arithmetic expression"; §2023:14.7.7 4) a) — "an implementor-defined intermediate data item is used"; CONFORMANCE.md
/// DOC-A.1-123) follows GnuCOBOL (CLAUDE.md rule 1), whose <c>cob_decimal_mul</c>, <c>cob_decimal_add</c> and
/// <c>cob_decimal_sub</c> are exact over an arbitrary-precision integer. This type is that integer, bounded: the
/// compiler selects it only where its digit-bound analysis PROVES the exact value fits <see cref="MaxDigits"/>
/// decimal digits (the bound is a property of the operands' PICTUREs, not of their values), and falls back to the
/// SDIDI's round-to-odd beyond it. It also carries a NESTED quotient whose bound passes the <see cref="Int128"/> lane
/// (<see cref="DivTruncated"/>, the same truncated value that lane forms; kb/Work PB3538). Every constructive operation
/// (<see cref="Mul"/>, <see cref="Up"/>, <see cref="Add"/>, <see cref="Sub"/>) also checks its own overflow and raises EC-SIZE-OVERFLOW (§2023:14.7.5 case 5, A.1 item 179), so the bound
/// is a compile-time optimisation of a run-time guarantee: this type never wraps.</para>
/// <para>It is NOT a carrier a consumer ever sees. <c>NumericRenderer</c> settles it at every public entry — into the
/// receiver's own scale and mode at a final transfer (<see cref="ToUnscaled"/>), into the SDIDI otherwise
/// (<see cref="ToDec"/>) — so the rest of the compiler keeps its three carriers. The consumers that take it EXACT form a
/// closed list (<c>ExactWideSettlementDriftTests</c>): the arithmetic statements, the relation, and an intrinsic function's
/// argument (kb/Work PB3953: the exact intrinsic family's wide bodies, <c>CobolIntrinsics.Wide.cs</c>, and the temporary
/// DISPLAY shows, <see cref="FunctionValueField"/>).</para>
/// </summary>
public readonly struct CobolWide
{
    /// <summary>The widest decimal magnitude every value of which fits the 256-bit magnitude: 10^77 &lt; 2^256 ≈ 1.158×10^77,
    /// so any value below 10^77 is representable. The compiler's digit-bound analysis (<c>NumX.Digits</c>) selects this
    /// type only for bounds up to this width.</summary>
    public const int MaxDigits = 77;

    /// <summary>The base-10^18 limbs a magnitude below 2^256 needs (10^90 &gt; 2^256): the length of the span
    /// <see cref="MagnitudeLimbs"/> fills.</summary>
    internal const int WideLimbs = 5;

    private readonly UInt128 _hi;
    private readonly UInt128 _lo;
    private readonly bool _negative;

    private CobolWide(bool negative, UInt128 hi, UInt128 lo)
    {
        _hi = hi;
        _lo = lo;
        _negative = negative && (hi != 0 || lo != 0);   // zero has no sign
    }

    private bool IsZero => _hi == 0 && _lo == 0;

    /// <summary>The magnitude's high and low 128 bits and the sign: the parts <see cref="CobolBig"/> (the arbitrary-precision form
    /// past this one, kb/Work PB4592) converts through, so this file holds no arbitrary-precision integer.</summary>
    internal (bool Negative, UInt128 High, UInt128 Low) Parts => (_negative, _hi, _lo);

    /// <summary>The wide value of the given sign and magnitude parts (<see cref="Parts"/>).</summary>
    internal static CobolWide FromParts(bool negative, UInt128 hi, UInt128 lo) => new(negative, hi, lo);

    /// <summary>The algebraic sign of the value: −1, 0 or +1.</summary>
    internal int Sign => IsZero ? 0 : _negative ? -1 : 1;

    /// <summary>An <see cref="Int128"/> value (any scale) as a wide value of the same unscaled magnitude.</summary>
    public static CobolWide From(Int128 value) => new(value < 0, 0, CobolDec.UAbs(value));

    /// <summary>An unsigned 16-byte value (a <see cref="UInt128"/> container, any scale) as a wide value of the same unscaled
    /// magnitude. A distinct NAME, not an overload of <see cref="From"/>: a <c>ulong</c> converts implicitly to both
    /// <see cref="Int128"/> and <see cref="UInt128"/>, so an overload pair would make an emitted call ambiguous (CS0121).</summary>
    public static CobolWide FromUnsigned(UInt128 value) => new(false, 0, value);

    /// <summary>The value as an <see cref="Int128"/> when it fits one (the same unscaled magnitude), else false.</summary>
    internal bool TryToInt128(out Int128 value)
    {
        value = 0;
        if (_hi != 0 || _lo > (UInt128)Int128.MaxValue) return false;
        value = _negative ? -(Int128)_lo : (Int128)_lo;
        return true;
    }

    /// <summary>⛔ AN SDIDI VALUE AS A WIDE VALUE AT THE SCALE IT IS WRITTEN AT, EXACTLY (kb/Work PB3953): <paramref name="value"/> =
    /// <c>Sig × 10^Exp</c> is the wide <paramref name="wide"/> at <paramref name="scale"/> = −Exp, or, for a positive exponent, the
    /// integer <c>Sig × 10^Exp</c> at scale 0. False when that integer is past the 256-bit range (the value is then larger in magnitude
    /// than every wide value, which is all a comparison needs to know).</summary>
    internal static bool TryFrom(CobolDec value, out CobolWide wide, out int scale)
    {
        scale = value.Exp < 0 ? -value.Exp : 0;
        return TryUp(From(value.Sig), value.Exp > 0 ? value.Exp : 0, out wide);
    }

    /// <summary>The magnitude of the value (§2023:15.7.4 ABS of the unscaled value; the caller keeps the scale).</summary>
    internal static CobolWide Abs(CobolWide a) => new(false, a._hi, a._lo);

    /// <summary>10^<paramref name="digits"/> (0 ≤ <paramref name="digits"/> ≤ 76), the divisor that cuts a value at a scale.</summary>
    internal static CobolWide PowerOfTen(int digits) => Up(From(1), digits);

    /// <summary>The quotient of <paramref name="a"/> by <paramref name="b"/> TRUNCATED toward zero, with the remainder, which has
    /// the dividend's sign (<c>a = b × quotient + remainder</c>, |remainder| &lt; |b|) — both unscaled values at one scale. A zero
    /// divisor is the caller's own rule (§2023:15.64.3 r2, §2023:15.77.3 r2), so it is refused here as a defect.</summary>
    internal static CobolWide DivRem(CobolWide a, CobolWide b, out CobolWide remainder)
    {
        if (b.IsZero) throw new DivideByZeroException("CobolWide.DivRem with a zero divisor: the caller owns the zero-divisor rule");
        var (hi, lo) = DivideMagnitude(new CobolWide(false, a._hi, a._lo), new CobolWide(false, b._hi, b._lo));
        var quotient = new CobolWide(a._negative != b._negative, hi, lo);
        remainder = Sub(a, Mul(b, quotient));   // |b × quotient| ≤ |a|: neither step can leave the range
        return quotient;
    }

    /// <summary>The decimal digits of the magnitude, unpadded ("0" for zero) — the digit string the returned value's temporary is
    /// described by (<c>CobolNum.ReturnedValueProfile</c>). Long division of the four 64-bit limbs by 10^19: each partial
    /// remainder is below 10^19 &lt; 2^64, so <c>(remainder &lt;&lt; 64) | limb</c> fits one <see cref="UInt128"/>.</summary>
    internal string MagnitudeDigits()
    {
        if (IsZero) return "0";
        const ulong Chunk = 10_000_000_000_000_000_000UL;   // 10^19
        Span<ulong> limbs = [(ulong)(_hi >> 64), (ulong)_hi, (ulong)(_lo >> 64), (ulong)_lo];
        Span<ulong> chunks = stackalloc ulong[5];   // 10^95 > 2^256: five base-10^19 chunks hold any magnitude
        int count = 0;
        while (limbs[0] != 0 || limbs[1] != 0 || limbs[2] != 0 || limbs[3] != 0)
        {
            UInt128 remainder = 0;
            for (int i = 0; i < limbs.Length; i++)
            {
                UInt128 current = (remainder << 64) | limbs[i];
                limbs[i] = (ulong)(current / Chunk);
                remainder = current % Chunk;
            }
            chunks[count++] = (ulong)remainder;
        }
        var text = new System.Text.StringBuilder(count * 19);
        text.Append(chunks[count - 1].ToString(System.Globalization.CultureInfo.InvariantCulture));
        for (int i = count - 2; i >= 0; i--) text.Append(chunks[i].ToString("D19", System.Globalization.CultureInfo.InvariantCulture));
        return text.ToString();
    }

    /// <summary>The magnitude as base-10^18 limbs, least significant first, into <paramref name="limbs"/>
    /// (<see cref="WideLimbs"/> long): long division of the four 64-bit limbs by 10^18, whose partial remainders are below
    /// 10^18 &lt; 2^60, so <c>(remainder &lt;&lt; 64) | limb</c> fits one <see cref="UInt128"/>. The input of the
    /// quarter-turn reduction (<c>CobolIntrinsics.ReduceQuarterTurns</c>).</summary>
    internal void MagnitudeLimbs(Span<long> limbs)
    {
        const ulong Base = 1_000_000_000_000_000_000UL;   // 10^18
        Span<ulong> words = [(ulong)(_hi >> 64), (ulong)_hi, (ulong)(_lo >> 64), (ulong)_lo];
        for (int n = 0; n < limbs.Length; n++)
        {
            UInt128 remainder = 0;
            for (int i = 0; i < words.Length; i++)
            {
                UInt128 current = (remainder << 64) | words[i];
                words[i] = (ulong)(current / Base);
                remainder = current % Base;
            }
            limbs[n] = (long)remainder;
        }
    }

    /// <summary>⛔ THE EXACT QUOTIENT OF THIS VALUE (at <paramref name="scale"/>) BY A COUNT, ROUNDED ONCE TO THE SDIDI
    /// (kb/Work PB4075): the single division of FUNCTION MEAN's <c>Σ / n</c> (ISO §2023:15.60.4 r1, kb/Work PB3860) over an exact
    /// wide sum, where <see cref="ToDec"/> then <see cref="CobolDec.Div"/> would round twice —
    /// <see cref="QuotientDec(int, CobolWide, int, CobolRounding, bool)"/>.</summary>
    internal CobolDec QuotientDec(int scale, int count, CobolRounding mode) =>
        QuotientDec(scale, From(count), 0, mode, jam: false);

    /// <summary>⛔ THE ONE EXACT QUOTIENT OF TWO SCALED WIDE VALUES, ROUNDED ONCE TO THE SDIDI (kb/Work PB4075, PB4111, PB4599): this
    /// value at <paramref name="scale"/> over a nonzero <paramref name="divisor"/> at <paramref name="divisorScale"/>, to 34 digits in
    /// <paramref name="mode"/> — MEAN's, VARIANCE's and PRESENT-VALUE's single division and, with <paramref name="jam"/>, a native
    /// final-transfer quotient over exact operands (<c>CobolExact.DivToOdd</c>). With <paramref name="jam"/> the 34-digit result is
    /// ROUND-TO-ODD (<see cref="CobolDec.DivToOdd"/>'s rule), whatever <paramref name="mode"/>: an inexact quotient keeps an odd last
    /// digit, so the receiver's one rounding sees the exact quotient's side of every boundary. It divides the EXACT operands: the
    /// 34-digit image of a 45-digit dividend over its factor stored 999999999999998 where 999999999999999 is owed.
    /// <para>The dividend is scaled by powers of ten until the integer quotient has 36 or 37 digits, the true remainder is the sticky
    /// bit, and <see cref="CobolDec.Round34Wide"/> lands the 34-digit significand — the rounding <see cref="CobolDec.Div"/> applies to
    /// an SDIDI dividend, so the two agree wherever both can form the quotient. When that scaled dividend would leave the 256-bit range
    /// (a divisor past 41 digits) the quotient digits past the integer part are drawn one at a time from the remainder
    /// (<see cref="NextQuotientDigit"/>), which never leaves it: no divisor within the form is refused. An exact quotient keeps the
    /// preferred exponent, the dividend's less the divisor's (ISO/IEC 60559 5.4.1, kb/Work PB3954). A zero divisor raises
    /// <see cref="CobolSizeError.ZeroDivide"/> (ISO §2023:14.7.5 case 2).</para></summary>
    internal CobolDec QuotientDec(int scale, CobolWide divisor, int divisorScale, CobolRounding mode, bool jam)
    {
        if (divisor.IsZero) throw CobolSizeError.ZeroDivide();
        int preferred = divisorScale - scale;
        if (IsZero) return CobolDec.Zero(preferred);
        CobolWide magnitude = Abs(this), d = Abs(divisor);
        // The integer quotient of dm digits by dn digits has at least dm − dn: scaling by 36 + dn − dm leaves it 36 or 37 digits.
        int up = Math.Max(0, 36 + d.MagnitudeDigits().Length - magnitude.MagnitudeDigits().Length);
        UInt128 hi, lo;
        int steps;
        bool inexact;
        if (TryUp(magnitude, up, out CobolWide scaled))
        {
            CobolWide quotient = DivRem(scaled, d, out CobolWide remainder);
            (hi, lo, steps, inexact) = (quotient._hi, quotient._lo, up, !remainder.IsZero);
        }
        else
        {
            // A divisor of 42+ digits over a dividend of at most 77: the integer quotient has at most 37 digits, so it is below 2^128.
            CobolWide quotient = DivRem(magnitude, d, out CobolWide remainder);
            (hi, lo, steps) = (UInt128.Zero, quotient._lo, 0);
            UInt128 rHi = remainder._hi, rLo = remainder._lo;
            while (lo < (UInt128)Pow10.AsWide(35) && (rHi != 0 || rLo != 0))     // fewer than 36 digits and a remainder left
            {
                lo = lo * 10u + (UInt128)NextQuotientDigit(ref rHi, ref rLo, d._hi, d._lo);
                steps++;
            }
            inexact = rHi != 0 || rLo != 0;
        }
        int exp = preferred - steps;
        if (!inexact && steps > 0) lo = CobolDec.ToPreferredExponent(lo, ref exp, preferred);   // below 2^128: 36 or 37 digits, or fewer when exact
        return CobolDec.Round34Wide(hi, lo, _negative != divisor._negative, exp, mode, extraSticky: inexact, jam: jam);
    }

    /// <summary>The next decimal digit of a quotient whose remainder is <paramref name="rHi"/>:<paramref name="rLo"/> (below the
    /// divisor <paramref name="dHi"/>:<paramref name="dLo"/>): <c>⌊10 × remainder / divisor⌋</c>, leaving <c>10 × remainder mod
    /// divisor</c> in the remainder. The product is formed as ten additions of the remainder, each followed by at most one subtraction
    /// of the divisor (the running value stays below it, so the sum is below twice it): a sum that carried out of bit 255 is
    /// larger than the divisor by that fact and the wrapped subtraction lands it back in range — as <see cref="DivideMagnitude"/>'s
    /// carried remainder. It is the digit step of the quotient whose scaled dividend would leave the 256-bit form.</summary>
    private static int NextQuotientDigit(ref UInt128 rHi, ref UInt128 rLo, UInt128 dHi, UInt128 dLo)
    {
        UInt128 accHi = 0, accLo = 0;
        int digit = 0;
        for (int i = 0; i < 10; i++)
        {
            UInt128 lo = accLo + rLo;
            UInt128 carry = lo < accLo ? 1u : 0u;
            UInt128 hi = accHi + rHi;
            bool over = hi < accHi;
            UInt128 carried = hi + carry;
            over |= carried < hi;
            (accHi, accLo) = (carried, lo);
            if (!over && (accHi < dHi || (accHi == dHi && accLo < dLo))) continue;
            UInt128 borrow = accLo < dLo ? 1u : 0u;
            accLo -= dLo;
            accHi = accHi - dHi - borrow;
            digit++;
        }
        (rHi, rLo) = (accHi, accLo);
        return digit;
    }

    /// <summary>This value, at <paramref name="scale"/>, rounded ONCE to the SDIDI's 34 digits in <paramref name="mode"/> — a
    /// value COMPUTED from exact arguments (a sum, a difference, a remainder), where <see cref="ToDec"/>'s round-to-odd is the
    /// lowering of an operand that something else rounds next.</summary>
    internal CobolDec ToDecRounded(int scale, CobolRounding mode) =>
        IsZero ? CobolDec.Zero(-scale) : CobolDec.Round34Wide(_hi, _lo, _negative, -scale, mode);

    /// <summary>The greatest common divisor of two magnitudes (Euclid over <see cref="DivRem"/>); 0 only for two zeros.</summary>
    internal static CobolWide Gcd(CobolWide a, CobolWide b)
    {
        a = Abs(a);
        b = Abs(b);
        while (!b.IsZero)
        {
            DivRem(a, b, out CobolWide remainder);
            (a, b) = (b, remainder);
        }
        return a;
    }

    private static CobolSizeError Overflow() => new("a native intermediate exceeds the exact 256-bit range "
        + "(ISO §2023:14.7.5 case 5 — the implementor-defined intermediate range is checked, A.1 item 179: EC-SIZE-OVERFLOW)",
        "EC-SIZE-OVERFLOW");

    public static CobolWide Negate(CobolWide a) => new(!a._negative, a._hi, a._lo);

    /// <summary>The exact product of the unscaled values (the caller adds the scales).</summary>
    public static CobolWide Mul(CobolWide a, CobolWide b) =>
        TryMul(a, b, out CobolWide product) ? product : throw Overflow();

    /// <summary><see cref="Mul"/> where the product may leave the 256-bit range: false then.</summary>
    internal static bool TryMul(CobolWide a, CobolWide b, out CobolWide product)
    {
        product = default;
        if (a.IsZero || b.IsZero) return true;
        if (a._hi != 0 && b._hi != 0) return false;                       // both past 2^128: the product is past 2^256
        var (hi, lo) = CobolDec.Mul128(a._lo, b._lo);
        if (a._hi != 0 || b._hi != 0)
        {
            // (ah·2^128 + al)·bl = ah·bl·2^128 + al·bl — the cross term must itself fit 128 bits, and so must its sum.
            var (crossHi, crossLo) = a._hi != 0 ? CobolDec.Mul128(a._hi, b._lo) : CobolDec.Mul128(a._lo, b._hi);
            if (crossHi != 0) return false;
            UInt128 sum = hi + crossLo;
            if (sum < hi) return false;
            hi = sum;
        }
        product = new CobolWide(a._negative != b._negative, hi, lo);
        return true;
    }

    /// <summary>The exact power <paramref name="a"/>^<paramref name="n"/> of the unscaled value (the caller multiplies the
    /// scale by <paramref name="n"/>), for <paramref name="n"/> ≥ 1 — square-and-multiply over <see cref="Mul"/>. Every
    /// square it forms is a factor of the result, so the intermediates are never wider than the answer and the
    /// compiler's digit bound (<c>base digits × n</c> ≤ <see cref="MaxDigits"/>) proves the call cannot overflow.</summary>
    public static CobolWide Pow(CobolWide a, int n) =>
        TryPow(a, n, out CobolWide power) ? power : throw Overflow();

    /// <summary><see cref="Pow"/> where the power may leave the 256-bit range: false then (the caller falls back to the SDIDI).</summary>
    public static bool TryPow(CobolWide a, long n, out CobolWide power)
    {
        power = From(1);
        CobolWide square = a;
        for (long m = n; m > 0; m >>= 1)
        {
            if ((m & 1) != 0 && !TryMul(power, square, out power)) return false;
            if (m > 1 && !TryMul(square, square, out square)) return false;
        }
        return true;
    }

    /// <summary><paramref name="a"/> × 10^<paramref name="digits"/> — the alignment of the lower-scaled operand of a sum
    /// to the higher scale.</summary>
    public static CobolWide Up(CobolWide a, int digits) =>
        TryUp(a, digits, out CobolWide aligned) ? aligned : throw Overflow();

    internal static bool TryUp(CobolWide a, int digits, out CobolWide aligned)
    {
        aligned = a;
        while (digits > 0)
        {
            int step = Math.Min(digits, 38);
            if (!TryMul(aligned, From(Pow10.AsWide(step)), out aligned)) return false;
            digits -= step;
        }
        return true;
    }

    /// <summary>The exact sum of two unscaled values ALREADY at one scale (the compiler aligns with <see cref="Up"/>).</summary>
    public static CobolWide Add(CobolWide a, CobolWide b) =>
        TryAdd(a, b, out CobolWide sum) ? sum : throw Overflow();

    /// <summary><see cref="Add"/> where the sum may leave the 256-bit range: false then.</summary>
    internal static bool TryAdd(CobolWide a, CobolWide b, out CobolWide sum)
    {
        sum = default;
        if (a._negative == b._negative || a.IsZero || b.IsZero)
        {
            if (a.IsZero) { sum = b; return true; }
            if (b.IsZero) { sum = a; return true; }
            UInt128 lo = a._lo + b._lo;
            UInt128 carry = lo < a._lo ? 1u : 0u;
            UInt128 hi = a._hi + b._hi;
            bool over = hi < a._hi;
            UInt128 hiCarried = hi + carry;
            if (over || hiCarried < hi) return false;
            sum = new CobolWide(a._negative, hiCarried, lo);
            return true;
        }
        // Opposite signs: the larger magnitude less the smaller, carrying the larger one's sign.
        int order = CompareMagnitude(a, b);
        if (order == 0) return true;
        (CobolWide big, CobolWide small) = order > 0 ? (a, b) : (b, a);
        UInt128 dlo = big._lo - small._lo;
        UInt128 borrow = big._lo < small._lo ? 1u : 0u;
        sum = new CobolWide(big._negative, big._hi - small._hi - borrow, dlo);
        return true;
    }

    /// <summary>The exact difference of two unscaled values already at one scale.</summary>
    public static CobolWide Sub(CobolWide a, CobolWide b) => Add(a, Negate(b));

    /// <summary>⛔ THE NESTED QUOTIENT ON THE EXACT WIDE FORM (kb/Work PB3538, review finding R1-1): the quotient of
    /// <paramref name="a"/> (at <paramref name="aScale"/>) by <paramref name="b"/> (at <paramref name="bScale"/>) TRUNCATED at
    /// <paramref name="resultScale"/>, as the unscaled value at that scale — the same value <c>CobolNum.Divide</c> forms in
    /// <see cref="CobolRounding.Truncation"/> on the <see cref="Int128"/> lane, for operands whose digit bounds do not prove
    /// that lane holds the quotient. The lane is a representation the compiler picks from bounds; the value is one rule
    /// (CONFORMANCE.md DOC-A.1-123), so `FUNCTION INTEGER(X) / 7 * 7` and `X / 7 * 7` are the same number.
    /// <para>A zero divisor raises <see cref="CobolSizeError.ZeroDivide"/> (ISO §2023:14.7.5 case 2). The dividend's radix
    /// alignment is <see cref="Up"/>, which raises EC-SIZE-OVERFLOW past the 256-bit range (§2023:14.7.5 case 5; the compiler's
    /// bound proves it cannot); a divisor aligned past that range is larger than any dividend, so the quotient is 0.</para></summary>
    public static CobolWide DivTruncated(CobolWide a, int aScale, CobolWide b, int bScale, int resultScale)
    {
        if (b.IsZero) throw CobolSizeError.ZeroDivide();
        if (a.IsZero) return default;
        int exp = bScale + resultScale - aScale;     // quotient_unscaled = trunc(a × 10^exp / b)
        CobolWide num = exp > 0 ? Up(a, exp) : a;
        CobolWide den = b;
        if (exp < 0 && !TryUp(b, -exp, out den)) return default;
        var (hi, lo) = DivideMagnitude(num, den);
        return new CobolWide(a._negative != b._negative, hi, lo);
    }

    /// <summary>The truncated quotient of two 256-bit magnitudes (<paramref name="d"/> nonzero): the native divide when
    /// both fit 128 bits, else binary shift-subtract from the dividend's top bit — clarity over speed until profiled, as
    /// <c>CobolDec.DivRem256</c>. The partial remainder stays below <paramref name="d"/>, so after its one-bit shift it is
    /// below 2^257: the bit shifted out of the top is carried, and a carried remainder is always at least the divisor.</summary>
    private static (UInt128 Hi, UInt128 Lo) DivideMagnitude(CobolWide n, CobolWide d)
    {
        if (n._hi == 0 && d._hi == 0) return (0, n._lo / d._lo);
        if (CompareMagnitude(n, d) < 0) return (0, 0);
        UInt128 qHi = 0, qLo = 0, rHi = 0, rLo = 0;
        int top = n._hi != 0 ? 255 - (int)UInt128.LeadingZeroCount(n._hi) : 127 - (int)UInt128.LeadingZeroCount(n._lo);
        for (int i = top; i >= 0; i--)
        {
            bool carried = (rHi >> 127) != 0;
            UInt128 bit = (i >= 128 ? n._hi >> (i - 128) : n._lo >> i) & 1;
            rHi = (rHi << 1) | (rLo >> 127);
            rLo = (rLo << 1) | bit;
            if (!carried && (rHi < d._hi || (rHi == d._hi && rLo < d._lo))) continue;
            UInt128 borrow = rLo < d._lo ? 1u : 0u;
            rLo -= d._lo;
            rHi = rHi - d._hi - borrow;                 // wraps back into range when the remainder was carried
            if (i >= 128) qHi |= (UInt128)1 << (i - 128); else qLo |= (UInt128)1 << i;
        }
        return (qHi, qLo);
    }

    private static int CompareMagnitude(CobolWide a, CobolWide b) =>
        a._hi != b._hi ? (a._hi < b._hi ? -1 : 1)
        : a._lo != b._lo ? (a._lo < b._lo ? -1 : 1)
        : 0;

    /// <summary>The algebraic comparison (−1/0/+1) of two scaled values — exact, and never a size error: an operand whose
    /// alignment to the other's scale would leave the 256-bit range is larger in magnitude than the other BY THAT FACT.</summary>
    public static int Compare(CobolWide a, int aScale, CobolWide b, int bScale)
    {
        int sa = a.Sign, sb = b.Sign;
        if (sa != sb) return sa < sb ? -1 : 1;
        if (sa == 0) return 0;
        int magnitude;
        if (aScale == bScale) magnitude = CompareMagnitude(a, b);
        else if (aScale < bScale) magnitude = TryUp(a, bScale - aScale, out CobolWide aUp) ? CompareMagnitude(aUp, b) : 1;
        else magnitude = TryUp(b, aScale - bScale, out CobolWide bUp) ? CompareMagnitude(a, bUp) : -1;
        return sa < 0 ? -magnitude : magnitude;
    }

    /// <summary>⛔ THE FINAL TRANSFER (kb/Work PB1900): this value, at <paramref name="scale"/>, moved to the receiver's
    /// <paramref name="toScale"/> and rounded ONCE with the receiver's <paramref name="mode"/> —
    /// <see cref="CobolDec.TransferWide"/>, the kernel <see cref="CobolDec.MulAtScale"/> shares, with its two statement
    /// dispositions (<paramref name="checkedTransfer"/>: a value past the carrier is EC-SIZE-TRUNCATION and PROHIBITED
    /// raises on an inexact one; unchecked, the low-order digits a store can use).</summary>
    public Int128 ToUnscaled(int scale, int toScale, CobolRounding mode, bool checkedTransfer) =>
        IsZero ? 0 : CobolDec.TransferWide(_negative, _hi, _lo, scale - toScale, mode, checkedTransfer);

    /// <summary>This value, at <paramref name="scale"/>, as the SDIDI reduced to 34 significant digits by ROUND-TO-ODD
    /// (<see cref="CobolDec.FromWideToOdd"/>'s rule): the lowering a consumer that has no wide form takes, so that
    /// whatever rounds it next, in any mode, sees the exact value on the same side of every boundary.</summary>
    public CobolDec ToDec(int scale) => CobolDec.FromWideToOdd(_negative, _hi, _lo, -scale);   // a zero is held at -scale too

    /// <summary>This value, at <paramref name="scale"/>, as binary64 (through the round-to-odd SDIDI, as every exact
    /// value reaches the float lane).</summary>
    public double ToDouble(int scale) => ToDec(scale).ToDouble();
}
