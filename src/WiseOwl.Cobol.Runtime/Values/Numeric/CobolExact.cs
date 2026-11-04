// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ AN EXACT NATIVE VALUE, WHATEVER CARRIED IT (kb/Work PB4111, PB4568, PB4575; docs/CONFORMANCE.md DOC-A.1-123): either an exact
/// wide value at a scale (a scaled <see cref="Int128"/> value, an unsigned 16-byte container, an expression of 39 to 77 digits on
/// <see cref="CobolWide"/>) or an SDIDI value (<c>significand × 10^exponent</c>, read exactly at its own exponent; a binary64 reaches
/// here through its §8.8.1.5.1 conversion). Two consumers take it:
/// <list type="bullet">
///   <item>the bodies that evaluate a function under NATIVE arithmetic on the SDIDI lane (<c>CobolIntrinsics.*Exact</c>), which form
///         the value of the function's equivalent arithmetic expression from these exact values on the 256-bit wide form and round
///         it ONCE into the returned-value carrier;</item>
///   <item>⛔ the native ARITHMETIC EXPRESSION in which an exact wide intermediate meets an SDIDI-carried operand (a floating-point
///         literal, a power, a function's SDIDI value; kb/Work PB4575). The wide value's scale is the compiler's and the SDIDI
///         value's is a run-time fact, so their sum, difference or product has a scale only the run time knows: it is THIS value
///         (<see cref="Add"/>, <see cref="Sub"/>, <see cref="Mul"/>, <see cref="Negate"/>, <see cref="DivTruncated"/>), exact on the wide form at that scale,
///         and the compiler carries it as <c>NumX.Exact</c>, settled like a nested product: rounded ONCE into a fixed-point
///         resultant (<see cref="ToUnscaled"/>), compared exactly by a relation (<see cref="Compare"/>), lowered to the SDIDI by
///         round-to-odd where a consumer has no exact form (<see cref="Image"/>). Before it, the wide value entered the SDIDI as its
///         34-digit image and <c>(P + 2.0E0) - P</c> of a 39-digit P was 0.</item>
/// </list>
/// <para>WHY (ISO/IEC 1989:2023 §8.8.1.3: native arithmetic is "an implementor-defined method of evaluating an arithmetic
/// expression, an arithmetic statement, … and all integer and numeric functions"; §15.4.1: "When native arithmetic is in effect and
/// an equivalent arithmetic expression is specified, the value returned is an implementor-defined approximation of the value of
/// that expression"). For that latitude CLAUDE.md rule 1 makes GnuCOBOL the answer, and GnuCOBOL 3.2.0 evaluates these from the
/// exact values (<c>cob_decimal</c>). The SDIDI took each wide value as its 34-digit image and rounded every step:
/// <c>VARIANCE (P, P - 1)</c> of a 39-digit P answered 10^10 where the value is 0.25, <c>ORD-MAX (P - 1, P, 2.0E0)</c> answered 1
/// although the compiler's own <c>IF P - 1 &lt; P</c> is true, and <c>IF P - 1 &lt; 2.0E0 + P</c> was false.</para>
/// <para>⛔ BOUNDED BY THE WIDE FORM, AS EVERY EXACT NATIVE INTERMEDIATE IS (DOC-A.1-123). The arithmetic is <see cref="CobolWide"/>'s,
/// never an unbounded integer (DESIGN §1.2 invariant 2 keeps arbitrary-precision integers out of the runtime value model): the work
/// is tried on the 256-bit form (<see cref="TryAlign"/> and the <c>Try…</c> operations) and, where the values cannot be aligned or a
/// step leaves the range (an SDIDI exponent thousands of places from its neighbours, a product past 77 digits), evaluated on the
/// SDIDI operands instead (<see cref="ToDec"/>), the determination DOC-A.1-123 states for an intermediate past 77 digits. A
/// comparison never needs that: <see cref="Compare"/> is exact for every pair.</para>
/// <para>⛔ IT CARRIES ITS PRECISION (numeric design D2, kb/Work PB3538): the fraction digits its operands carry, which a nested
/// quotient above it reads (<see cref="ImageWithPrecision"/>) and which is NOT its scale when an operand is a nested quotient held
/// with guard digits. The operations carry it by the rules <see cref="CobolDecWithPrecision"/> applies: the larger for a sum or
/// difference, the sum for a product, the same for a negation.</para>
/// </summary>
public readonly struct CobolExact
{
    /// <summary>The widest significand the native Dec lane carries EXACTLY: below 10^38 (the compiler's
    /// <c>NumericRenderer.NativeDecExactDigits</c>, <c>CobolDec.AddSigned</c>'s wide scratch).</summary>
    private static readonly Int128 ExactDecLimit = Pow10.AsWide(38);

    private readonly CobolWide _wide;
    private readonly int _scale;
    private readonly CobolDec _dec;
    private readonly bool _sdidi;
    private readonly int _precision;

    private CobolExact(CobolWide wide, int scale, int precision) => (_wide, _scale, _precision) = (wide, scale, precision);

    private CobolExact(CobolDec dec, int precision) => (_dec, _sdidi, _precision) = (dec, true, precision);

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

    /// <summary>The algebraic sign: −1, 0 or +1.</summary>
    internal int Sign => _sdidi ? Int128.Sign(_dec.Sig) : _wide.Sign;

    /// <summary>The scale this value is exact at (negative for an integer with trailing zeros: an SDIDI's positive exponent).</summary>
    internal int Scale => _sdidi ? -_dec.Exp : _scale;

    /// <summary>The value as the SDIDI: an SDIDI as itself, a wide value lowered by round-to-odd (<see cref="CobolWide.ToDec"/>), so
    /// that whatever rounds it next, in any mode, sees the exact value on the same side of every boundary: the lowering of an exact
    /// value a consumer with no exact form takes (<c>NumericRenderer.LowerWide</c>) and the operand of an SDIDI body.</summary>
    public CobolDec Image => _sdidi ? _dec : _wide.ToDec(_scale);

    /// <summary><see cref="Image"/> with the precision this value carries: the dividend or divisor of a nested native quotient
    /// (<c>CobolDecWithPrecision.DivTruncatedToOdd</c>), whose working scale is read from it.</summary>
    public CobolDecWithPrecision ImageWithPrecision => new(Image, _precision);

    /// <summary>The value as binary64, through <see cref="Image"/> (as every exact value reaches the float lane).</summary>
    public double ToDouble() => Image.ToDouble();

    /// <summary>⛔ THE NATIVE SUM (kb/Work PB4575): exact on the wide form at the operands' common scale (the larger of the scales
    /// each is exact at, <see cref="TryAt"/>), else the SDIDI sum of the operands as <see cref="ToDec"/> lifts them (exact to 38
    /// digits, a wider one rounded once in the INTERMEDIATE ROUNDING <paramref name="mode"/>, as §8.8.1.5.2 r1 lifts an operand the
    /// SDIDI cannot hold) at <paramref name="mode"/>; the larger precision.</summary>
    public static CobolExact Add(CobolExact a, CobolExact b, CobolRounding mode) => Additive(a, b, subtract: false, mode);

    /// <summary>The native difference, as <see cref="Add"/>.</summary>
    public static CobolExact Sub(CobolExact a, CobolExact b, CobolRounding mode) => Additive(a, b, subtract: true, mode);

    private static CobolExact Additive(CobolExact a, CobolExact b, bool subtract, CobolRounding mode)
    {
        int precision = Math.Max(a._precision, b._precision);
        int scale = Math.Max(a.Scale, b.Scale);
        if (a.TryAt(scale, out CobolWide left) && b.TryAt(scale, out CobolWide right)
            && CobolWide.TryAdd(left, subtract ? CobolWide.Negate(right) : right, out CobolWide sum))
            return new(sum, scale, precision);
        CobolDec l = a.ToDec(mode), r = b.ToDec(mode);
        return new(subtract ? CobolDec.Sub(l, r, mode) : CobolDec.Add(l, r, mode), precision);
    }

    /// <summary>⛔ THE NATIVE PRODUCT (kb/Work PB4575): exact on the wide form at the sum of the operands' own scales (an SDIDI value
    /// with a positive exponent at its negative one, so <c>P * 1.0E40</c> is the 40-digit <c>P × 10</c> at scale −39), else the SDIDI
    /// product of the operands as <see cref="Add"/> lifts them; the sum of the precisions.</summary>
    public static CobolExact Mul(CobolExact a, CobolExact b, CobolRounding mode)
    {
        int precision = a._precision + b._precision;
        if (a.TryAt(a.Scale, out CobolWide left) && b.TryAt(b.Scale, out CobolWide right)
            && CobolWide.TryMul(left, right, out CobolWide product))
            return new(product, a.Scale + b.Scale, precision);
        return new(CobolDec.Mul(a.ToDec(mode), b.ToDec(mode), mode), precision);
    }

    /// <summary>⛔ THE NESTED NATIVE QUOTIENT (kb/Work PB3538, PB4575; DOC-A.1-123): the quotient TRUNCATED at the working scale
    /// <see cref="CobolNum.NestedQuotientScale"/> derives from <paramref name="receiverScale"/> and the operands' larger precision,
    /// the one rule of every native lane: exact on the wide form while the dividend's alignment fits it
    /// (<see cref="CobolWide.DivTruncated"/>), else held to 34 digits by round-to-odd (<see cref="CobolDec.DivTruncatedToOdd"/>) over
    /// the operands as <see cref="Add"/> lifts them. It carries that precision, not the working scale. A zero divisor raises
    /// <see cref="CobolSizeError.ZeroDivide"/> (ISO §14.7.5 case 2).</summary>
    public static CobolExact DivTruncated(CobolExact a, CobolExact b, int receiverScale, CobolRounding mode)
    {
        int precision = Math.Max(a._precision, b._precision);
        int w = CobolNum.NestedQuotientScale(receiverScale, precision);
        if (b.Sign == 0) throw CobolSizeError.ZeroDivide();
        if (a.TryAt(a.Scale, out CobolWide num) && b.TryAt(b.Scale, out CobolWide den))
        {
            int up = b.Scale + w - a.Scale;   // the dividend's alignment (CobolWide.DivTruncated), which must stay on the form
            if (up <= 0 || CobolWide.TryUp(num, up, out _))
                return new(CobolWide.DivTruncated(num, a.Scale, den, b.Scale, w), w, precision);
        }
        return new(CobolDec.DivTruncatedToOdd(a.ToDec(mode), b.ToDec(mode), w), precision);
    }

    /// <summary>The negation, on the value's own carrier; the same precision.</summary>
    public static CobolExact Negate(CobolExact a) =>
        a._sdidi ? new(CobolDec.Negate(a._dec), a._precision) : new(CobolWide.Negate(a._wide), a._scale, a._precision);

    /// <summary>⛔ THE FINAL TRANSFER (kb/Work PB1900, PB4575): this value moved to the receiver's <paramref name="toScale"/> and
    /// rounded ONCE with the receiver's <paramref name="mode"/>, through <see cref="CobolWide.ToUnscaled"/> for both carriers (an SDIDI
    /// value as its significand at its own exponent), so a checked transfer past the carrier is EC-SIZE-TRUNCATION and PROHIBITED
    /// raises on an inexact one, and an unchecked one keeps the low-order digits a store can use, exactly as the wide value's.</summary>
    public Int128 ToUnscaled(int toScale, CobolRounding mode, bool checkedTransfer) =>
        _sdidi
            ? CobolWide.From(_dec.Sig).ToUnscaled(-_dec.Exp, toScale, mode, checkedTransfer)
            : _wide.ToUnscaled(_scale, toScale, mode, checkedTransfer);

    /// <summary>The value on the wide form at a scale of at least 0 (an integer the product of a positive exponent left at a
    /// negative scale written out there), or false for an SDIDI value or one that leaves the form when written out: the one
    /// question the consumers that keep every digit (<c>FunctionValueField.OfExact</c>, <c>CobolIntrinsics.IntegerArgExact</c>) ask
    /// before they take their wide arm, or their SDIDI arm over <see cref="Image"/>.</summary>
    internal bool TryWide(out CobolWide wide, out int scale)
    {
        (wide, scale) = (_wide, Math.Max(0, _scale));
        return !_sdidi && (_scale >= 0 || CobolWide.TryUp(_wide, -_scale, out wide));
    }

    /// <summary>The unscaled value at <paramref name="scale"/> (at least <see cref="Scale"/>) on the wide form, only zeros appended;
    /// false when it is past the 256-bit range or holds a digit finer than <paramref name="scale"/>.
    /// <para>⛔ AN SDIDI VALUE ALIGNS FROM ITS OWN EXPONENT, the <see cref="Scale"/> the common scale is the largest of: its significand
    /// times <c>10^(scale + Exp)</c>. A NEGATIVE common scale is legitimate (every argument an SDIDI with a positive exponent:
    /// <c>SUM (1.0E40, 1.0E5, -1.0E40)</c> aligns at scale −4 as the integers 10^36, 10 and −10^36). <see cref="CobolWide.TryFrom"/>
    /// is NOT this alignment: it writes a positive exponent out at scale 0, so a guard against its scale refused every such list and
    /// sent the body to the 34-digit images (the review finding R1-1 of kb/Work PB4568).</para></summary>
    internal bool TryAt(int scale, out CobolWide aligned)
    {
        aligned = default;
        int own = Scale;
        return scale >= own && CobolWide.TryUp(_sdidi ? CobolWide.From(_dec.Sig) : _wide, scale - own, out aligned);
    }

    /// <summary>⛔ EVERY VALUE OF <paramref name="xs"/> ON THE WIDE FORM AT THEIR COMMON SCALE (the largest of their scales, at which
    /// each is exact) — or false, and the caller evaluates on the SDIDI images (DOC-A.1-123 past 77 digits).</summary>
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
    /// values on the wide form at their own scales (<see cref="CobolWide.Compare"/>, which never aligns past the range), and a mixed
    /// pair with the SDIDI value written on the wide form at its own exponent — or, when its integer is past the 256-bit range, by its
    /// sign alone, because it is then larger in magnitude than every wide value. The ordering every selection over mixed carriers
    /// takes (<c>CobolIntrinsics.OrdMaxExact</c> and its siblings, and <see cref="FunctionValueField"/>'s temporaries).</summary>
    public static int Compare(CobolExact a, CobolExact b)
    {
        if (a._sdidi && b._sdidi) return CobolDec.Compare(a._dec, b._dec);
        if (!a._sdidi && !b._sdidi) return CobolWide.Compare(a._wide, a._scale, b._wide, b._scale);
        bool aWide = !a._sdidi;
        CobolExact dec = aWide ? b : a, wide = aWide ? a : b;
        int order = CobolWide.TryFrom(dec._dec, out CobolWide w, out int s)
            ? CobolWide.Compare(wide._wide, wide._scale, w, s)
            : -Int128.Sign(dec._dec.Sig);   // the SDIDI value is past every wide value: its sign orders the pair
        return aWide ? order : -order;
    }

    /// <summary>The value as an SDIDI CONTENT — what a selection returns, and the operand an operation past the wide form takes: an
    /// SDIDI as itself; a wide value exactly when its significand fits the native Dec lane's 38-digit exact operand
    /// (<c>CobolDec.From</c>), and otherwise rounded ONCE to 34 digits in <paramref name="mode"/>.</summary>
    internal CobolDec ToDec(CobolRounding mode) =>
        _sdidi ? _dec
        : _wide.TryToInt128(out Int128 narrow) && Int128.Abs(narrow) < ExactDecLimit ? CobolDec.From(narrow, _scale)
        : _wide.ToDecRounded(_scale, mode);
}
