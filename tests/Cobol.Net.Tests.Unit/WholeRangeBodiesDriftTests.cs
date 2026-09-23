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
        Assert.Equal(Math.Sin(0.5), CobolIntrinsics.Sin(new CobolDec(5, -1)));                  // below 2π: the double body
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
