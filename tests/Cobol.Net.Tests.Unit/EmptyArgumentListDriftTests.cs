// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ NO VARIADIC INTRINSIC BODY INVENTS A VALUE FOR AN EMPTY ARGUMENT LIST (kb/Work PB257).
///
/// <para><b>The rule.</b> ISO <b>§15.3</b> — "The evaluation of an ALL subscript shall result in at least one
/// argument, otherwise the result of the reference to the function-identifier is undefined" — plus each
/// function's own <c>MinArgs</c>. A variadic statistical body therefore has NO defined answer over an empty
/// list, and the one place that says so is <c>CobolIntrinsics.RequireArguments</c>, which raises
/// EC-ARGUMENT-FUNCTION and terminates the reference.</para>
///
/// <para><b>What went wrong.</b> The two VARIANCE carriers DISAGREED about the case and neither was right:
/// <c>CobolIntrinsics.Variance</c> carried <c>if (xs.Length == 0) return 0;</c> — a silent value where the
/// standard defines none — while <c>VarianceDec</c> had no guard at all and would die inside <c>SumDec</c>'s
/// <c>xs[0]</c> with an <c>IndexOutOfRangeException</c>. The adjudication that let it stand reasoned from the
/// arity gate, which does NOT hold it: <c>TableAllPlace.StaticCount</c> is null for an <c>OCCURS … DEPENDING
/// ON</c> or DYNAMIC ALL level, so <c>FUNCTION VARIANCE(TBL(ALL))</c> passes arity with <c>given == 1</c>
/// whatever the run-time count is. The same pairwise disagreement ran through TEN functions — MAX, MIN, SUM,
/// RANGE, MEDIAN, MIDRANGE, MEAN, ORD-MAX, ORD-MIN and VARIANCE — one carrier answering a silent zero and the
/// other indexing past the end.</para>
///
/// <para>⚠ <b>What this file guards.</b> Two halves, so that "every body asserts the invariant" stays true
/// without a hand-maintained list of bodies (CLAUDE.md rule 5). <see cref="NoIntrinsicBody_InventsAValueForAnEmptyArgumentList"/>
/// bans the SHAPE — a variadic body testing its array for emptiness and answering anyway — structurally and
/// with no exemption list. <see cref="EveryVariadicIntrinsicBody_AssertsTheInvariantOrForwardsTheWholeArray"/>
/// is the positive half: every <c>params</c> array declaration in <c>src/Cobol.Net.Runtime/Intrinsics</c> either
/// calls <c>RequireArguments</c> itself or hands the WHOLE array to a body that does, which is what makes the
/// delegating bodies (STANDARD-DEVIATION over VARIANCE, MEAN over SUM, RANGE over MAX/MIN) correct by
/// construction rather than by exemption. Both read only lines that are not wholly comments, so this file's own
/// siblings may quote the shape they ban.</para>
/// </summary>
public sealed class EmptyArgumentListDriftTests
{
    /// <summary>A body that TESTS the argument array for emptiness and then answers — the pre-PB257 shape, in
    /// each form it appeared in: a guard statement, a ternary, an expression body.</summary>
    private static readonly Regex[] BannedEmptyAnswer =
    [
        new(@"\b\w+\.Length\s*==\s*0\s*\)\s*return\b", RegexOptions.Compiled),
        new(@"\b\w+\.Length\s*==\s*0\s*\?", RegexOptions.Compiled),
        new(@"\b\w+\.Length\s*!=\s*0\s*:", RegexOptions.Compiled),
    ];

    private static bool IsComment(string t) =>
        t.StartsWith("//", StringComparison.Ordinal) || t.StartsWith("*", StringComparison.Ordinal)
        || t.StartsWith("/*", StringComparison.Ordinal);

    /// <summary>True when <paramref name="line"/> answers an empty argument list instead of asserting the §15.3
    /// invariant. <c>source.Length == 0</c> and friends inside the TEXT functions are a different question — a
    /// zero-length ARGUMENT VALUE, which several §15 returned-value rules answer explicitly (§15.87.4 r1,
    /// §15.37.4 r5) — so the predicate fires only on an identifier named like an argument LIST.</summary>
    private static bool AnswersAnEmptyArgumentList(string line)
    {
        string t = line.Trim();
        if (IsComment(t)) return false;
        if (!Regex.IsMatch(t, @"\b(xs|args|amounts|values)\.Length\b")) return false;
        return BannedEmptyAnswer.Any(r => r.IsMatch(t));
    }

    private static IEnumerable<string> IntrinsicSources() =>
        Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Runtime", "Intrinsics"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    [Fact]
    public void NoIntrinsicBody_InventsAValueForAnEmptyArgumentList()
    {
        var offenders = new List<string>();
        foreach (string file in IntrinsicSources())
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (AnswersAnEmptyArgumentList(lines[i]))
                    offenders.Add($"{Path.GetRelativePath(TestRepo.Root, file)}:{i + 1}  {lines[i].Trim()}");
        }

        Assert.True(offenders.Count == 0,
            $"{offenders.Count} intrinsic body(ies) answer an EMPTY argument list instead of asserting the "
            + $"§15.3 at-least-one-argument invariant:{Environment.NewLine}"
            + string.Join(Environment.NewLine, offenders.Select(o => "    " + o))
            + $"{Environment.NewLine}The standard defines no value over an empty list, so a body that returns "
            + "one is a wrong answer its carrier's twin does not share. Call "
            + "CobolIntrinsics.RequireArguments(xs.Length, \"<FUNCTION>\") instead. See kb/Work/PB257.");
    }

    /// <summary>⛔ THE WATCHDOG MUST BE SHOWN TO BITE (feedback: a passing check proves nothing if it never
    /// looked at what changed). These are the VERBATIM pre-PB257 lines from both carriers, beside the
    /// zero-length ARGUMENT-VALUE tests of the text family, which are a different rule and must stay legal.</summary>
    [Fact]
    public void TheEmptyListDetector_FlagsTheShapesItExistsToBan_AndNothingElse()
    {
        string[] banned =
        [
            "        if (xs.Length == 0) return 0;                       // EC-ARGUMENT default",
            "    public static double MaxReal(params double[] xs) => xs.Length == 0 ? 0 : xs.Max();",
            "    public static double RangeReal(params double[] xs) => xs.Length == 0 ? 0 : xs.Max() - xs.Min();",
            "        xs.Length == 0 ? 0 : (xs.Max() + xs.Min()) / 2.0;",
            "        if (xs.Length == 0) return 0;",
        ];
        foreach (string line in banned)
            Assert.True(AnswersAnEmptyArgumentList(line), $"the detector missed a banned empty-list answer: {line}");

        string[] legal =
        [
            "        RequireArguments(xs.Length, \"VARIANCE\");",
            "        if (source.Length == 0 || froms.Any(f => f.Length == 0))",     // §15.87.4 r1 — a VALUE rule
            "        if (hay.Length == 0 || needle.Length == 0) return 0;",         // §15.37.4 r5 — a VALUE rule
            "        if (chars.Length == 0) return TrimOne(s, mode, ' ');",         // §15.96.3 r3's antecedent
            "        for (int i = 1; i < xs.Length; i++)",
            "        return CobolDec.Div(acc, CobolDec.From(xs.Length, 0), mode);",
            "    /// carried <c>if (xs.Length == 0) return 0;</c> — a silent value",
        ];
        foreach (string line in legal)
            Assert.False(AnswersAnEmptyArgumentList(line), $"the detector flagged a legal line: {line}");
    }

    /// <summary>The POSITIVE half, and it is a UNIFORM rule with NO exemption list: every variadic NUMERIC
    /// argument list in the intrinsic runtime asserts the §15.3 invariant AT ITS OWN ENTRY.</summary>
    /// <remarks><para>A delegating body is not exempt — RANGE over MAX/MIN, MEAN over SUM, MIDRANGE over
    /// MAX/MIN, STANDARD-DEVIATION over VARIANCE all assert it themselves. The assertion is one integer
    /// compare; an exemption is a list somebody has to maintain, and a list is exactly what CLAUDE.md rule 5
    /// says not to put where a structure belongs. The scope is the NUMERIC carriers — <c>double</c>,
    /// <c>CobolDec</c> and <c>Int128</c>, the three copies of the same ten statistical functions — because a
    /// variadic STRING list is a different question: §15.18 CONCAT and §15.96 TRIM have returned-value rules
    /// that define their own empty cases, and the string extremes reach the invariant through their one
    /// selection helper instead.</para></remarks>
    [Fact]
    public void EveryVariadicNumericIntrinsicBody_AssertsTheAtLeastOneArgumentInvariant()
    {
        var decl = new Regex(@"\bparams\s+(?:double|CobolDec|Int128)\[\]\s+(\w+)\s*\)", RegexOptions.Compiled);
        var anyDecl = new Regex(@"\bparams\s+[\w.<>]+\[\]\s+\w+\s*\)", RegexOptions.Compiled);
        var offenders = new List<string>();
        int measured = 0;
        foreach (string file in IntrinsicSources())
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].Trim();
                if (IsComment(t)) continue;
                var m = decl.Match(t);
                if (!m.Success) continue;
                string arr = m.Groups[1].Value;
                measured++;
                // The member's body: from this line to the next member's doc comment or variadic declaration.
                string body = "";
                for (int j = i; j < lines.Length; j++)
                {
                    string b = lines[j];
                    if (j > i && (b.StartsWith("    ///", StringComparison.Ordinal)
                                  || (anyDecl.IsMatch(b) && !IsComment(b.Trim())))) break;
                    body += b + "\n";
                }
                if (!body.Contains($"RequireArguments({arr}.Length", StringComparison.Ordinal))
                    offenders.Add($"{Path.GetRelativePath(TestRepo.Root, file)}:{i + 1}  {t}");
            }
        }

        // ⛔ A RUN MUST ASSERT ITS POPULATION: a scan that found no `params` declaration at all would report
        // "no offenders" about nothing (feedback_verdict_evidence_invariant). Three carriers × the ten
        // statistical functions plus the two PRESENT-VALUE bodies is thirty-odd; twenty is the floor.
        Assert.True(measured >= 20,
            $"only {measured} variadic numeric intrinsic body(ies) were measured — the scan found nothing to "
            + "check, which is not the same as finding nothing wrong");
        Assert.True(offenders.Count == 0,
            $"{offenders.Count} variadic numeric intrinsic body(ies) do not assert the §15.3 at-least-one-"
            + $"argument invariant at their own entry:{Environment.NewLine}"
            + string.Join(Environment.NewLine, offenders.Select(o => "    " + o))
            + $"{Environment.NewLine}Call CobolIntrinsics.RequireArguments(<array>.Length, \"<FUNCTION>\") as "
            + "the body's first statement — delegating bodies included, because a forwarding exemption is a "
            + "list and this rule has none. See kb/Work/PB257.");
    }
}
