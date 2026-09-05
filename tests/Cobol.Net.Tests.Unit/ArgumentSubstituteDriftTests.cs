// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ NO INTRINSIC GUARD SPELLS ITS OWN SUBSTITUTED TEXT RESULT (kb/Work PB383, PB470).
///
/// <para><b>The rule.</b> ISO <b>§15.3 rule 14</b> — "If the EC-ARGUMENT-FUNCTION exception condition is set to
/// exist and checking for EC-ARGUMENT-FUNCTION is not enabled, the implementor defines the result of the function
/// reference" — hands the substituted result to the implementor, and <c>docs/CONFORMANCE.md</c> row
/// <c>DOC-A.1-90</c> writes the determination down ONCE. Its numeric half is
/// <c>ExceptionEngine.ArgumentError</c>'s own <c>return 0</c>; its TEXT half has exactly two classes, and both
/// live in <c>CobolNet.Runtime.Exceptions.ArgumentSubstitute</c>.</para>
///
/// <para><b>What went wrong, twice.</b> While the raise and its substitute were two separate statements, a guard
/// could pick its own answer and nobody could see the disagreement. <c>BooleanOfInteger</c> answered the
/// one-position boolean <c>"0"</c> where its sibling <c>BaseConvert</c> answered the zero-length value for the
/// SAME determination (PB383); <c>LOCALE-DATE</c>, <c>LOCALE-TIME</c> and <c>LOCALE-TIME-FROM-SECONDS</c>
/// answered a ZERO-LENGTH value where row DOC-A.1-90's own words give SPACES, at four guards (PB470). Both were
/// wrong answers in a user's program, and both were invisible through a MOVE (§14.6.8.5 space-fills the receiver
/// from a zero-length sender), so no golden that MOVEd the result could contradict them.</para>
///
/// <para>⚠ <b>What this file does and does not guard.</b> It bans the SHAPE — a substituted result spelled as a
/// literal at the guard — over <c>src/Cobol.Net.Runtime/Intrinsics</c>, structurally and with no exemption list:
/// a returned string literal made only of SPACES (the empty string included) is, in that folder, always one of
/// row DOC-A.1-90's two text classes and never a computed result. Literals that ARE results —
/// <c>BASECONVERT</c>'s <c>"0"</c>, the <c>"&lt;"</c>/<c>"="</c>/<c>"&gt;"</c> of the comparison functions — are
/// outside the predicate by construction rather than by exemption. It does NOT decide WHICH class a site owes;
/// that is the call site's cited derivation, measured behaviourally by
/// <c>2023/l1_argument_default_boolean_length</c> and <c>2014/pb470_locale_argument_substitute</c>. And it reads
/// only lines that are not wholly comments, so a doc comment may quote the banned shape (this one's siblings
/// do).</para>
/// </summary>
public sealed class ArgumentSubstituteDriftTests
{
    /// <summary>A <c>return</c> / lambda-body / ternary-arm whose whole value is a string literal of nothing but
    /// spaces — <c>return "";</c>, <c>return " ";</c>, <c>=&gt; "";</c>, a continuation line <c>? ""</c> or
    /// <c>: " "</c>. Every one of the twelve sites PB470 converted matched one of these.</summary>
    private static readonly Regex[] BannedSubstitute =
    [
        new(@"\breturn\s+""[ ]*""\s*[;,)]", RegexOptions.Compiled),
        new(@"=>\s*""[ ]*""\s*[;,)]", RegexOptions.Compiled),
        new(@"^[?:]\s*""[ ]*""\s*$", RegexOptions.Compiled),
    ];

    /// <summary>True when <paramref name="line"/> spells a substituted text result rather than reading its class
    /// from <c>ArgumentSubstitute</c>. Wholly-comment lines are skipped: the mechanism's own documentation quotes
    /// the shape it bans, and a comment cannot be a substituted value.</summary>
    private static bool SpellsASubstitute(string line)
    {
        string t = line.Trim();
        if (t.StartsWith("//", StringComparison.Ordinal) || t.StartsWith("*", StringComparison.Ordinal)
            || t.StartsWith("/*", StringComparison.Ordinal))
            return false;
        return BannedSubstitute.Any(r => r.IsMatch(t));
    }

    private static IEnumerable<string> IntrinsicSources() =>
        Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Runtime", "Intrinsics"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    [Fact]
    public void NoIntrinsicGuard_SpellsItsOwnSubstitutedTextResult()
    {
        var offenders = new List<string>();
        foreach (string file in IntrinsicSources())
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (SpellsASubstitute(lines[i]))
                    offenders.Add($"{Path.GetRelativePath(TestRepo.Root, file)}:{i + 1}  {lines[i].Trim()}");
        }

        Assert.True(offenders.Count == 0,
            $"{offenders.Count} intrinsic site(s) spell a substituted text result instead of reading its class "
            + $"from CobolNet.Runtime.Exceptions.ArgumentSubstitute:{Environment.NewLine}"
            + string.Join(Environment.NewLine, offenders.Select(o => "    " + o))
            + $"{Environment.NewLine}ISO §15.3 rule 14 hands the result to the implementor and docs/CONFORMANCE.md "
            + "row DOC-A.1-90 states it once. Raise AND substitute in ONE expression — "
            + "ExceptionState.ArgumentErrorZeroLength(detail) or ExceptionState.ArgumentErrorSpaces(detail, n) — "
            + "or, where a bool screening predicate already raised, ArgumentSubstitute.ZeroLength / "
            + "ArgumentSubstitute.Spaces(n). See kb/Work/PB383 and kb/Work/PB470.");
    }

    /// <summary>⛔ THE WATCHDOG MUST BE SHOWN TO BITE (feedback: a passing check proves nothing if it never
    /// looked at what changed). These are the VERBATIM pre-PB470 lines from the four LOCALE guards, the four
    /// CHAR / CHAR-NATIONAL guards and the FORMATTED-TIME ternary, beside the literals that are real results and
    /// must stay legal.</summary>
    [Fact]
    public void TheDetector_FlagsTheShapesItExistsToBan_AndNothingElse()
    {
        string[] banned =
        [
            "            return \" \";",                                   // CHAR / CHAR-NATIONAL, pre-PB470
            "            return \" \";                                      // EC default (§15.3)",
            "            return \"\";",                                     // LOCALE-DATE / LOCALE-TIME, pre-PB470
            "        if (SecondsOutOfStandardFormFor(\"LOCALE-TIME-FROM-SECONDS\", u, s, l)) return \"\";",
            "            ? \"\"",                                           // FORMATTED-TIME's ternary arm
            "            : \" \"",
            "    private static string F() => \"\";",
        ];
        foreach (string line in banned)
            Assert.True(SpellsASubstitute(line), $"the detector missed a banned substitute: {line}");

        string[] legal =
        [
            "        if (acc == 0) return \"0\";",                          // BASECONVERT's zero — a RESULT
            "            return \"=\";   // §14.6.13.1.3 #8, a different determination",
            "        return c < 0 ? \"<\" : c > 0 ? \">\" : \"=\";",        // §15.51.4 r5 / §15.86.4
            "            \"LOCALE-DATE\" => \"ISO §15.52.4 r1\",",          // a switch arm carrying a citation
            "                    sb.Append(run >= 2 ? des : des.Length > 0 ? des[..1] : \"\");",
            "            return ExceptionState.ArgumentErrorSpaces(detail, LocaleSubstitutePositions);",
            "            return Exceptions.ArgumentSubstitute.ZeroLength;",
            "    /// <para>⛔ Do NOT \"simplify\" this back to <c>return \"\";</c>.</para>",
        ];
        foreach (string line in legal)
            Assert.False(SpellsASubstitute(line), $"the detector flagged a legal line: {line}");
    }
}
