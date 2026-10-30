// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE ILL-CONDITIONED BINARY64-FAMILY BODIES ON THE EXACT WIDE INTERMEDIATE (kb/Work PB4075): SIN, COS, TAN, LOG, LOG10,
/// ACOS and ASIN of an argument expression whose exact value passes the <see cref="Int128"/> carrier (a nested product of 39 to
/// 77 digits, <see cref="CobolWide"/>). They are the exact-intake overloads of <c>CobolIntrinsics.Float.cs</c> (kb/Work PB999,
/// PB1041) on the wider carrier, with the same contracts and the same domain screen.
/// <para>WHY THEY EXIST. §15.82.4 r1 / §15.20.4 r1 / §15.89.4 r1 return an approximation of the sine / cosine / tangent OF
/// argument-1, and argument-1 is the exact value its expression has. The argument used to arrive as the 34-digit round-to-odd
/// image of that value, and the image of a 39-digit product is some 999 radians from the product: <c>FUNCTION SIN (BIG * BIG *
/// K3)</c> of 18-, 18- and 3-digit items printed the sine of the image. LOG, LOG10, ACOS and ASIN are ill-conditioned at 1 and
/// ±1 exactly as they are for an SDIDI, and a wide value can sit 10^−25 from 1 where the image's 10^−34 rounding is the whole
/// answer. So each body forms, on the exact wide value, the quantity its function is well conditioned in (the quadrant and
/// residue about the nearest multiple of π/2, x − 1, 1 − |x|) and narrows only that. EXP, EXP10 and SQRT are NOT here: a wide
/// argument's image is within 10^−34 relative of it, so their condition numbers (at most 709 for EXP, ½ for SQRT) keep the
/// image's error far below binary64's.</para>
/// </summary>
public static partial class CobolIntrinsics
{
    private static readonly CobolWide OneWide = CobolWide.From(1);
    private static readonly CobolWide MinusOneWide = CobolWide.From(-1);

    /// <summary>The SAME argument-1 domain screen as <see cref="DomainDecAdmitted"/> — one predicate, <see cref="Admits"/> — over
    /// an exact wide operand (kb/Work PB4075): the admitted value itself, or <c>null</c> after the condition is raised.</summary>
    public static CobolWide? DomainWideAdmitted(CobolWide value, int scale, ArgumentDomain domain, string function, string rule)
    {
        if (Admits(domain, Math.Sign(CobolWide.Compare(value, scale, MinusOneWide, 0)), value.Sign,
                Math.Sign(CobolWide.Compare(value, scale, OneWide, 0))))
            return value;
        DomainViolation(domain, function, rule);
        return null;
    }

    /// <summary>§15.82 SIN of an exact wide argument: sin(q·π/2 + r) over the residue r of the 39-to-77-digit magnitude itself
    /// (<see cref="ReduceQuarterTurns(CobolWide, int)"/>); below π/4 the double body.</summary>
    public static double SinWide(CobolWide x, int scale)
    {
        double d = x.ToDouble(scale);
        return Math.Abs(d) <= QuarterPi ? Math.Sin(d) : SinReduced(ReduceQuarterTurns(x, scale), x.Sign < 0);
    }

    /// <summary>§15.20 COS of an exact wide argument.</summary>
    public static double CosWide(CobolWide x, int scale)
    {
        double d = x.ToDouble(scale);
        return Math.Abs(d) <= QuarterPi ? Math.Cos(d) : CosReduced(ReduceQuarterTurns(x, scale));
    }

    /// <summary>§15.89 TAN of an exact wide argument.</summary>
    public static double TanWide(CobolWide x, int scale)
    {
        double d = x.ToDouble(scale);
        return Math.Abs(d) <= QuarterPi ? Math.Tan(d) : TanReduced(ReduceQuarterTurns(x, scale), x.Sign < 0);
    }

    /// <summary>§15.55 LOG of an exact wide argument: ln(1 + u) of the EXACT u = x − 1 within ½ of 1
    /// (<see cref="NearOneWide"/>), the logarithm of the value elsewhere, where its relative error is the image's 10^−34.
    /// <c>null</c> = rejected by §15.55.3 r2.</summary>
    public static double LogWide(CobolWide? x, int scale) =>
        x is not { } v ? double.NaN
        : NearOneWide(v, scale, out double u) ? LogOnePlus(u) : Math.Log(v.ToDouble(scale));

    /// <summary>§15.56 LOG10 of an exact wide argument (<see cref="LogWide"/>'s formulation over log10). <c>null</c> = rejected by
    /// §15.56.3 r2.</summary>
    public static double Log10Wide(CobolWide? x, int scale) =>
        x is not { } v ? double.NaN
        : NearOneWide(v, scale, out double u) ? Log10OnePlus(u) : Math.Log10(v.ToDouble(scale));

    /// <summary>Whether x lies within ½ of 1 and, if so, <paramref name="u"/> = x − 1 formed on the EXACT wide value (the
    /// alignment of 1 is exact: a value within ½ of 1 has at most 77 places) and only then narrowed.</summary>
    private static bool NearOneWide(CobolWide v, int scale, out double u)
    {
        u = 0;
        if (!(Math.Abs(v.ToDouble(scale) - 1) < 0.5)) return false;
        u = CobolWide.Sub(v, CobolWide.Up(OneWide, scale)).ToDouble(scale);
        return true;
    }

    /// <summary>§15.8 ACOS of an exact wide argument (<see cref="Acos(CobolDec?)"/>'s half-angle identity over the EXACT wide
    /// 1 − |x|). <c>null</c> = rejected by §15.8.3 r2.</summary>
    public static double AcosWide(CobolWide? x, int scale)
    {
        if (x is not { } v) return double.NaN;
        double d = v.ToDouble(scale);
        if (Math.Abs(d) <= 0.5) return Math.Acos(d);
        double fromOne = AcosOfMagnitudeWide(v, scale);
        return v.Sign > 0 ? fromOne : (PiHi - fromOne) + PiLo;
    }

    /// <summary>§15.10 ASIN of an exact wide argument. <c>null</c> = rejected by §15.10.3 r2.</summary>
    public static double AsinWide(CobolWide? x, int scale)
    {
        if (x is not { } v) return double.NaN;
        double d = v.ToDouble(scale);
        if (Math.Abs(d) <= 0.5) return Math.Asin(d);
        double r = (HalfPiHi - AcosOfMagnitudeWide(v, scale)) + HalfPiLo;
        return v.Sign > 0 ? r : -r;
    }

    private static double AcosOfMagnitudeWide(CobolWide v, int scale) =>
        AcosOfGap(CobolWide.Sub(CobolWide.Up(OneWide, scale), CobolWide.Abs(v)).ToDouble(scale));
}
