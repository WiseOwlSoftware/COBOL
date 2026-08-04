// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ TWO CARRIERS IMPLEMENT ONE RULE, SO THE RULE MUST BE ASSERTED ON BOTH (fix-queue PB32).
/// </summary>
/// <remarks>
/// <para>
/// <c>IntrinsicRenderer.RenderNum</c> routes on <c>AnyRealArgument</c>, so every exact-family function has an
/// exact <see cref="Int128"/> body AND a binary64 one. Its sibling <c>IntrinsicRealArgDriftTests</c> asserts the
/// second body EXISTS; nothing asserted that the two AGREE, and they did not. MOD's §15.64.3 r2 zero-divisor rule
/// was written into both bodies and then corrected in only one: <c>ModReal</c> read <c>b == 0 ? 0 : …</c>, a
/// second independent guard that returned the §15.3 default without ever SETTING EC-ARGUMENT-FUNCTION. Under
/// <c>&gt;&gt;TURN EC-ARGUMENT-FUNCTION CHECKING ON</c> the run unit sailed past a condition Table 13
/// (§14.6.13.1.1) makes FATAL.
/// </para>
/// <para>
/// ⚠ A GUARD THAT ONLY KNOWS ABOUT MOD AND REM WOULD BE THE SIXTH INDIVIDUAL FIX. The second test below is the
/// general one: it reads <c>CobolIntrinsics.RealArgs.cs</c> and fails on ANY real body that answers a domain
/// guard with a literal instead of routing to a shared raise site. That is the shape — an exact body raising
/// where its float twin silently returns — rather than the two instances of it that happen to exist today.
/// </para>
/// </remarks>
public sealed class IntrinsicCarrierAgreementDriftTests
{
    /// <summary>Run <paramref name="body"/> with EC-ARGUMENT-FUNCTION checking forced on, restoring it after —
    /// the flag is process-global ambient state, so a leak would silently arm every later test in the class.</summary>
    private static void UnderChecking(Action body)
    {
        bool saved = ExceptionState.ArgumentFunctionChecking;
        ExceptionState.ArgumentFunctionChecking = true;
        try { body(); }
        finally { ExceptionState.ArgumentFunctionChecking = saved; }
    }

    /// <summary>
    /// §15.64.3 r2 / §15.77.3 r2: a zero divisor sets EC-ARGUMENT-FUNCTION — from EITHER carrier.
    /// This is the assertion that was missing; <c>ModReal(1, 0)</c> returned 0.0 and raised nothing.
    /// </summary>
    [Theory]
    [InlineData("MOD")]
    [InlineData("REM")]
    public void ZeroDivisor_RaisesFromBothCarriers(string fn)
    {
        UnderChecking(() =>
        {
            // The EXACT (Int128) carrier — this arm was already correct.
            var exact = Assert.Throws<CobolFatalException>(() =>
                fn == "MOD" ? CobolIntrinsics.ModScaled(11, 0) : CobolIntrinsics.RemScaled(11, 0));
            // The BINARY64 carrier, reached whenever any argument renders as a double — this arm was silent.
            var real = Assert.Throws<CobolFatalException>(() =>
                fn == "MOD" ? CobolIntrinsics.ModReal(11d, 0d) : CobolIntrinsics.RemReal(11d, 0d));

            Assert.Equal("EC-ARGUMENT-FUNCTION", exact.EcName);
            Assert.Equal("EC-ARGUMENT-FUNCTION", real.EcName);
            // One raise site per RULE means one message per rule: the citation cannot drift between carriers.
            Assert.Equal(exact.Message, real.Message);
            Assert.Contains(fn == "MOD" ? "15.64.3" : "15.77.3", exact.Message);
        });
    }

    /// <summary>With checking DISABLED, §15.3's closing paragraph makes the returned value implementor-defined —
    /// but it must be the SAME implementor-defined value from both carriers, for the same reason §15.4 forbids a
    /// result that depends on its receiver's shape.</summary>
    [Fact]
    public void ZeroDivisor_UncheckedDefault_AgreesAcrossCarriers()
    {
        bool saved = ExceptionState.ArgumentFunctionChecking;
        ExceptionState.ArgumentFunctionChecking = false;
        try
        {
            Assert.Equal(0, (double)CobolIntrinsics.ModScaled(11, 0));
            Assert.Equal(0, CobolIntrinsics.ModReal(11d, 0d));
            Assert.Equal(0, (double)CobolIntrinsics.RemScaled(11, 0));
            Assert.Equal(0, CobolIntrinsics.RemReal(11d, 0d));
        }
        finally { ExceptionState.ArgumentFunctionChecking = saved; }
    }

    /// <summary>
    /// ⛔ THE GENERAL GUARD: no <c>…Real</c> body may answer a domain guard with a bare literal.
    /// </summary>
    /// <remarks>
    /// Every legitimately-guarded real body routes to a shared raise site — <c>TryIntegerArg</c> for the §15.3
    /// type-6 integer family, <c>ModZeroDivisor</c> / <c>RemZeroDivisor</c> for the two division rules. A body
    /// that instead writes <c>x == 0 ? 0 : …</c> has silently re-implemented an exception condition as a default
    /// value, which is precisely how MOD and REM drifted from their exact twins. The empty-argument
    /// <c>xs.Length == 0 ? 0</c> forms are NOT this defect and are excluded by name: a zero-argument variadic
    /// call cannot be written in COBOL (the general formats require at least one argument), so those are
    /// unreachable defensive returns rather than a rule being defaulted.
    /// <para>⚠ IT SCANS CODE ONLY, AND IT PROVED IT NEEDED TO BY FAILING ON ITS OWN FIRST RUN. The prose above
    /// and the remarks on <c>ModReal</c> both QUOTE the defective form <c>b == 0 ? 0 : …</c> so a later reader
    /// knows what was wrong; scanning raw file text therefore reported the fixed code as an offender. Comments
    /// are stripped before matching. A guard that cannot tell a defect from a description of a defect would have
    /// had to be silenced, and a silenced guard is the dead lookup this project keeps finding.</para>
    /// </remarks>
    /// <summary>The detector, so that both the guard below and its self-test use the SAME one — a self-test
    /// against a re-typed copy of the pattern proves nothing about the pattern actually shipped.</summary>
    private static List<string> DomainGuardOffenders(string source)
    {
        // Strip block and line comments (including `///` doc comments) — see the remark on the guard below.
        string src = Regex.Replace(Regex.Replace(source, @"/\*.*?\*/", "", RegexOptions.Singleline), @"//[^\n]*", "");
        // A ternary whose CONDITION tests an argument against zero and whose TRUE arm is a bare numeric literal.
        return [.. Regex.Matches(src, @"(?<cond>\b[A-Za-z_][A-Za-z0-9_]*\s*==\s*0)\s*\?\s*(?<then>-?\d+(?:\.\d+)?)\s*:")
            .Select(m => new { Cond = m.Groups["cond"].Value, Line = src[..m.Index].Count(c => c == '\n') + 1 })
            // `xs.Length == 0` is the unreachable-variadic guard, not a domain rule.
            .Where(o => !o.Cond.Contains("Length", StringComparison.Ordinal))
            .Select(o => $"line {o.Line}: `{o.Cond} ? …`")];
    }

    /// <summary>⛔ THE GUARD MUST FAIL ON THE DEFECT IT EXISTS FOR — asserted, not assumed
    /// (feedback_green_gates_arent_evidence). The first input is <c>ModReal</c> exactly as it was written before
    /// PB32; the second is the fixed form; the third is the excluded variadic guard; the fourth is a COMMENT
    /// describing the defect, which is what made the guard fail on its own first run.</summary>
    [Fact]
    public void TheDomainGuardDetector_CatchesTheDefectAndNothingElse()
    {
        Assert.Single(DomainGuardOffenders(
            "public static double ModReal(double a, double b) => b == 0 ? 0 : a - (b * Math.Floor(a / b));"));
        Assert.Empty(DomainGuardOffenders(
            "public static double ModReal(double a, double b) => b == 0 ? ModZeroDivisor() : a - b;"));
        Assert.Empty(DomainGuardOffenders(
            "public static double MaxReal(params double[] xs) => xs.Length == 0 ? 0 : xs.Max();"));
        Assert.Empty(DomainGuardOffenders("/// this body used to answer <c>b == 0 ? 0 : …</c>"));
    }

    [Fact]
    public void NoRealBody_AnswersADomainGuardWithALiteral()
    {
        var offenders = DomainGuardOffenders(
            File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "Intrinsics", "CobolIntrinsics.RealArgs.cs")));

        Assert.True(offenders.Count == 0,
            "A …Real body answers a domain guard with a literal instead of a shared raise site — its exact twin "
            + "raises an exception condition for the same input, so the two carriers disagree (fix-queue PB32). "
            + "Route it through the rule's ONE raise site, as ModZeroDivisor/RemZeroDivisor do. Offenders: "
            + string.Join(", ", offenders));
    }
}
