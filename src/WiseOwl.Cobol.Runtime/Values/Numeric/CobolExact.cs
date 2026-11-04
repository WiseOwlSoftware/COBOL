// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE EXACT VALUE OF A NATIVE FUNCTION ARGUMENT, WHATEVER CARRIED IT (kb/Work PB4111, PB4568; docs/CONFORMANCE.md DOC-A.1-123):
/// either an exact wide value at a scale (a scaled <see cref="Int128"/> value, an unsigned 16-byte container, an argument expression
/// of 39 to 77 digits on <see cref="CobolWide"/>) or an SDIDI value (<c>significand × 10^exponent</c>, read exactly at its own
/// exponent; a binary64 argument reaches here through its §8.8.1.5.1 conversion). It is the intake of the bodies that evaluate a
/// function under NATIVE arithmetic on the SDIDI lane (<c>CobolIntrinsics.*Exact</c>), which form the value of the function's
/// equivalent arithmetic expression from these exact values on the 256-bit wide form and round it ONCE into the returned-value
/// carrier.
/// <para>WHY (ISO/IEC 1989:2023 §8.8.1.3: native arithmetic is "an implementor-defined method of evaluating … all integer and
/// numeric functions"; §15.4.1: "When native arithmetic is in effect and an equivalent arithmetic expression is specified, the
/// value returned is an implementor-defined approximation of the value of that expression"). For that latitude CLAUDE.md rule 1
/// makes GnuCOBOL the answer, and GnuCOBOL 3.2.0 evaluates these functions from the exact argument values. The SDIDI bodies took
/// each argument as its 34-digit image and rounded every step: <c>VARIANCE (P, P - 1)</c> of a 39-digit P answered 10^10 where
/// the value is 0.25, and <c>ORD-MAX (P - 1, P, 2.0E0)</c> answered 1 although the compiler's own <c>IF P - 1 &lt; P</c> is true.</para>
/// <para>⛔ BOUNDED BY THE WIDE FORM, AS EVERY EXACT NATIVE INTERMEDIATE IS (DOC-A.1-123). The arithmetic is <see cref="CobolWide"/>'s,
/// never an unbounded integer (DESIGN §1.2 invariant 2 keeps arbitrary-precision integers out of the runtime value model): a body tries the work
/// on the 256-bit form (<see cref="TryAlign"/> and the <c>Try…</c> operations) and, where the values cannot be aligned or a step
/// leaves the range (an SDIDI exponent thousands of places from its neighbours, a product past 77 digits), evaluates on the
/// SDIDI images instead (<see cref="Image"/>) — the determination DOC-A.1-123 states for an intermediate past 77 digits. A
/// comparison never needs that: <see cref="Compare"/> is exact for every pair.</para>
/// <para>It is NOT a carrier of the compiler: no rendered value is ever a <see cref="CobolExact"/>. A body takes it and returns an
/// SDIDI (<see cref="CobolDec"/>), an ordinal or a binary64, so the settlement rules of the three carriers are untouched.</para>
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

    private CobolExact(CobolWide wide, int scale) => (_wide, _scale) = (wide, scale);

    private CobolExact(CobolDec dec) => (_dec, _sdidi) = (dec, true);

    /// <summary>A scaled <see cref="Int128"/> value (the native lane's exact carrier) at its scale.</summary>
    public static CobolExact From(Int128 unscaled, int scale) => new(CobolWide.From(unscaled), scale);

    /// <summary>An unsigned 16-byte container's value at its scale. A distinct NAME, not an overload of <see cref="From"/>: a
    /// <c>ulong</c> converts implicitly to both <see cref="Int128"/> and <see cref="UInt128"/> (CS0121).</summary>
    public static CobolExact FromUnsigned(UInt128 unscaled, int scale) => new(CobolWide.FromUnsigned(unscaled), scale);

    /// <summary>An exact wide intermediate (an argument expression of 39 to 77 digits) at its compile-time scale.</summary>
    public static CobolExact FromWide(CobolWide value, int scale) => new(value, scale);

    /// <summary>An SDIDI value exactly: its significand at its own exponent, nothing rounded.</summary>
    public static CobolExact FromDec(CobolDec value) => new(value);

    /// <summary>The algebraic sign: −1, 0 or +1.</summary>
    internal int Sign => _sdidi ? Int128.Sign(_dec.Sig) : _wide.Sign;

    /// <summary>The scale this value is exact at (negative for an integer with trailing zeros: an SDIDI's positive exponent).</summary>
    internal int Scale => _sdidi ? -_dec.Exp : _scale;

    /// <summary>The value as the SDIDI: an SDIDI as itself, a wide value lowered by round-to-odd (<see cref="CobolWide.ToDec"/>) —
    /// the operand the SDIDI evaluation takes where the wide form cannot carry the work.</summary>
    internal CobolDec Image => _sdidi ? _dec : _wide.ToDec(_scale);

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

    /// <summary>The value as an SDIDI CONTENT — what a selection returns: an SDIDI as itself; a wide value exactly when its
    /// significand fits the native Dec lane's 38-digit exact operand (<c>CobolDec.From</c>), and otherwise rounded ONCE to 34 digits
    /// in <paramref name="mode"/>.</summary>
    internal CobolDec ToDec(CobolRounding mode)
    {
        if (_sdidi) return _dec;
        return _wide.TryToInt128(out Int128 narrow) && Int128.Abs(narrow) < ExactDecLimit
            ? CobolDec.From(narrow, _scale)
            : _wide.ToDecRounded(_scale, mode);
    }
}
