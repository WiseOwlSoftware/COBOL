// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
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

    // ── THE INTAKE WIDTH IS THE RUNTIME BODY'S DECLARED CARRIER (kb/Work PB254) ────────────────────────────────
    //
    // PB22 landed ONE narrowing — CobolIntrinsics.IntegerArg — in front of every §15 integer argument, and it is
    // right for a PARTIAL function: §15.5.2's date forms and FACTORIAL's unrepresentable results genuinely make
    // an out-of-range argument "an incorrect value … according to the rules specified in the function
    // definition" (§15.3), so raising EC-ARGUMENT-FUNCTION there is the standard's own answer. It swept up the
    // TOTAL functions too. §15.90.3 r1 / §15.91.3 r1 say only "Argument-1 shall be an integer" and §15.90.4 r1a
    // / §15.91.4 r1a are CATCH-ALLS; §15.37.3 r3 says only "argument-3 shall be an integer data item or integer
    // literal" and §15.37.4 r2/r3 answer for every integer. For those three the landing MANUFACTURED an
    // exception condition — fatal under checking, and with checking off it substituted 0, which reported
    // TEST-DATE-YYYYMMDD(1.0E19) as a VALID DATE and FIND-STRING(… START AFTER 9999999999999999999) as a match
    // at position 1 where rule 3 requires zero.
    //
    // ⚠ THE PAIRING IS NOT A LIST KEPT HERE. Totality is declared ONCE, by the runtime body's parameter type —
    // Int128 for a total argument, long for one the argument rules bound — and this guard re-derives the pairing
    // from the shipped signatures and fails when an arm disagrees with its body. Widening a body is therefore
    // enough to force its arm wide; forgetting the arm is red, not silent.

    /// <summary>Every runtime intrinsic body whose signature makes the argument↔parameter correspondence
    /// UNAMBIGUOUS — exactly one <c>long</c>/<c>Int128</c> parameter, agreed across every overload — mapped to
    /// whether that carrier is the WIDE one. A body with two or more integer carriers (BOOLEAN-OF-INTEGER's
    /// value+length, the windowing trio's date+offset+base-year, MOD/REM's numeric pair) is deliberately not
    /// checked: the renderer's emitted argument list is not this test's to parse position by position, and a
    /// guard that guessed the correspondence would be the dead lookup this project keeps finding.</summary>
    private static Dictionary<string, bool> SingleCarrierIntrinsicBodies()
    {
        var byName = new Dictionary<string, List<Type>>(StringComparer.Ordinal);
        var ambiguous = new HashSet<string>(StringComparer.Ordinal);
        foreach (Type t in typeof(CobolIntrinsics).Assembly.GetExportedTypes())
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var carriers = m.GetParameters().Select(p => p.ParameterType)
                    .Where(p => p == typeof(long) || p == typeof(Int128)).ToList();
                if (carriers.Count == 0) continue;                          // no integer carrier — nothing to pair
                if (carriers.Count > 1) { ambiguous.Add(m.Name); continue; } // correspondence not derivable here
                if (!byName.TryGetValue(m.Name, out var seen)) byName[m.Name] = seen = [];
                seen.Add(carriers[0]);
            }
        return byName
            .Where(kv => !ambiguous.Contains(kv.Key) && kv.Value.Distinct().Count() == 1)
            .ToDictionary(kv => kv.Key, kv => kv.Value[0] == typeof(Int128), StringComparer.Ordinal);
    }

    /// <summary>An arm head in either switch shape the renderer uses — the statement form
    /// <c>case "A" or "B":</c> and the expression form <c>"A" or "B" =&gt;</c>, with or without a
    /// <c>when</c> clause. Anchoring on the head (rather than on the intake call) is what lets the guard say
    /// WHICH function is mis-rendered.</summary>
    private static readonly Regex ArmHead = new(
        """(?:case\s+)?"(?<n>[A-Za-z][A-Za-z0-9]*)"(?<more>(?:\s+or\s+"[A-Za-z][A-Za-z0-9]*")*)\s*(?:when\b[^\n]*?)?(?::|=>)""",
        RegexOptions.Compiled);

    private static readonly Regex NarrowIntake = new(@"\b(?:IntArg|ArgInt)\(", RegexOptions.Compiled);
    private static readonly Regex WideIntake = new(@"\b(?:IntArgWide|ArgIntWide)\(", RegexOptions.Compiled);

    /// <summary>The detector, shared by the guard and its self-test. Each arm chunk runs from its head to the
    /// next head; an arm that renders no integer intake at all is not this rule's business.</summary>
    private static List<string> IntakeWidthOffenders(string rendererSource, IReadOnlyDictionary<string, bool> wide)
    {
        // Comments are stripped for the same reason the PB32 guard strips them: the remarks on IntArg and
        // AsIntWide NAME both helpers, so raw text would report the documentation as the defect.
        string src = Regex.Replace(Regex.Replace(rendererSource, @"/\*.*?\*/", "", RegexOptions.Singleline), @"//[^\n]*", "");
        var heads = ArmHead.Matches(src).ToList();
        var offenders = new List<string>();
        for (int k = 0; k < heads.Count; k++)
        {
            int end = k + 1 < heads.Count ? heads[k + 1].Index : src.Length;
            string arm = src[heads[k].Index..end];
            bool narrow = NarrowIntake.IsMatch(arm), broad = WideIntake.IsMatch(arm);
            if (!narrow && !broad) continue;                                // renders no integer argument
            foreach (string name in Names(heads[k]))
            {
                if (!wide.TryGetValue(name, out bool wantWide)) continue;    // no unambiguous body to pair with
                if (wantWide && (!broad || narrow))
                    offenders.Add($"{name}: body declares Int128 (a TOTAL §15 integer argument) but the arm narrows");
                else if (!wantWide && broad)
                    offenders.Add($"{name}: body declares long (a BOUNDED argument) but the arm takes the wide intake");
            }
        }
        return offenders;

        static IEnumerable<string> Names(Match m) =>
            [m.Groups["n"].Value, .. Regex.Matches(m.Groups["more"].Value, "\"([A-Za-z][A-Za-z0-9]*)\"")
                                          .Select(x => x.Groups[1].Value)];
    }

    /// <summary>⛔ THE GUARD MUST FAIL ON THE DEFECT IT EXISTS FOR (feedback_green_gates_arent_evidence). Input
    /// one is the PB254 arm exactly as it was written — both TEST- validators narrowed — and the guard has to
    /// name BOTH functions, because a multi-name arm head is how the pair hid behind one line. Input two is the
    /// landed form. Input three is the opposite drift: a wide intake in front of a bounded body. Input four is a
    /// COMMENT naming the narrow helper, which is what made the sibling PB32 guard fail on its own first run.</summary>
    [Fact]
    public void TheIntakeWidthDetector_CatchesTheDefectAndNothingElse()
    {
        var wide = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["TestDateYyyymmdd"] = true, ["TestDayYyyyddd"] = true, ["Factorial"] = false,
        };
        Assert.Equal(2, IntakeWidthOffenders(
            """case "TestDateYyyymmdd" or "TestDayYyyyddd": return DateFn(m, IntArg(ic, 0));""", wide).Count);
        Assert.Empty(IntakeWidthOffenders(
            """case "TestDateYyyymmdd" or "TestDayYyyyddd": return DateFn(m, IntArgWide(ic, 0));""", wide));
        Assert.Single(IntakeWidthOffenders("""case "Factorial": return Intrinsic(m, IntArgWide(ic, 0));""", wide));
        Assert.Empty(IntakeWidthOffenders(
            """/// this arm used to read case "TestDateYyyymmdd": … IntArg(ic, 0)""", wide));
    }

    /// <summary>The guard proper, over the shipped renderer and the shipped runtime signatures.</summary>
    [Fact]
    public void EveryTotalIntegerArgument_TakesTheWideIntake()
    {
        var wide = SingleCarrierIntrinsicBodies();
        // The guard is worthless if it pairs nothing: assert the population it actually examined (PB254's own
        // three functions are the ones that must be in it).
        Assert.True(wide.GetValueOrDefault("TestDateYyyymmdd"), "TestDateYyyymmdd is not paired as a wide body");
        Assert.True(wide.GetValueOrDefault("TestDayYyyyddd"), "TestDayYyyyddd is not paired as a wide body");
        Assert.True(wide.GetValueOrDefault("FindString"), "FindString is not paired as a wide body");
        Assert.False(wide.GetValueOrDefault("Factorial", true), "Factorial is not paired as a narrow body");

        var offenders = IntakeWidthOffenders(
            File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "IntrinsicRenderer.cs")), wide);

        Assert.True(offenders.Count == 0,
            "A renderer arm's integer intake disagrees with the runtime body's declared carrier (kb/Work PB254). "
            + "The carrier IS the totality claim: Int128 means the function's argument rules place no constraint "
            + "on the VALUE, so §15.3 has no incorrect argument to raise on and the arm must use IntArgWide / "
            + "ArgIntWide; long means the argument rules bound it, so the arm must use IntArg / ArgInt and keep "
            + "the §15.3 landing. Offenders: " + string.Join("; ", offenders));
    }

    /// <summary>The behaviour the carrier exists for, asserted end to end on the runtime bodies: the three total
    /// arguments answer their spec verdict past <c>long</c>, from BOTH carriers, and raise nothing even with
    /// EC-ARGUMENT-FUNCTION checking ENABLED — the leg that used to abort the run unit on conforming source.</summary>
    [Fact]
    public void TheTotalArguments_AnswerPastLong_AndRaiseNothingUnderChecking()
    {
        Int128 past = Int128.Parse("12345678901234567890");                 // 20 digits — beyond long.MaxValue
        UnderChecking(() =>
        {
            Assert.Equal(1, CobolDate.TestDateYyyymmdd(past));               // §15.90.4 r1a — > 99 999 999
            Assert.Equal(1, CobolDate.TestDayYyyyddd(past));                 // §15.91.4 r1a — > 9 999 999
            Assert.Equal(1, CobolDate.TestDateYyyymmdd(-past));              // r1a's LOWER half
            Assert.Equal(1, CobolDate.TestDayYyyyddd(-past));
            Assert.Equal(1, CobolIntrinsics.TestDateYyyymmddReal(1e19));     // the binary64 carrier — was 0, "valid"
            Assert.Equal(1, CobolIntrinsics.TestDayYyyydddReal(1e19));
            Assert.Equal(1, CobolIntrinsics.TestDateYyyymmddReal(double.PositiveInfinity));
            Assert.Equal(1, CobolIntrinsics.TestDayYyyydddReal(double.NegativeInfinity));
            // §15.37.4 r3 — argument-3 ignores every match, so none is left and the answer is 0, not a position.
            Assert.Equal(0, CobolIntrinsics.FindString("abcabc", "abc", false, past, false));
            Assert.Equal(0, CobolIntrinsics.FindString("abcabc", "abc", true, past, false));
        });
        // The in-window verdicts are unchanged by the widening (the r1a guard makes the int narrowing exact).
        Assert.Equal(0, CobolDate.TestDateYyyymmdd(20240229));
        Assert.Equal(3, CobolDate.TestDateYyyymmdd(20230229));
        Assert.Equal(0, CobolDate.TestDayYyyyddd(2024366));
        Assert.Equal(2, CobolDate.TestDayYyyyddd(2023366));
        Assert.Equal(4, CobolIntrinsics.FindString("abcabc", "abc", false, 1, false));
    }
}
