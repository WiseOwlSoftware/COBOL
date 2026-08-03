// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Keeps the FLOATING-POINT argument path complete for the exact intrinsic family (fix-queue PB2).
/// </summary>
/// <remarks>
/// <para>
/// A floating-point argument is legal for every exact-family intrinsic — §15.7.3 r1 and its siblings require
/// class NUMERIC, and a COMP-2 item is class numeric (§8.5.2.1 Table 2). Before PB2 the emitter dispatched on the
/// FUNCTION's family rather than the ARGUMENT's type and handed a <c>double</c> to an <c>Int128</c> parameter, so
/// legal COBOL produced a raw Roslyn <c>CS1503</c> that escaped as a backend error. Ten of eleven functions
/// probed did it.
/// </para>
/// <para>
/// The fix routes real-argument calls to a <c>…Real</c> body by CONVENTION rather than by a lookup table, and a
/// convention that is not asserted is a convention that is remembered — which is how the argument-class table
/// this file's sibling guards went dead in the first place. Adding an exact-family arm to the renderer without
/// its real counterpart would restore the CS1503 for that one function only, which is exactly the kind of hole
/// nothing else notices.
/// </para>
/// </remarks>
public sealed class IntrinsicRealArgDriftTests
{
    private static string RendererSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "IntrinsicRenderer.cs"));

    private static string RealArgsSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "Intrinsics", "CobolIntrinsics.RealArgs.cs"));

    private static string ExactSource() =>
        File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "Intrinsics", "CobolIntrinsics.Exact.cs"));

    /// <summary>The runtime methods the renderer's EXACT switch can reach, from its <c>case "…"</c> labels.</summary>
    private static HashSet<string> ExactArms()
    {
        string src = RendererSource();
        int from = src.IndexOf("switch (sig.RuntimeMethod)", StringComparison.Ordinal);
        Assert.True(from > 0, "IntrinsicRenderer no longer switches on sig.RuntimeMethod — this guard is blind");
        string body = src[from..];
        // ⛔ EVERY LABEL OF AN or-CHAIN, NOT JUST THE FIRST (fix-queue PB21 — the guard's SECOND blind spot).
        // This read `case "(?<m>[A-Za-z]+)"`, which anchors on the word `case`, so
        //     case "DateOfInteger" or "DayOfInteger" or "IntegerOfDate" or "IntegerOfDay":
        // contributed ONE name and hid three. That is how `IntegerOfDayReal` stayed missing while this test was
        // green: `FUNCTION INTEGER-OF-DAY(<COMP-2>)` bound clean and failed Roslyn with CS0117. The renderer
        // uses or-chained labels throughout, so the under-count was systematic rather than incidental.
        // A label list runs from `case` to `:` or to a `when` guard; each quoted name inside it is an arm.
        return [.. Regex.Matches(body, @"case\s+(?<lbl>""[A-Za-z]+""(?:\s+or\s+""[A-Za-z]+"")*)\s*(?::|when\b)")
            .SelectMany(m => Regex.Matches(m.Groups["lbl"].Value, @"""(?<n>[A-Za-z]+)""")
                                  .Select(x => x.Groups["n"].Value))];
    }

    private static HashSet<string> RealBodies() =>
        // Int128 is in the alternation because FactorialReal returns it (§15.36 — 33! needs the wide carrier).
        // Without it the guard would report FactorialReal missing while it sat two lines away.
        [.. Regex.Matches(RealArgsSource(), @"public static (?:double|long|Int128) (?<m>\w+)\(")
            .Select(m => m.Groups["m"].Value)];

    /// <summary>The documented transform, mirrored here so a change to it has to be deliberate.</summary>
    private static string RealName(string exact) =>
        exact.EndsWith("Scaled", StringComparison.Ordinal)
            ? string.Concat(exact.AsSpan(0, exact.Length - "Scaled".Length), "Real")
            : exact + "Real";

    /// <summary>The runtime methods of catalog rows declaring a general NUMERIC ('n') argument.</summary>
    /// <remarks>
    /// ⚠ This reads the catalog's <c>ArgKinds</c> hint column, which PB1 established is UNAUDITED — and that is
    /// acceptable HERE for a reason worth stating, because it would not be acceptable in the compiler. This use
    /// only SCOPES A TEST: a wrong hint leaves a gap in what is asserted, or asserts one body too many. PB1's use
    /// would have REJECTED SOURCE, where a wrong hint turns legal COBOL away. Same column, opposite blast radius.
    /// <para>
    /// ⛔ <b>'i' IS INCLUDED, AND ITS EXEMPTION WAS THE BUG (fix-queue PB21).</b> This used to scope on <c>'n'</c>
    /// alone, reasoning that "an integer-argument function ('i' — DATE-OF-INTEGER, FACTORIAL) … cannot receive a
    /// float without violating their own argument rule first". <b>That premise is false.</b> §15.3's INTEGER type
    /// resolves through CLASS NUMERIC — and a COMP-2 item is category numeric (§8.5.2.12 item 2) hence class
    /// numeric (§8.5.2.1 Table 2) — so <c>IntrinsicArgumentRules.Admissible('i')</c> is <c>[Numeric]</c> and
    /// ADMITS a float. The integer-ness is a VALUE property, checked at run time, not a class the screen can
    /// reject on. So an 'i' row can absolutely reach the renderer with a float, and did:
    /// <c>FUNCTION INTEGER-OF-DAY(&lt;COMP-2&gt;)</c> bound clean and then failed Roslyn with <b>CS0117</b>,
    /// because <c>IntegerOfDayReal</c> did not exist. The guard that existed to prevent exactly that outcome had
    /// exempted the group it happened in.
    /// </para>
    /// <para>
    /// The genuinely exempt kinds are the ones whose admissible set EXCLUDES class numeric: <c>'s'</c> (the string
    /// family — alphanumeric / numeric-edited / national) and <c>'b'</c> (boolean, PB19). Those reject a float at
    /// bind time, so no <c>…Real</c> body can be reached. That is why <c>IntegerOfBooleanReal</c> is deliberately
    /// NOT written: PB19's §15.45.3 r1 screen made it unreachable, and an unreachable body reads as coverage.
    /// </para>
    /// </remarks>
    private static HashSet<string> NumericArgumentMethods()
    {
        string catalog = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "IntrinsicCatalog.cs"));
        var rx = new Regex("Add\\(new\\(\"[A-Z0-9-]+\",\\s*IntrinsicType\\.\\w+,\\s*IntrinsicArity\\.\\w+,"
                           + "\\s*[-\\w]+,\\s*(?<max>[-\\w]+),\\s*\"(?<k>[a-z ]*)\",\\s*\"(?<rm>\\w*)\"");
        return [.. rx.Matches(catalog)
            // A zero-argument function can never receive a float — SECONDS-PAST-MIDNIGHT is 0/0 and its empty
            // ArgKinds would otherwise read as "numeric" through the empty-means-'n' default.
            .Where(m => m.Groups["max"].Value != "0")
            // 'n' AND 'i' — both resolve to Admissible == [CobolClass.Numeric], which admits a float item.
            .Where(m => m.Groups["k"].Value.Contains('n') || m.Groups["k"].Value.Contains('i')
                     || m.Groups["k"].Value.Length == 0)
            .Select(m => m.Groups["rm"].Value)
            .Where(s => s.Length > 0)];
    }

    /// <summary>
    /// ⛔ Every exact arm that can receive a general NUMERIC argument has a real-argument body — because that
    /// argument may be a COMP-2, and without the body the user gets a raw CS1503 from the generated C#.
    /// </summary>
    [Fact]
    public void EveryExactArm_HasItsRealCounterpart()
    {
        var real = RealBodies();
        var numericArg = NumericArgumentMethods();

        var missing = ExactArms()
            .Where(numericArg.Contains)
            // ⛔ THE ONE ARM ROUTED TO ITSELF RATHER THAN TO A ...Real BODY (PB21). RenderFloat wraps every
            // result in FromDouble(double, ws), so a ...Real body must return something a double can carry —
            // and §15.36's cannot: 33! is ~8.7e36, which is why the exact body returns Int128. Its arm consumes
            // the argument through IntArg, whose (long)(double) handles a float, so RenderNum skips the float
            // dispatch for it by name. Writing FactorialReal to satisfy the pattern would have meant returning a
            // double and losing exactness past 2^53 — the same argument-shape dependence PB13 closed.
            // ⚠ This exemption is NOT a free pass: the assertion below still fails if the RENDERER stops
            // skipping it, because then the missing body becomes reachable again.
            .Where(m => m != "Factorial" || !RendererSource().Contains("sig.RuntimeMethod != \"Factorial\""))
            .Where(m => !m.EndsWith("String", StringComparison.Ordinal))
            .Where(m => !real.Contains(RealName(m)))
            .Order()
            .ToList();

        Assert.True(missing.Count == 0,
            $"exact-family arm(s) with no floating-point counterpart: [{string.Join(", ", missing)}] — a real "
            + "argument to any of these emits a double into an Int128 parameter and the user sees a raw CS1503 "
            + "from the generated C# (fix-queue PB2). Add the body to CobolIntrinsics.RealArgs.cs, named "
            + $"[{string.Join(", ", missing.Select(RealName))}].");
    }

    /// <summary>
    /// ⛔ THE CS0121 TRAP, asserted so it cannot come back. The first attempt at PB2 gave the real bodies the SAME
    /// names as the exact ones, reasoning that <c>Int128</c> has no implicit conversion from <c>double</c>. It
    /// does not — but an integer LITERAL converts implicitly to BOTH, so <c>FUNCTION MAX(5 7)</c> emitted
    /// <c>MaxScaled(5, 7)</c> and C# reported an ambiguous call, breaking six corpus programs that never touched
    /// a float.
    /// </summary>
    [Fact]
    public void NoRealBody_SharesAnExactMethodName()
    {
        var exact = Regex.Matches(ExactSource(), @"public static (?:Int128|long|int) (?<m>\w+)\(")
            .Select(m => m.Groups["m"].Value).ToHashSet(StringComparer.Ordinal);

        var clashes = RealBodies().Where(exact.Contains).Order().ToList();
        Assert.True(clashes.Count == 0,
            $"real-argument body/bodies [{string.Join(", ", clashes)}] share a name with an exact-family method. "
            + "That is a CS0121 ambiguity waiting on the first integer literal argument, not an overload — the "
            + "real bodies must carry the …Real name.");
    }

    /// <summary>The renderer routes on the ARGUMENT's type, not only on the function's family.</summary>
    [Fact]
    public void TheRenderer_RoutesOnTheArgumentType()
    {
        string src = RendererSource();
        Assert.True(src.Contains("AnyRealArgument", StringComparison.Ordinal),
            "IntrinsicRenderer no longer asks whether any ARGUMENT is real — dispatching on sig.Float alone is "
            + "precisely the PB2 defect, and it returns as a raw CS1503 on legal COBOL.");
        Assert.True(src.Contains("RealMethod", StringComparison.Ordinal),
            "the exact→real method-name transform is gone from IntrinsicRenderer (fix-queue PB2).");
    }
}
