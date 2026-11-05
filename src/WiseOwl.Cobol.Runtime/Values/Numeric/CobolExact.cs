// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ AN EXACT NATIVE VALUE, WHATEVER CARRIED IT (kb/Work PB4111, PB4568, PB4575, PB4592; docs/CONFORMANCE.md DOC-A.1-123): an exact
/// wide value at a scale (a scaled <see cref="Int128"/> value, an unsigned 16-byte container, an expression of 39 to 77 digits on
/// <see cref="CobolWide"/>), an SDIDI value (<c>significand × 10^exponent</c>, read exactly at its own exponent), or — only where the
/// 256-bit form fails — an arbitrary-precision integer at a scale (<see cref="CobolBig"/>). A binary64 reaches here as its exact
/// expansion (<see cref="FromBinary64"/>, a float ITEM's content) or through its shortest round-trip decimal (an engine approximation).
/// Two consumers take it:
/// <list type="bullet">
///   <item>the bodies that evaluate a function under NATIVE arithmetic on the SDIDI lane (<c>CobolIntrinsics.*Exact</c>), which form
///         the value of the function's equivalent arithmetic expression from these exact values and round it ONCE into the
///         returned-value carrier;</item>
///   <item>⛔ the native ARITHMETIC EXPRESSION in which an exact intermediate meets an SDIDI-carried operand (a floating-point
///         literal, a power, a function's SDIDI value; kb/Work PB4575) or leaves the 77 digits of the wide form (kb/Work PB4729). The
///         wide value's scale is the compiler's and the SDIDI value's is a run-time fact, so their sum, difference or product has a
///         scale only the run time knows: it is THIS value (<see cref="Add"/>, <see cref="Sub"/>, <see cref="Mul"/>, <see cref="Negate"/>,
///         <see cref="DivTruncated"/>, <see cref="Pow"/>), and the compiler carries it as <c>NumX.Exact</c>, settled like a nested
///         product: rounded ONCE into a fixed-point resultant (<see cref="ToUnscaled"/>), compared exactly by a relation
///         (<see cref="Compare"/>), lowered to the SDIDI by round-to-odd where a consumer has no exact form (<see cref="Image"/>).</item>
/// </list>
/// <para>WHY (ISO/IEC 1989:2023 §8.8.1.3: native arithmetic is "an implementor-defined method of evaluating an arithmetic
/// expression, an arithmetic statement, … and all integer and numeric functions"; §15.4.1: "When native arithmetic is in effect and
/// an equivalent arithmetic expression is specified, the value returned is an implementor-defined approximation of the value of
/// that expression"). For that latitude CLAUDE.md rule 1 makes GnuCOBOL the answer, and GnuCOBOL 3.2.0 evaluates these from the
/// exact values (<c>cob_decimal</c>, an arbitrary-precision integer at a scale) at every size. The SDIDI took each wide value as its
/// 34-digit image and rounded every step: <c>VARIANCE (P, P - 1)</c> of a 39-digit P answered 10^10 where the value is 0.25, and
/// <c>(P*1.0E0) ** 2 / P - P</c> left the 34-digit residue of P² (99001 for 0).</para>
/// <para>⛔ THREE FORMS, TRIED IN ORDER, AND THE THIRD ONLY WHERE THE FIRST FAILS (the owner's decision of 2026-11-04, lifting the
/// arbitrary-precision ban for this one fallback; DESIGN §1.2 invariant 2). The work is tried on the 256-bit form
/// (<see cref="TryAt"/> and the <c>Try…</c> operations of <see cref="CobolWide"/>) and, where a step leaves its range, formed
/// exactly by <see cref="CobolBig"/>; a result that fits the 256-bit form again returns to it, so the fast path has no new
/// code. The published bound is the SDIDI's range: <c>|value| &lt; 10^6145</c> (EC-SIZE-OVERFLOW past it, ISO §14.7.5 5)). A
/// comparison never needs more: <see cref="Compare"/> is exact for every pair.</para>
/// <para>⛔ IT CARRIES ITS PRECISION (numeric design D2, kb/Work PB3538): the fraction digits its operands carry, which a nested
/// quotient above it reads (<see cref="ImageWithPrecision"/>) and which is NOT its scale when an operand is a nested quotient held
/// with guard digits. The operations carry it by the rules <see cref="CobolDecWithPrecision"/> applies: the larger for a sum or
/// difference, the sum for a product, the same for a negation.</para>
/// </summary>
public readonly struct CobolExact
{
    private readonly CobolWide _wide;
    private readonly int _scale;
    private readonly CobolDec _dec;
    private readonly bool _sdidi;
    private readonly CobolBig _big;
    private readonly bool _isBig;
    private readonly int _precision;
    private readonly double _approximation;
    private readonly bool _isApproximation;

    private CobolExact(CobolWide wide, int scale, int precision) => (_wide, _scale, _precision) = (wide, scale, precision);

    private CobolExact(CobolDec dec, int precision) => (_dec, _sdidi, _precision) = (dec, true, precision);

    private CobolExact(CobolDec dec, int precision, double approximation) =>
        (_dec, _sdidi, _precision, _approximation, _isApproximation) = (dec, true, precision, approximation, true);

    private CobolExact(CobolBig big, int precision) => (_big, _isBig, _precision) = (big, true, precision);

    /// <summary>A result of the arbitrary-precision form on the 256-bit form again when its integer fits it: the third form holds only
    /// what the first cannot.</summary>
    private static CobolExact Of(CobolBig big, int precision) =>
        big.TryToWide(out CobolWide wide) ? new(wide, big.Scale, precision) : new(big, precision);

    /// <summary>This value on the arbitrary-precision form, at its own scale.</summary>
    private CobolBig AsBig() => _isBig ? _big : _sdidi ? CobolBig.FromDec(_dec) : CobolBig.From(_wide, _scale);

    /// <summary>A scaled <see cref="Int128"/> value (the native lane's exact carrier) at its scale; <paramref name="precision"/> the
    /// fraction digits its operands carry when they are not the scale (a nested quotient's guard digits are not among them).</summary>
    public static CobolExact From(Int128 unscaled, int scale, int? precision = null) =>
        new(CobolWide.From(unscaled), scale, precision ?? scale);

    /// <summary>An unsigned 16-byte container's value at its scale. A distinct NAME, not an overload of <see cref="From"/>: a
    /// <c>ulong</c> converts implicitly to both <see cref="Int128"/> and <see cref="UInt128"/> (CS0121).</summary>
    public static CobolExact FromUnsigned(UInt128 unscaled, int scale) => new(CobolWide.FromUnsigned(unscaled), scale, scale);

    /// <summary>An exact wide intermediate (an expression of 39 to 77 digits) at its compile-time scale, with its precision as
    /// <see cref="From"/>.</summary>
    public static CobolExact FromWide(CobolWide value, int scale, int? precision = null) => new(value, scale, precision ?? scale);

    /// <summary>An SDIDI value exactly: its significand at its own exponent, nothing rounded; its precision the compiler's when it
    /// knows one, else the fraction digits the value is held at (<see cref="CobolDecWithPrecision.Own"/>).</summary>
    public static CobolExact FromDec(CobolDec value, int? precision = null) => new(value, precision ?? CobolDec.FractionDigits(value));

    /// <summary>An SDIDI value whose precision only the run time knows, in the spelling that carries it (kb/Work PB3538 R3-1).</summary>
    public static CobolExact FromDec(CobolDecWithPrecision value) => new(value.Value, value.Precision);

    /// <summary>⛔ A FLOAT ITEM'S CONTENT, EXACTLY (kb/Work PB4730): the binary64's own value <c>m × 2^e</c> written out in full — 0.1 is
    /// its 55-place expansion, DP = 2.5E+40 the 41-digit integer 25000000000000000155002161260194579873792 — not the SDIDI's 34-digit
    /// rounding of it (§8.8.1.5.1 leaves that conversion to the implementor). MAX, MIN and an odd MEDIAN return "the content of the
    /// argument" (§15.59.4 1), §15.63.4 1), §15.61.1), and a list that mixes a float item with a fixed-point one selects among exact
    /// values. A NaN is the IEC 60559 invalid-operation state and an infinity exceeds the range, as <see cref="CobolDec.FromBinary64"/>
    /// reports them.</summary>
    public static CobolExact FromBinary64(double value)
    {
        CobolDec.RejectNonFinite(value);
        if (value == 0) return From(0, 0);
        var (negative, mantissa, exponent) = CobolBig.SplitBinary64(value);
        // The 256-bit form first: m × 2^e is the integer m << e (mantissa below 2^53, so e up to 203), m × 5^k at scale k for e = -k
        // while the product fits, and only a binary64 whose expansion is wider (a tiny magnitude: k past 87) is formed on the third form.
        if (exponent >= 0 && exponent <= 203)
        {
            UInt128 low = (UInt128)mantissa, high = 0;
            if (exponent >= 128) { high = low << (exponent - 128); low = 0; }
            else if (exponent > 0) { high = low >> (128 - exponent); low <<= exponent; }
            return new(CobolWide.FromParts(negative, high, low), 0, 0);
        }
        if (exponent < 0 && CobolWide.TryPow(CobolWide.From(5), -(long)exponent, out CobolWide power)
            && CobolWide.TryMul(CobolWide.FromUnsigned((UInt128)mantissa), power, out CobolWide product))
            return new(negative ? CobolWide.Negate(product) : product, -exponent, -exponent);
        CobolBig big = CobolBig.FromBinary64(value);
        return Of(big, Math.Max(0, big.Scale));
    }

    /// <summary>⛔ AN ENGINE'S APPROXIMATION ON THE EXACT LANE (kb/Work PB4622, PB4730): a function's binary64 returned value (EXP10 (-6)) beside
    /// a float item in a selection list. Its value is its shortest round-trip decimal (<see cref="CobolDec.FromApproximation"/>, the value a
    /// fixed-point receiver lands, D16), and the exact value REMEMBERS the binary64 it came from, so a binary64 receiver lands that double
    /// and not the truncation of its decimal (0.1 truncated sits one binary64 below the double that spelled it). Only a selection returns it
    /// whole; every operation forms a new value without the memory.</summary>
    public static CobolExact FromApproximation(double value)
    {
        CobolDec dec = CobolDec.FromApproximation(value);
        return new(dec, CobolDec.FractionDigits(dec), value);
    }

    /// <summary>The binary64 this value is the approximation of, when it is one (<see cref="FromApproximation"/>).</summary>
    internal bool TryApproximation(out double approximation)
    {
        approximation = _approximation;
        return _isApproximation;
    }

    /// <summary>The exact magnitude's binary64-rounding questions (<see cref="FloatResultant.FromExact"/>), on the arbitrary-precision form.</summary>
    internal CobolBig.Binary64Magnitude BinaryMagnitude() => AsBig().Magnitude();

    /// <summary>The algebraic sign: −1, 0 or +1.</summary>
    internal int Sign => _isBig ? _big.Sign : _sdidi ? Int128.Sign(_dec.Sig) : _wide.Sign;

    /// <summary>The scale this value is exact at (negative for an integer with trailing zeros: an SDIDI's positive exponent).</summary>
    internal int Scale => _isBig ? _big.Scale : _sdidi ? -_dec.Exp : _scale;

    /// <summary>The value as the SDIDI: an SDIDI as itself, a wide or arbitrary-precision value lowered by round-to-odd
    /// (<see cref="CobolWide.ToDec"/>), so that whatever rounds it next, in any mode, sees the exact value on the same side of every
    /// boundary: the lowering of an exact value a consumer with no exact form takes (<c>NumericRenderer.LowerWide</c>) and the operand
    /// of an SDIDI body.</summary>
    public CobolDec Image => _isBig ? _big.ToDec(CobolRounding.Truncation, jam: true) : _sdidi ? _dec : _wide.ToDec(_scale);

    /// <summary><see cref="Image"/> with the precision this value carries: the dividend or divisor of a nested native quotient
    /// (<c>CobolDecWithPrecision.DivTruncatedToOdd</c>), whose working scale is read from it.</summary>
    public CobolDecWithPrecision ImageWithPrecision => new(Image, _precision);

    /// <summary>The value as binary64, through <see cref="Image"/> (as every exact value reaches the float lane).</summary>
    public double ToDouble() => Image.ToDouble();

    /// <summary>⛔ THE NATIVE SUM (kb/Work PB4575, PB4592): exact on the wide form at the operands' common scale (the larger of the scales
    /// each is exact at, <see cref="TryAt"/>), else exact on the arbitrary-precision form (<see cref="CobolBig.Add"/>) — never the
    /// 34-digit images, whose cancellation left their rounding residue (<c>(P + 1.0E-50) - P</c> of a 39-digit P stored -999.0000 for
    /// 0.0000); the larger precision.</summary>
    public static CobolExact Add(CobolExact a, CobolExact b) => Additive(a, b, subtract: false);

    /// <summary>The native difference, as <see cref="Add"/>.</summary>
    public static CobolExact Sub(CobolExact a, CobolExact b) => Additive(a, b, subtract: true);

    private static CobolExact Additive(CobolExact a, CobolExact b, bool subtract)
    {
        int precision = Math.Max(a._precision, b._precision);
        int scale = Math.Max(a.Scale, b.Scale);
        if (a.TryAt(scale, out CobolWide left) && b.TryAt(scale, out CobolWide right)
            && CobolWide.TryAdd(left, subtract ? CobolWide.Negate(right) : right, out CobolWide sum))
            return new(sum, scale, precision);
        return Of(CobolBig.Add(a.AsBig(), b.AsBig(), subtract), precision);
    }

    /// <summary>⛔ THE NATIVE PRODUCT (kb/Work PB4575, PB4587, PB4592): exact on the wide form at the sum of the operands' own scales (an
    /// SDIDI value with a positive exponent at its negative one, so <c>P * 1.0E40</c> is the 40-digit <c>P × 10</c> at scale −39), else
    /// the exact product on the arbitrary-precision form, so <c>(A * B) ** 2</c> and <c>(A * B) * (A * B)</c> are one value; the sum of
    /// the precisions.</summary>
    public static CobolExact Mul(CobolExact a, CobolExact b)
    {
        int precision = a._precision + b._precision;
        if (a.TryAt(a.Scale, out CobolWide left) && b.TryAt(b.Scale, out CobolWide right)
            && CobolWide.TryMul(left, right, out CobolWide product))
            return new(product, a.Scale + b.Scale, precision);
        return Of(CobolBig.Mul(a.AsBig(), b.AsBig()), precision);
    }

    /// <summary>⛔ THE NESTED NATIVE QUOTIENT (kb/Work PB3538, PB4575, PB4702; DOC-A.1-123): the quotient TRUNCATED at the working scale
    /// <see cref="CobolNum.NestedQuotientScale"/> derives from <paramref name="receiverScale"/> and the operands' larger precision,
    /// the one rule of every native lane: exact on the wide form while the dividend's alignment fits it
    /// (<see cref="CobolWide.DivTruncated"/>), else exact on the arbitrary-precision form (<see cref="CobolBig.DivTruncated"/>), never
    /// the quotient of the operands' 34-digit images. It carries that precision, not the working scale. A zero divisor raises
    /// <see cref="CobolSizeError.ZeroDivide"/> (ISO §14.7.5 case 2).</summary>
    public static CobolExact DivTruncated(CobolExact a, CobolExact b, int receiverScale)
    {
        int precision = Math.Max(a._precision, b._precision);
        return TruncatedQuotient(a, b, CobolNum.NestedQuotientScale(receiverScale, precision), precision);
    }

    /// <summary>⛔ THE SUBSIDIARY QUOTIENT OF A NATIVE <c>DIVIDE ... GIVING ... REMAINDER</c> OVER EXACT OPERANDS (ISO §14.9.12.4 6) c),
    /// kb/Work PB4640, PB4702; DOC-A.1-70, DOC-A.1-123): the exact quotient TRUNCATED toward zero at <paramref name="scale"/>,
    /// identifier-3's decimal point location, with EVERY integer digit — "derived from the quotient by truncation of digits at the least
    /// significant end". It is formed from the operands' exact values, not from the final transfer's 34-digit quotient
    /// (<see cref="DivToOdd"/>): a quotient past 34 digits (a size error at identifier-3, the remainder still stored with checking off,
    /// GnuCOBOL 3.2's <c>cob_div_quotient</c> / <c>cob_div_remainder</c> by CLAUDE.md rule 1) lost its low-order digits there, so
    /// <c>dividend − subsidiary × divisor</c> was not the remainder. The truncation is the nested quotient's
    /// (<see cref="DivTruncated"/>), at the receiver's own scale and not the working scale derived from it, and the precision is that
    /// scale. A zero divisor raises <see cref="CobolSizeError.ZeroDivide"/> (ISO §14.7.5 case 2).</summary>
    public static CobolExact SubsidiaryQuotient(CobolExact a, CobolExact b, int scale) =>
        TruncatedQuotient(a, b, scale, scale);

    /// <summary>The quotient truncated toward zero at <paramref name="w"/> fraction digits, carrying <paramref name="precision"/>: exact
    /// on the wide form while the dividend's alignment fits it (<see cref="CobolWide.DivTruncated"/>), else exact on the
    /// arbitrary-precision form. The ONE truncated quotient of the nested quotient and the subsidiary quotient.</summary>
    private static CobolExact TruncatedQuotient(CobolExact a, CobolExact b, int w, int precision)
    {
        if (b.Sign == 0) throw CobolSizeError.ZeroDivide();
        if (a.TryAt(a.Scale, out CobolWide num) && b.TryAt(b.Scale, out CobolWide den))
        {
            int up = b.Scale + w - a.Scale;   // the dividend's alignment (CobolWide.DivTruncated), which must stay on the form
            if (up <= 0 || CobolWide.TryUp(num, up, out _))
                return new(CobolWide.DivTruncated(num, a.Scale, den, b.Scale, w), w, precision);
        }
        return Of(CobolBig.DivTruncated(a.AsBig(), b.AsBig(), w), precision);
    }

    /// <summary>⛔ THE FINAL-TRANSFER NATIVE QUOTIENT OF EXACT OPERANDS (kb/Work PB4599; DOC-A.1-123): the EXACT quotient of the two
    /// values reduced to the SDIDI's 34 digits ONCE, by ROUND-TO-ODD (<see cref="CobolDec.DivToOdd"/>'s rule), so the receiver's one
    /// rounding equals rounding the exact quotient. Every operand is an exact value, so both are divided as the integers they are at
    /// their own scales, whatever the divisor's digits (<see cref="Quotient"/>): the operands' images are never divided. Before it the
    /// dividend entered the SDIDI as its 34-digit image and <c>A * A * A / (A * A)</c> of a 15-digit A stored 999999999999998.
    /// <para><paramref name="intermediate"/> is the program's INTERMEDIATE ROUNDING mode where the statement reaches this quotient through
    /// the SDIDI lane (a floating-point literal beside the exact operand, CONFORMANCE.md DOC-A.1-82): PROHIBITED screens the quotient
    /// as every SDIDI intermediate is, and a quotient not exact in 34 digits raises EC-SIZE-TRUNCATION (ISO §11.9.11.2 r3d); any other
    /// mode leaves the round-to-odd value, which no later rounding sees as other than the exact quotient. The plain native lane passes
    /// <see cref="CobolRounding.Truncation"/>: it has no SDIDI intermediate to screen. A final transfer into a fixed-point
    /// resultant that has no such intermediate lands from <see cref="QuotientAtScale"/> instead.</para>
    /// A zero divisor raises <see cref="CobolSizeError.ZeroDivide"/> (ISO §14.7.5 case 2).</summary>
    public static CobolDec DivToOdd(CobolExact a, CobolExact b, CobolRounding intermediate) =>
        Quotient(a, b, intermediate, jam: intermediate != CobolRounding.Prohibited);

    /// <summary>The exact quotient of two exact values reduced to the SDIDI's 34 digits ONCE: ROUND-TO-ODD with <paramref name="jam"/>, else
    /// in <paramref name="mode"/> (MEAN's, MEDIAN's and MIDRANGE's single division of an exact sum). On the wide form while both operands
    /// are on it (<see cref="CobolWide.QuotientDec(int, CobolWide, int, CobolRounding, bool)"/>), else on the arbitrary-precision form.</summary>
    internal static CobolDec Quotient(CobolExact a, CobolExact b, CobolRounding mode, bool jam)
    {
        if (b.Sign == 0) throw CobolSizeError.ZeroDivide();
        return !a._isBig && !b._isBig
            ? a.Significand.QuotientDec(a.Scale, b.Significand, b.Scale, mode, jam)
            : CobolBig.QuotientDec(a.AsBig(), b.AsBig(), mode, jam);
    }

    /// <summary>⛔ THE FINAL TRANSFER OF A NATIVE QUOTIENT INTO A FIXED-POINT RESULTANT, LANDED FROM THE EXACT QUOTIENT (kb/Work PB4701):
    /// <c>a / b</c> rounded ONCE at <paramref name="toScale"/> in the receiver's <paramref name="mode"/> from the true remainder
    /// (PROHIBITED raises on an inexact quotient, §14.7.4.3 r7) and landed as <see cref="ToUnscaled"/> lands a value. A quotient the
    /// receiver cannot hold keeps the exact quotient's own low-order digits — GnuCOBOL's <c>cob_decimal_div</c> then
    /// <c>cob_decimal_get_field</c> — and not the low-order digits of the 34-digit round-to-odd image (<c>REMD</c> stored
    /// 000003000100000000 for 000003000000000000). A zero divisor raises <see cref="CobolSizeError.ZeroDivide"/> (ISO §14.7.5 case 2).</summary>
    public static Int128 QuotientAtScale(CobolExact a, CobolExact b, int toScale, CobolRounding mode, bool checkedTransfer)
    {
        if (b.Sign == 0) throw CobolSizeError.ZeroDivide();
        return TryQuotientAtScale(a, b, toScale, mode, checkedTransfer, out Int128 landed)
            ? landed
            : CobolBig.QuotientAtScale(a.AsBig(), b.AsBig(), toScale, mode, checkedTransfer);
    }

    /// <summary>The final-transfer quotient on the 256-bit form: both operands on it, the dividend's alignment to the receiver's scale
    /// on it, and the truncated quotient and the divisor within 128 bits (so the remainder is, and <see cref="CobolNum.BumpsMagnitude{T}"/>
    /// rounds it once). Anything wider is the third form's, which is exact at any size.</summary>
    private static bool TryQuotientAtScale(CobolExact a, CobolExact b, int toScale, CobolRounding mode, bool checkedTransfer, out Int128 landed)
    {
        landed = 0;
        if (!a.TryAt(a.Scale, out CobolWide numerator) || !b.TryAt(b.Scale, out CobolWide divisor)) return false;
        long up = (long)b.Scale + toScale - a.Scale;
        if (up > 0 ? up > 76 || !CobolWide.TryUp(numerator, (int)up, out numerator)
            : up < 0 && (up < -76 || !CobolWide.TryUp(divisor, (int)-up, out divisor))) return false;
        CobolWide quotient = CobolWide.DivRem(numerator, divisor, out CobolWide remainder);
        var (_, quotientHigh, quotientLow) = quotient.Parts;
        bool negative = numerator.Sign != 0 && (numerator.Sign < 0) != (divisor.Sign < 0);
        var (_, divisorHigh, divisorLow) = divisor.Parts;
        var (_, _, remainderLow) = remainder.Parts;   // |remainder| < |divisor| < 2^128, so its high half is zero
        if (quotientHigh != 0 || divisorHigh != 0) return false;
        if (remainder.Sign != 0 && CobolNum.BumpsMagnitude(negative, !UInt128.IsEvenInteger(quotientLow), remainderLow, divisorLow, mode))
        {
            if (quotientLow == UInt128.MaxValue) return false;
            quotientLow++;
        }
        landed = CobolBig.Land(quotientLow, negative, checkedTransfer);
        return true;
    }

    /// <summary>The value's integer at its own <see cref="Scale"/>: the wide form, or the SDIDI's significand.</summary>
    private CobolWide Significand => _sdidi ? CobolWide.From(_dec.Sig) : _wide;

    /// <summary>The negation, on the value's own carrier; the same precision.</summary>
    public static CobolExact Negate(CobolExact a) =>
        a._isBig ? new(CobolBig.Negate(a._big), a._precision)
        : a._sdidi ? new(CobolDec.Negate(a._dec), a._precision)
        : new(CobolWide.Negate(a._wide), a._scale, a._precision);

    /// <summary>⛔ THE NATIVE POWER TO AN INTEGER EXPONENT (kb/Work PB2617, PB4575, PB4587): the EXACT power, GnuCOBOL's
    /// <c>cob_decimal_pow</c> (<c>mpz_pow_ui</c> with scale × n, CLAUDE.md rule 1), for a positive exponent — on the wide form while it
    /// fits (<see cref="CobolWide.TryPow"/>), else on the arbitrary-precision form (<see cref="CobolBig.Pow"/>) — and reduced to the
    /// SDIDI's 34 digits ONCE, by round-to-odd, wherever a consumer lowers it (<see cref="Image"/>): <c>1.01 ** 119</c> into
    /// <c>PIC 9V9(30)</c> is …851 as GnuCOBOL and the exact value give, where the chain of round-to-odd products it replaces drifted to
    /// …852. A zero exponent is 1 and a negative one is §8.8.1.5.4 r3's reciprocal <c>1 / (b ** |e|)</c>, one rounding of the exact
    /// quotient. The §8.8.1.2 rule-6 screen is <see cref="CobolIntrinsics.CheckPowRule6"/>'s, written once. The precision is the base's
    /// times a non-negative exponent (the multiple, numeric design D2), and the reciprocal's own fraction digits.</summary>
    public static CobolExact Pow(CobolExact b, Int128 e)
    {
        CobolIntrinsics.CheckPowRule6(b.Sign, (double)e);
        if (e == 0) return From(1, 0);
        Int128 n = Int128.Abs(e);
        int precision = (int)Int128.Clamp(b._precision * Int128.Clamp(e, 0, int.MaxValue), int.MinValue, int.MaxValue);
        // An exponent past 2^40 is one of its own parity: every base but 0 and ±1 is out of the range at it, and those three
        // depend on the parity alone.
        long count = n < (Int128)(1L << 40) ? (long)n : (1L << 41) + (long)(n & 1);
        CobolExact power = PositivePower(b, count, precision);
        if (e > 0) return power;
        CobolDec reciprocal = Quotient(From(1, 0), power, CobolRounding.Truncation, jam: true);
        return new(reciprocal, CobolDec.FractionDigits(reciprocal));
    }

    private static CobolExact PositivePower(CobolExact b, long n, int precision)
    {
        if (n <= int.MaxValue && b.TryAt(b.Scale, out CobolWide wide))
        {
            long scale = (long)b.Scale * n;
            if (scale is >= int.MinValue and <= int.MaxValue && CobolWide.TryPow(wide, n, out CobolWide power))
                return new(power, (int)scale, precision);
        }
        return Of(CobolBig.Pow(b.AsBig(), n), precision);
    }

    /// <summary>This value with the fraction digits <paramref name="precision"/> (an integer function's value is held at 0).</summary>
    internal CobolExact WithPrecision(int precision) =>
        _isBig ? new(_big, precision) : _isApproximation ? new(_dec, precision, _approximation) : _sdidi ? new(_dec, precision) : new(_wide, _scale, precision);

    /// <summary>⛔ THE FINAL TRANSFER (kb/Work PB1900, PB4575, PB4592): this value moved to the receiver's <paramref name="toScale"/> and
    /// rounded ONCE with the receiver's <paramref name="mode"/>, through <see cref="CobolWide.ToUnscaled"/> for the wide form (an SDIDI
    /// value as its significand at its own exponent) and <see cref="CobolBig.ToUnscaled"/> for the arbitrary-precision one, so a
    /// checked transfer past the carrier is EC-SIZE-TRUNCATION and PROHIBITED raises on an inexact one, and an unchecked one keeps the
    /// value's own low-order digits a store can use, exactly as the wide value's: <c>COMPUTE R = P*P / 7</c> stores 571571143 as
    /// GnuCOBOL does.</summary>
    public Int128 ToUnscaled(int toScale, CobolRounding mode, bool checkedTransfer) =>
        _isBig ? _big.ToUnscaled(toScale, mode, checkedTransfer)
        : _sdidi ? CobolWide.From(_dec.Sig).ToUnscaled(-_dec.Exp, toScale, mode, checkedTransfer)
        : _wide.ToUnscaled(_scale, toScale, mode, checkedTransfer);

    /// <summary>The value on the wide form at a scale of at least 0 (an integer the product of a positive exponent left at a
    /// negative scale written out there), or false for an SDIDI value, an arbitrary-precision one, or one that leaves the form when
    /// written out: the one question the consumers that keep every digit (<c>FunctionValueField.OfExact</c>,
    /// <c>CobolIntrinsics.IntegerArgExact</c>) ask before they take their wide arm, or their SDIDI arm over <see cref="Image"/>.</summary>
    internal bool TryWide(out CobolWide wide, out int scale)
    {
        (wide, scale) = (_wide, Math.Max(0, _scale));
        return !_sdidi && !_isBig && (_scale >= 0 || CobolWide.TryUp(_wide, -_scale, out wide));
    }

    /// <summary>The unscaled value at <paramref name="scale"/> (at least <see cref="Scale"/>) on the wide form, only zeros appended;
    /// false when it is past the 256-bit range (an arbitrary-precision value always is) or holds a digit finer than
    /// <paramref name="scale"/>.
    /// <para>⛔ AN SDIDI VALUE ALIGNS FROM ITS OWN EXPONENT, the <see cref="Scale"/> the common scale is the largest of: its significand
    /// times <c>10^(scale + Exp)</c>. A NEGATIVE common scale is legitimate (every argument an SDIDI with a positive exponent:
    /// <c>SUM (1.0E40, 1.0E5, -1.0E40)</c> aligns at scale −4 as the integers 10^36, 10 and −10^36). <see cref="CobolWide.TryFrom"/>
    /// is NOT this alignment: it writes a positive exponent out at scale 0, so a guard against its scale refused every such list and
    /// sent the body to the 34-digit images (the review finding R1-1 of kb/Work PB4568).</para></summary>
    internal bool TryAt(int scale, out CobolWide aligned)
    {
        aligned = default;
        int own = Scale;
        return !_isBig && scale >= own && CobolWide.TryUp(Significand, scale - own, out aligned);
    }

    /// <summary>⛔ EVERY VALUE OF <paramref name="xs"/> ON THE WIDE FORM AT THEIR COMMON SCALE (the largest of their scales, at which
    /// each is exact) — or false, and the caller forms the work on the arbitrary-precision form (<see cref="Add"/>) or, for the bodies
    /// that have no such form yet, on the SDIDI images.</summary>
    internal static bool TryAlign(ReadOnlySpan<CobolExact> xs, out CobolWide[] aligned, out int scale)
    {
        scale = int.MinValue;
        foreach (CobolExact x in xs) scale = Math.Max(scale, x.Scale);
        aligned = new CobolWide[xs.Length];
        for (int i = 0; i < xs.Length; i++)
            if (!xs[i].TryAt(scale, out aligned[i])) return false;
        return true;
    }

    /// <summary>⛔ THE ALGEBRAIC COMPARISON OF TWO EXACT VALUES (−1/0/+1; ISO §8.8.4.2.4: "a comparison is made with respect to the
    /// algebraic value of the operands"), never of an image, and exact for every pair: two SDIDI values by their own compare, two wide
    /// values on the wide form at their own scales (<see cref="CobolWide.Compare"/>, which never aligns past the range), a mixed
    /// pair with the SDIDI value written on the wide form at its own exponent — or, when its integer is past the 256-bit range, on the
    /// arbitrary-precision form (a wide value at a negative scale can be larger still) — and any pair with an arbitrary-precision value on that
    /// form (<see cref="CobolBig.Compare"/>). The ordering every selection over mixed carriers
    /// takes (<c>CobolIntrinsics.OrdMaxExact</c> and its siblings, and <see cref="FunctionValueField"/>'s temporaries).</summary>
    public static int Compare(CobolExact a, CobolExact b)
    {
        if (a._isBig || b._isBig) return CobolBig.Compare(a.AsBig(), b.AsBig());
        if (a._sdidi && b._sdidi) return CobolDec.Compare(a._dec, b._dec);
        if (!a._sdidi && !b._sdidi) return CobolWide.Compare(a._wide, a._scale, b._wide, b._scale);
        // A mixed pair: the SDIDI value written on the wide form at its own exponent. When that leaves the 256-bit range the pair
        // is NOT ordered by its sign (a wide value at a NEGATIVE scale, PIC 9(20)P(10) cubed, can be larger still): it goes to the
        // arbitrary-precision form (review finding R2-1 of kb/Work PB4750).
        bool aWide = !a._sdidi;
        CobolExact dec = aWide ? b : a, wide = aWide ? a : b;
        if (!CobolWide.TryFrom(dec._dec, out CobolWide w, out int s)) return CobolBig.Compare(a.AsBig(), b.AsBig());
        int order = CobolWide.Compare(wide._wide, wide._scale, w, s);
        return aWide ? order : -order;
    }

    /// <summary>A COMPUTED value (a sum, a difference, a remainder) rounded ONCE to the SDIDI's 34 digits in <paramref name="mode"/>, where
    /// <see cref="Image"/>'s round-to-odd is the lowering of an operand that something else rounds next: an SDIDI as itself.</summary>
    internal CobolDec ToDecRounded(CobolRounding mode) =>
        _isBig ? _big.ToDec(mode, jam: false) : _sdidi ? _dec : _wide.ToDecRounded(_scale, mode);

    /// <summary>The truncated remainder (<c>REM</c>) or the floored modulus (<c>MOD</c>) of two exact values past the wide form, exact
    /// (<see cref="CobolBig.Remainder"/>) at their larger scale; the caller owns the zero-divisor rule.</summary>
    internal static CobolExact Remainder(CobolExact a, CobolExact b, bool floored) =>
        Of(CobolBig.Remainder(a.AsBig(), b.AsBig(), floored), 0);

    /// <summary>The integer part of an arbitrary-precision value (truncation toward zero, or the floor) held at precision 0, and whether
    /// a fraction was dropped (§15.3 type 6's integrality test).</summary>
    internal CobolExact IntegerPartBig(bool floor, out bool hadFraction) =>
        Of(AsBig().IntegerPart(floor, out hadFraction), 0);

    /// <summary>FRACTION-PART of an arbitrary-precision value: <c>argument − INTEGER-PART (argument)</c>, at its own scale.</summary>
    internal CobolExact FractionPartBig() => Of(AsBig().FractionPart(), _precision);

    /// <summary>The digits of the unscaled magnitude, whichever form holds the value (no sign, no scale): what a temporary that shows every
    /// digit of an exact value writes out (<c>CobolNum.ReturnedValueProfile</c>, kb/Work PB4750). Text, so the raw arbitrary-precision type
    /// stays inside <see cref="CobolBig"/>.</summary>
    internal string MagnitudeDigits() =>
        _isBig ? _big.MagnitudeDigits() : _sdidi ? CobolDec.UAbs(_dec.Sig).ToString(System.Globalization.CultureInfo.InvariantCulture) : _wide.MagnitudeDigits();

    /// <summary>Is this an SDIDI (<see cref="Image"/> is the value itself)?</summary>
    internal bool IsDec => _sdidi;

    /// <summary>Is this the arbitrary-precision form? The consumers with a wide arm and an SDIDI arm ask before they choose.</summary>
    internal bool IsBig => _isBig;
}
