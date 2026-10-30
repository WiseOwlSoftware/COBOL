// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Exceptions;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ AN ARGUMENT EXPRESSION PAST THE <c>Int128</c> CARRIER REACHES THE ILL-CONDITIONED BINARY64 BODIES AND MEAN AS ITS EXACT VALUE,
/// NOT AS THE 34-DIGIT ROUND-TO-ODD IMAGE (kb/Work PB4075; ISO/IEC 1989:2023 §15.82.4 / §15.20.4 / §15.89.4 / §15.55.4 / §15.56.4 /
/// §15.8.4 / §15.10.4 return the approximation of the function OF argument-1, and §15.60.4 r1 one quotient of the exact sum).
/// <para>Every expected value is an independent oracle's: Python <c>decimal</c> at 400 digits, π by Machin, the trigonometric
/// functions by Taylor series over the argument reduced modulo 2π, the logarithm by <c>Decimal.ln</c>, the arc functions by the
/// half-angle arctangent; the means by <c>Decimal</c> division in a 34-digit context of the rounding named. Binary64 results are
/// compared within two ulps.</para>
/// </summary>
public sealed class WideArgumentBodiesTests
{
    /// <summary>A decimal digit string (optionally signed) as the exact wide value, 18 digits at a time.</summary>
    private static CobolWide W(string digits)
    {
        bool negative = digits.StartsWith('-');
        string d = negative ? digits[1..] : digits;
        CobolWide acc = default;
        var step = CobolWide.From(1_000_000_000_000_000_000);
        int len = d.Length % 18 == 0 ? 18 : d.Length % 18;
        for (int pos = 0; pos < d.Length; pos += len, len = 18)
            acc = CobolWide.Add(CobolWide.Mul(acc, step), CobolWide.From(long.Parse(d.AsSpan(pos, len))));
        return negative ? CobolWide.Negate(acc) : acc;
    }

    private static void Near(double expected, double actual, string what)
    {
        double ulp = Math.BitIncrement(Math.Abs(expected)) - Math.Abs(expected);
        Assert.True(Math.Abs(actual - expected) <= 2 * ulp, $"{what} = {actual:R}, oracle {expected:R} ({Math.Abs(actual - expected) / ulp:F1} ulps)");
    }

    [Fact]
    public void TheDigitHelper_BuildsTheValueItNames()
    {
        foreach (string s in new[] { "7", "1000000000000000000", "998999999999999998002000000000000000999", new string('9', 77), "-12345678901234567890123" })
        {
            BigInteger expected = BigInteger.Parse(s);
            CobolWide w = W(s);
            Assert.Equal(expected.Sign, Math.Sign(CobolWide.Compare(w, 0, default, 0)));
            Assert.Equal(BigInteger.Abs(expected).ToString(), w.MagnitudeDigits());
        }
    }

    [Theory]
    [InlineData("Sin", "998999999999999998002000000000000000999", 0, 0.932559711390065)]
    [InlineData("Cos", "998999999999999998002000000000000000999", 0, 0.3610157679272177)]
    [InlineData("Tan", "998999999999999998002000000000000000999", 0, 2.5831550703294295)]
    [InlineData("Sin", "-998999999999999998002000000000000000999", 0, -0.932559711390065)]
    [InlineData("Cos", "-998999999999999998002000000000000000999", 0, 0.3610157679272177)]
    [InlineData("Tan", "-998999999999999998002000000000000000999", 0, -2.5831550703294295)]
    [InlineData("Sin", "123456789012345678901234567890123456789012", 20, -0.5896203135127723)]
    [InlineData("Cos", "123456789012345678901234567890123456789012", 20, -0.8076805593135816)]
    [InlineData("Tan", "123456789012345678901234567890123456789012", 20, 0.7300167209841837)]
    [InlineData("Sin", "314159265358979323846264338327900000000000000000000", 50, 5.028841971693994e-31)]
    [InlineData("Cos", "314159265358979323846264338327900000000000000000000", 50, -1.0)]
    [InlineData("Tan", "314159265358979323846264338327900000000000000000000", 50, -5.028841971693994e-31)]
    [InlineData("Sin", "99999999999999999999999999999999999999999999999999999999999999999999999999999", 0, 0.03546699526220028)]
    [InlineData("Cos", "99999999999999999999999999999999999999999999999999999999999999999999999999999", 0, -0.9993708482075465)]
    [InlineData("Tan", "99999999999999999999999999999999999999999999999999999999999999999999999999999", 0, -0.03548932343365152)]
    [InlineData("Sin", "99999999999999999999999999999999999999999999999999999999999999999999999999999", 60, -0.4645301048353727)]
    [InlineData("Cos", "99999999999999999999999999999999999999999999999999999999999999999999999999999", 60, -0.8855573282976307)]
    [InlineData("Tan", "99999999999999999999999999999999999999999999999999999999999999999999999999999", 60, 0.5245624309025501)]
    [InlineData("Sin", "5000000000000000000000000000000000000000000", 0, 0.9910597904469083)]
    [InlineData("Cos", "5000000000000000000000000000000000000000000", 0, 0.13341848357454186)]
    [InlineData("Tan", "5000000000000000000000000000000000000000000", 0, 7.428204577765239)]
    [InlineData("Log", "1000000000000000000000000100000000000000000000", 45, 1e-25)]
    [InlineData("Log10", "1000000000000000000000000100000000000000000000", 45, 4.3429448190325184e-26)]
    [InlineData("Log", "999999999999999999999999900000000000000000000", 45, -1e-25)]
    [InlineData("Log10", "999999999999999999999999900000000000000000000", 45, -4.3429448190325184e-26)]
    [InlineData("Log", "1000000000000000000000000030000000000000000000", 45, 3e-26)]
    [InlineData("Log", "123500000000000000000000000000000000000000", 40, 2.513656063073986)]
    [InlineData("Log10", "123500000000000000000000000000000000000000", 40, 1.0916669575956846)]
    [InlineData("Log", "100000000000000000000000000000000000000000000000000", 45, 11.512925464970229)]
    [InlineData("Log10", "100000000000000000000000000000000000000000000000000", 45, 5.0)]
    [InlineData("Acos", "999999999999999999999999900000000000000000000", 45, 4.4721359549995796e-13)]
    [InlineData("Acos", "-999999999999999999999999900000000000000000000", 45, 3.141592653589346)]
    [InlineData("Acos", "750000000000000000000000000000000000000000000", 45, 0.7227342478134157)]
    [InlineData("Acos", "-600000000000000000000000000000000000000000000", 45, 2.214297435588181)]
    [InlineData("Asin", "999999999999999999999999900000000000000000000", 45, 1.5707963267944494)]
    [InlineData("Asin", "-999999999999999999999999900000000000000000000", 45, -1.5707963267944494)]
    [InlineData("Asin", "750000000000000000000000000000000000000000000", 45, 0.848062078981481)]
    [InlineData("Asin", "-600000000000000000000000000000000000000000000", 45, -0.6435011087932844)]
    public void TheWideBodies_AnswerForTheExactArgument(string method, string digits, int scale, double expected)
    {
        CobolWide x = W(digits);
        double actual = method switch
        {
            "Sin" => CobolIntrinsics.SinWide(x, scale),
            "Cos" => CobolIntrinsics.CosWide(x, scale),
            "Tan" => CobolIntrinsics.TanWide(x, scale),
            "Log" => CobolIntrinsics.LogWide(x, scale),
            "Log10" => CobolIntrinsics.Log10Wide(x, scale),
            "Acos" => CobolIntrinsics.AcosWide(x, scale),
            "Asin" => CobolIntrinsics.AsinWide(x, scale),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "no such wide body"),
        };
        Near(expected, actual, $"{method}({digits}E-{scale})");
    }

    /// <summary>The 34-digit image the old lane handed these bodies is NOT the argument: the same oracle's sine of the image stands
    /// hundreds of radians from the product, which is what this change removes.</summary>
    [Fact]
    public void TheImage_IsNotTheArgument()
    {
        CobolWide x = W("998999999999999998002000000000000000999");
        CobolDec image = x.ToDec(0);
        Assert.NotEqual(0.932559711390065, CobolIntrinsics.Sin(image), 6);
        Assert.Equal(0.932559711390065, CobolIntrinsics.SinWide(x, 0), 12);
    }

    /// <summary>The domain screen is the SDIDI overload's own predicate on the exact wide value: the bounds themselves are
    /// admitted, a value one unit of the 45th place past them is not, and a rejected argument raises under checking and is null
    /// (the body's NaN) otherwise.</summary>
    [Fact]
    public void TheWideDomainScreen_DecidesOnTheExactValue()
    {
        var closed = CobolIntrinsics.ArgumentDomain.ClosedUnit;
        var positive = CobolIntrinsics.ArgumentDomain.Positive;
        string one = "1" + new string('0', 45);
        string past = "1" + new string('0', 44) + "1";
        bool saved = ExceptionState.ArgumentFunctionChecking;
        try
        {
            ExceptionState.ArgumentFunctionChecking = false;
            Assert.NotNull(CobolIntrinsics.DomainWideAdmitted(W(one), 45, closed, "ACOS", "§15.8.3 rule 2"));
            Assert.NotNull(CobolIntrinsics.DomainWideAdmitted(W("-" + one), 45, closed, "ACOS", "§15.8.3 rule 2"));
            Assert.Null(CobolIntrinsics.DomainWideAdmitted(W(past), 45, closed, "ACOS", "§15.8.3 rule 2"));
            Assert.Null(CobolIntrinsics.DomainWideAdmitted(W("-" + past), 45, closed, "ACOS", "§15.8.3 rule 2"));
            Assert.Null(CobolIntrinsics.DomainWideAdmitted(W("0"), 45, positive, "LOG", "§15.55.3 rule 2"));
            Assert.NotNull(CobolIntrinsics.DomainWideAdmitted(W("1"), 45, positive, "LOG", "§15.55.3 rule 2"));
            Assert.True(double.IsNaN(CobolIntrinsics.AcosWide(null, 45)));
            Assert.True(double.IsNaN(CobolIntrinsics.LogWide(null, 45)));
            ExceptionState.ArgumentFunctionChecking = true;
            Assert.Throws<CobolFatalException>(() =>
                CobolIntrinsics.DomainWideAdmitted(W(past), 45, closed, "ACOS", "§15.8.3 rule 2"));
        }
        finally { ExceptionState.ArgumentFunctionChecking = saved; }
    }

    private static void MeanIs(string sig, int exp, CobolRounding mode, int scale, params string[] values)
    {
        CobolDec mean = CobolIntrinsics.MeanWide(mode, scale, values.Select(W).ToArray());
        Assert.Equal(Int128.Parse(sig), mean.Sig);
        Assert.Equal(exp, mean.Exp);
    }

    /// <summary>⛔ MEAN of an exact wide sum is ONE rounding of the exact quotient (kb/Work PB4075): the oracle is
    /// <c>Decimal</c> division in a 34-digit context. The first row is the report's own: the sum of the 39-digit product and 1
    /// halved, whose exact …000500 the old two-step (image, then SDIDI quotient) made …000100000.</summary>
    [Fact]
    public void MeanOfAnExactWideSum_IsOneRounding()
    {
        const string N = "998999999999999998002000000000000000999";
        var away = CobolRounding.NearestAwayFromZero;
        var even = CobolRounding.NearestEven;
        MeanIs("4994999999999999990010000000000000", 5, away, 0, N, "1");
        MeanIs("3329999999999999993340000000000000", 5, away, 0, N, "1", "2");
        MeanIs("-4994999999999999990010000000000000", 5, away, 0, "-" + N, "1");
        MeanIs("3000000000000000000000000000000000", 7, away, 0, "40000000000000000000000000000000000000000", "20000000000000000000000000000000000000000");
        // The tie at the 34th digit: …325 is dropped to …33 away from zero and …32 to even; …335 is …34 either way.
        MeanIs("1234567890123456789012345678901233", 1, away, 0, "12345678901234567890123456789012325", "12345678901234567890123456789012325");
        MeanIs("1234567890123456789012345678901232", 1, even, 0, "12345678901234567890123456789012325", "12345678901234567890123456789012325");
        MeanIs("1234567890123456789012345678901234", 1, away, 0, "12345678901234567890123456789012335", "12345678901234567890123456789012335");
        MeanIs("1234567890123456789012345678901234", 1, even, 0, "12345678901234567890123456789012335", "12345678901234567890123456789012335");
        // A quotient with fewer digits than the SDIDI holds keeps its preferred exponent (the dividend's), or the places it needs.
        MeanIs("4994999999999999990010000000000000", -7, away, 12, N, "7");
        MeanIs("15", -41, away, 40, "1", "2");
        MeanIs("5", -1, away, 0, "1", "0");
        MeanIs("0", -5, away, 5, "0", "0");
    }

    /// <summary><c>MeanWide</c> and <c>MeanDec</c> are one rule: wherever the SDIDI can hold the exact sum (34 digits or fewer) the two
    /// quotients are the same value at the same exponent, in every rounding mode, so the lane is a representation and never changes
    /// the answer (kb/Work PB4075's claim about <c>CobolWide.QuotientDec</c>).</summary>
    [Theory]
    [InlineData(CobolRounding.NearestAwayFromZero)]
    [InlineData(CobolRounding.NearestEven)]
    [InlineData(CobolRounding.Truncation)]
    public void MeanWide_AgreesWithMeanDec_WhereTheSdidiHoldsTheSum(CobolRounding mode)
    {
        string[][] lists =
        [
            ["1", "2"], ["1", "2", "4"], ["7"], ["-5", "2"], ["10", "20", "30", "40"], ["0", "0"],
            ["1234567890123456789012345678901", "1"], ["-1234567890123456789012345678901", "2", "3"],
        ];
        foreach (string[] list in lists)
        foreach (int scale in new[] { 0, 3, 17 })
        {
            CobolDec wide = CobolIntrinsics.MeanWide(mode, scale, list.Select(W).ToArray());
            CobolDec sdidi = CobolIntrinsics.MeanDec(mode, list.Select(v => new CobolDec(Int128.Parse(v), -scale)).ToArray());
            Assert.True(wide.Sig == sdidi.Sig && wide.Exp == sdidi.Exp,
                $"MEAN ({string.Join(' ', list)}) at scale {scale}, {mode}: wide {wide.Sig}E{wide.Exp}, SDIDI {sdidi.Sig}E{sdidi.Exp}");
        }
    }

    /// <summary>INTERMEDIATE ROUNDING IS PROHIBITED (§11.9.11.2 r3d): an inexact exact-sum quotient raises EC-SIZE-TRUNCATION, an
    /// exact one does not — the mode reaches the one rounding.</summary>
    [Fact]
    public void MeanWide_UnderProhibited_RaisesOnlyForAnInexactQuotient()
    {
        const string N = "998999999999999998002000000000000000999";
        Assert.Equal("EC-SIZE-TRUNCATION",
            Assert.Throws<CobolSizeError>(() => CobolIntrinsics.MeanWide(CobolRounding.Prohibited, 0, W(N), W("1"))).EcName);
        CobolDec exact = CobolIntrinsics.MeanWide(CobolRounding.Prohibited, 0, W("3" + new string('0', 40)), W("1" + new string('0', 40)));
        Assert.Equal(Int128.Parse("2000000000000000000000000000000000"), exact.Sig);
        Assert.Equal(7, exact.Exp);
    }

    /// <summary>An empty argument list invents no value (§15.3 requires at least one argument; kb/Work PB257).</summary>
    [Fact]
    public void MeanOfNothing_IsRefused() =>
        Assert.ThrowsAny<Exception>(() => CobolIntrinsics.MeanWide(CobolRounding.NearestAwayFromZero, 0));
}
