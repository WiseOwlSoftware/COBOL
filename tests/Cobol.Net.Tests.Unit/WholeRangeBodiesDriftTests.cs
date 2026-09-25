// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Binding;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A WHOLE-RANGE float body takes an SDIDI argument UNNARROWED, and its domain is still the ONE screen's (kb/Work
/// PB999). The SDIDI reaches 10^±6144 (ISO §8.8.1.5.2) and binary64 does not, so narrowing before the body answered
/// LOG10(10^−400) with log10(+0.0) and SIN(10^400) with sin(+∞) — the §15.56.4 r1 / §15.82.4 r1 returned value is the
/// approximation of the function OF ARGUMENT-1, not of a substitute.
/// </summary>
/// <remarks>
/// The drift half keeps <see cref="IntrinsicRenderer.WholeRangeBodies"/> and the runtime in step: every member is a
/// Float catalog row with a <c>CobolDec</c> overload, and the overload's parameter is <c>CobolDec?</c> EXACTLY when
/// the row carries a §15.x.3 value domain — the renderer hands a domain row
/// <c>CobolIntrinsics.DomainDecAdmitted</c>'s nullable result, so a mismatch would either fail to compile the
/// generated C# or bypass the screen. The runtime half pins the values against an independent oracle (Python
/// <c>decimal</c> with π to 6400 digits).
/// </remarks>
public sealed class WholeRangeBodiesDriftTests
{
    [Fact]
    public void EveryWholeRangeBody_HasTheCarrierOverloadItsDomainRequires()
    {
        Assert.NotEmpty(IntrinsicRenderer.WholeRangeBodies);
        foreach (string method in IntrinsicRenderer.WholeRangeBodies.Keys)
        {
            // Each member's function-name is its runtime method upper-cased (LOG10 → Log10); the catalog proves it.
            Assert.True(IntrinsicCatalog.TryGet(method.ToUpperInvariant(), out var sig) && sig.RuntimeMethod == method,
                $"{method}: no catalog row whose runtime method it is");
            Assert.True(sig.Float, $"{method}: a whole-range body must be a binary64-family (Float) row");
            Type want = sig.Domain is IntrinsicDomain.None ? typeof(CobolDec) : typeof(CobolDec?);
            var overload = typeof(CobolIntrinsics).GetMethod(method, BindingFlags.Public | BindingFlags.Static, [want]);
            Assert.True(overload is not null && overload.ReturnType == typeof(double),
                $"{method}: no public static double {method}({want.Name}{(want == typeof(CobolDec?) ? "?" : "")}) — the renderer emits one for an SDIDI operand");
        }
    }

    private static CobolDec Pow10(int e) => new(1, e);

    private static void UnderChecking(bool on, Action body)
    {
        bool saved = ExceptionState.ArgumentFunctionChecking;
        ExceptionState.ArgumentFunctionChecking = on;
        try { body(); }
        finally { ExceptionState.ArgumentFunctionChecking = saved; }
    }

    [Fact]
    public void LogFamily_OutsideBinary64_AnswersForTheRealArgument()
    {
        Assert.Equal(0.0, Pow10(-400).ToDouble());                           // the substitute the old lane used
        Assert.Equal(-400.0, CobolIntrinsics.Log10(Pow10(-400)));
        Assert.Equal(400.0, CobolIntrinsics.Log10(Pow10(400)));
        Assert.Equal(-400 * Math.Log(10), CobolIntrinsics.Log(Pow10(-400)), 10);
        Assert.Equal(1e-200, CobolIntrinsics.Sqrt(Pow10(-400)), 1e-214);
        Assert.Equal(1e200, CobolIntrinsics.Sqrt(Pow10(400)) / 1.0, 1e186);
        // Inside the range the overloads ARE the double bodies.
        Assert.Equal(Math.Log10(123.5), CobolIntrinsics.Log10(new CobolDec(1235, -1)));
        Assert.Equal(CobolIntrinsics.Sqrt(2.0), CobolIntrinsics.Sqrt(new CobolDec(2, 0)));
        Assert.True(double.IsNaN(CobolIntrinsics.Log10((CobolDec?)null)));      // a rejected argument
    }

    [Fact]
    public void Trig_ReducesTheExactArgumentModuloTwoPi()
    {
        // Oracle: x mod 2π centred on [−π, π) by an independent Python decimal computation (prec 6400, π by Machin).
        Assert.Equal(-0.9985382319830978, CobolIntrinsics.Sin(Pow10(400)), 1e-14);
        Assert.Equal(-0.054049970102390536, CobolIntrinsics.Cos(Pow10(400)), 1e-14);
        Assert.Equal(18.474353086440175, CobolIntrinsics.Tan(Pow10(400)), 1e-11);
        Assert.Equal(0.9985382319830978, CobolIntrinsics.Sin(new CobolDec(-1, 400)), 1e-14);   // odd
        Assert.Equal(-0.5696334009536362, CobolIntrinsics.Sin(Pow10(40)), 1e-14);              // inside binary64
        Assert.Equal(0.9168078385445297, CobolIntrinsics.Sin(Pow10(6144)), 1e-14);             // the SDIDI's top place
        Assert.Equal(-0.8296453523350967, CobolIntrinsics.Cos(                                  // its largest value
            new CobolDec(Int128.Parse("9999999999999999999999999999999999"), 6111)), 1e-14);
        Assert.Equal(-0.06632189735120068, CobolIntrinsics.Sin(new CobolDec(125, -1)), 1e-15);  // a negative Exp
        Assert.Equal(0.22506860600748407, CobolIntrinsics.Sin(                                  // 34 digits, Exp −10
            new CobolDec(Int128.Parse("1234567890123456789012345678901234"), -10)), 1e-14);
        Assert.Equal(Math.Sin(0.5), CobolIntrinsics.Sin(new CobolDec(5, -1)));                  // below π/4: the double body
    }

    /// <summary>⛔ Every EXACT-INTAKE arm (kb/Work PB1041), each against an independent oracle — Python
    /// <c>decimal</c> at 140 digits, π by Machin — to within two binary64 ulps. The rows sit where each body is
    /// ILL-CONDITIONED (sin / cos / tan within 10^−18 … 10^−34 of a multiple of π/2, in all four quadrants and both
    /// signs; LOG / LOG10 at 1 ± 10^−26; ACOS / ASIN at ±(1 − 10^−26); EXP / EXP10 at |x| ≈ 700 / 22), where
    /// narrowing the argument first answered 1.22·10^−16 for 8.46·10^−18, 0 for 10^−26, 0 for 1.414·10^−13 and was
    /// 150 ulps off — plus ordinary arguments, so the reduced path is pinned where the double body used to be.</summary>
    [Theory]
    [InlineData("Sin", "314159265358979323", -17, 8.462643383279504e-18)]
    [InlineData("Sin", "-314159265358979323", -17, -8.462643383279504e-18)]
    [InlineData("Sin", "3141592653589793238462643383279503", -33, -1.158028306006249e-34)]
    [InlineData("Cos", "1570796326794896619231321691639", -30, 7.514420985846997e-31)]
    [InlineData("Tan", "1570796326794896619231321691639", -30, 1.3307745225925532e+30)]
    [InlineData("Tan", "-1570796326794896619231321691639", -30, -1.3307745225925532e+30)]
    [InlineData("Sin", "4712388980384689857693965074919", -30, -1.0)]
    [InlineData("Cos", "4712388980384689857693965074919", -30, -2.5432629575409906e-31)]
    [InlineData("Tan", "4712388980384689857693965074919", -30, 3.931956768508404e+30)]
    [InlineData("Cos", "6283185307179586476925286766559", -30, 1.0)]
    [InlineData("Sin", "6283185307179586476925286766559", -30, -5.7683943387987505e-33)]
    [InlineData("Sin", "1", 0, 0.8414709848078965)]
    [InlineData("Cos", "1", 0, 0.5403023058681398)]
    [InlineData("Tan", "1", 0, 1.5574077246549023)]
    [InlineData("Sin", "2", 0, 0.9092974268256817)]
    [InlineData("Cos", "3", 0, -0.9899924966004454)]
    [InlineData("Tan", "5", 0, -3.380515006246586)]
    [InlineData("Sin", "-4", 0, 0.7568024953079282)]
    [InlineData("Sin", "12345678901234567", 0, 0.31833323676993)]
    [InlineData("Cos", "12345678901234567", 0, -0.9479788765408118)]
    [InlineData("Tan", "12345678901234567", 0, -0.3358020359393792)]
    [InlineData("Sin", "1", 30, -0.09011690191213806)]
    [InlineData("Cos", "1", 30, -0.9959311944053957)]
    [InlineData("Tan", "1", 30, 0.09048506806330217)]
    [InlineData("Log", "100000000000000000000000001", -26, 1e-26)]
    [InlineData("Log", "99999999999999999999999999", -26, -1e-26)]
    [InlineData("Log10", "100000000000000000000000001", -26, 4.3429448190325184e-27)]
    [InlineData("Log", "125", -2, 0.22314355131420976)]
    [InlineData("Log10", "7", -1, -0.15490195998574316)]
    [InlineData("Acos", "99999999999999999999999999", -26, 1.414213562373095e-13)]
    [InlineData("Acos", "-99999999999999999999999999", -26, 3.141592653589652)]
    [InlineData("Acos", "75", -2, 0.7227342478134157)]
    [InlineData("Acos", "-6", -1, 2.214297435588181)]
    [InlineData("Asin", "99999999999999999999999999", -26, 1.5707963267947551)]
    [InlineData("Asin", "-99999999999999999999999999", -26, -1.5707963267947551)]
    [InlineData("Asin", "75", -2, 0.848062078981481)]
    [InlineData("Exp", "700123456789012345678901234567", -27, 1.1475032771153836e+304)]
    [InlineData("Exp", "-700123456789012345678901234567", -27, 8.714572062171533e-305)]
    [InlineData("Exp", "25", -1, 12.182493960703473)]
    [InlineData("Exp", "-55", -1, 0.004086771438464067)]
    [InlineData("Exp10", "221234567890123456789012345678", -28, 1.3287913398290713e+22)]
    [InlineData("Exp10", "-221234567890123456789012345678", -28, 7.525636042515409e-23)]
    [InlineData("Exp10", "3", 0, 1000.0)]
    public void EveryExactIntakeArm_AnswersForTheExactArgument(string method, string sig, int exp, double expected)
    {
        var x = new CobolDec(Int128.Parse(sig), exp);
        double actual = method switch
        {
            "Sin" => CobolIntrinsics.Sin(x),
            "Cos" => CobolIntrinsics.Cos(x),
            "Tan" => CobolIntrinsics.Tan(x),
            "Log" => CobolIntrinsics.Log((CobolDec?)x),
            "Log10" => CobolIntrinsics.Log10((CobolDec?)x),
            "Acos" => CobolIntrinsics.Acos((CobolDec?)x),
            "Asin" => CobolIntrinsics.Asin((CobolDec?)x),
            "Exp" => CobolIntrinsics.Exp(x),
            "Exp10" => CobolIntrinsics.Exp10(x),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "no such arm"),
        };
        double ulp = Math.BitIncrement(Math.Abs(expected)) - Math.Abs(expected);
        Assert.True(Math.Abs(actual - expected) <= 2 * ulp,
            $"{method}({sig}E{exp}) = {actual:R}, oracle {expected:R} ({Math.Abs(actual - expected) / ulp:F1} ulps)");
    }

    /// <summary>⛔ ANNUITY's §15.9.4 1) b) denominator 1 − (1 + rate)^−n, evaluated without forming 1 + rate in
    /// binary64 (kb/Work PB1310): a rate below 2^−53 made it 1 − 1 = 0 and the value 0 where it is about 1/n.
    /// Oracle: Python <c>decimal</c> at 80 digits, to within two ulps.</summary>
    [Theory]
    [InlineData(1e-17, 12, 0.08333333333333334)]
    [InlineData(1e-20, 12, 0.08333333333333333)]
    [InlineData(1e-8, 360, 0.002777782791669667)]
    [InlineData(0.05, 10, 0.1295045749654567)]
    [InlineData(0.5, 3, 0.7105263157894737)]
    public void Annuity_KeepsATinyRate(double rate, double periods, double expected)
    {
        double actual = CobolIntrinsics.Annuity(rate, periods);
        double ulp = Math.BitIncrement(expected) - expected;
        Assert.True(Math.Abs(actual - expected) <= 2 * ulp, $"ANNUITY({rate:R} {periods}) = {actual:R}, oracle {expected:R}");
    }

    [Fact]
    public void TheNewDomainArms_TurnARejectedArgumentIntoNaN()
    {
        Assert.True(double.IsNaN(CobolIntrinsics.Acos((CobolDec?)null)));
        Assert.True(double.IsNaN(CobolIntrinsics.Asin((CobolDec?)null)));
        Assert.Equal(0.0, CobolIntrinsics.Acos((CobolDec?)new CobolDec(1, 0)));                 // the bound itself
        Assert.Equal(Math.PI, CobolIntrinsics.Acos((CobolDec?)new CobolDec(-1, 0)));
        Assert.Equal(Math.PI / 2, CobolIntrinsics.Asin((CobolDec?)new CobolDec(1, 0)));
        Assert.Equal(0.0, CobolIntrinsics.Log((CobolDec?)new CobolDec(1, 0)));
    }

    [Fact]
    public void TheAdmittedScreen_IsDomainDecsPredicate()
    {
        UnderChecking(true, () =>
        {
            Assert.Equal(Pow10(-400), CobolIntrinsics.DomainDecAdmitted(Pow10(-400), CobolIntrinsics.ArgumentDomain.Positive, "LOG10", "§15.56.3 rule 2"));
            Assert.Throws<CobolFatalException>(() => CobolIntrinsics.DomainDecAdmitted(new CobolDec(-1, -400), CobolIntrinsics.ArgumentDomain.NonNegative, "SQRT", "§15.84.3 rule 2"));
        });
        UnderChecking(false, () =>
            Assert.Null(CobolIntrinsics.DomainDecAdmitted(new CobolDec(0, 0), CobolIntrinsics.ArgumentDomain.Positive, "LOG", "§15.55.3 rule 2")));
    }
}
