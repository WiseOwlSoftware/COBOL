// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Runtime.Exceptions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A RUN-TIME MESSAGE OF A COBOL-85 RUN CITES THE 1985 TEXT (kb/Work PB6023).
///
/// <para><b>The rule.</b> A breach the run time reports at COBOL-85 (an intrinsic argument outside its rule, a reference
/// modification out of range) names the rule of ANSI X3.23-1985 or of its 1989 intrinsic-function amendment that was broken
/// (§1989:A.2.14.3 AR1 for FACTORIAL, §1985:IV.4.3.8.4.4 GR4 for a reference modification), never a COBOL-2023 clause the
/// program's edition does not have. <c>ExceptionEngine.EditionRule</c> is the one place a run-time message chooses between the two
/// by the executing element's edition.</para>
///
/// <para><b>What went wrong.</b> PB6019 gave every breach one answer at 85 (termination) while about fifteen of the messages still
/// said <c>FACTORIAL argument -4 violates §2023:15.36.3 AR1</c> and a zero-length reference modification printed
/// <c>EC-BOUND-REF-MOD (fatal): ... (ISO §2023:8.4.3.3.4 item 5c)</c>: an exception-name and a clause COBOL-85 lacks.</para>
///
/// <para><b>The structure the test reads.</b> The functions COBOL-85 has are the catalog rows of the 1989 module
/// (<see cref="IntrinsicCatalog.Paragraph1989"/>), their COBOL-2023 sections are read off the 2023 text, and every raise statement
/// of the run time (an <c>ArgumentError</c> call or one of its twins, a <c>RefModViolation</c>) that cites the 2023 section of such a
/// function, §2023:15.3 (the general argument rules) or §2023:8.4.3.3.4 (reference modification) must also carry the 1985 rule:
/// through <c>EditionRule</c>, or as the other arm of the edition-parameter choice the date family makes (<c>unbounded</c>
/// choosing between a 1989 rule and a 2023 one). A statement that names only functions a later edition introduced cannot run at 85 and is not
/// read; the LOCALE forms (<c>CobolLocale.cs</c>, <c>CobolIntrinsics.NumvalLocale.cs</c>) are a 2002+ facility and are not
/// read either, nor is a rule a later edition ADDED to a 1989 function, which its <c>DialectBehaviors</c> row keeps from 85.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class RuntimeMessageEditionDriftTests
{
    private static readonly string[] RaiseCalls =
        ["ArgumentError(", "ArgumentErrorZeroLength(", "ArgumentErrorSpaces(", "RefModViolation(", "DigitCapExceeded("];

    private static readonly Regex EditionRuleCall = new(@"EditionRule\(\s*""[^""]*""\s*,\s*""[^""]*""\s*\)", RegexOptions.Compiled);
    private static readonly Regex Cite2023 = new(@"§2023:(?<s>\d+(?:\.\d+)*)", RegexOptions.Compiled);

    [Fact]
    public void AtCobol85_ARunTimeMessageCitesThe1985Rule()
    {
        var e = new ExceptionEngine { IsCobol85 = true };
        Assert.Equal("§1989:A.2.14.3 AR1", e.EditionRule("§1989:A.2.14.3 AR1", "§2023:15.36.3 AR1"));
        var x = Assert.Throws<CobolImplementorFatalException>(() => e.RefModViolation("reference modification (1:0) out of range"));
        Assert.DoesNotContain("EC-BOUND-REF-MOD", x.Message);
        var later = new ExceptionEngine();
        Assert.Equal("ISO §2023:15.84.3 AR2", later.EditionRule("§1989:A.2.40.3 AR2", "ISO §2023:15.84.3 AR2"));
        Assert.Equal("EC-BOUND-REF-MOD", Assert.Throws<CobolFatalException>(() => later.RefModViolation("x")).EcName);
    }

    [Fact]
    public void EveryRaiseOfA1989Rule_CarriesIts1985Citation()
    {
        string spec = File.ReadAllText(TestRepo.Specs("ISO_COBOL_2023.md"));
        var functions = SpecClauseText.FunctionSections(spec);
        var names1989 = functions.Where(f => IntrinsicCatalog.Paragraph1989(f.Name) is not null).Select(f => f.Name).ToHashSet(StringComparer.Ordinal);
        Assert.True(names1989.Count >= 40, $"only {names1989.Count} functions of the 1989 module found — the catalog or the §15 scan changed.");
        var sections1989 = functions.Where(f => names1989.Contains(f.Name)).Select(f => f.Section).ToList();
        var allNames = functions.Select(f => f.Name).OrderByDescending(n => n.Length).ToList();
        // A rule a later edition ADDED to a 1989 function is not asked at 85 (its DialectBehavior row gates it, e.g. NUMVAL-C's
        // argument-2 content rule, 15.68.3 AR2, from 2002), so its raise needs no 1985 rule: the rows name the rule's locator.
        var laterRules = DialectBehaviors.All.ToArray().Where(r => r.ChangedIn >= 2002)
            .Select(r => Regex.Match(r.SpecRef, @"^(?<s>15(?:\.\d+)+) AR(?<n>\d+)$")).Where(m => m.Success)
            .Select(m => (Section: m.Groups["s"].Value, Rule: m.Groups["n"].Value)).ToList();

        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src("WiseOwl.Cobol.Runtime"), "*.cs", SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(file);
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") || name.Contains("Locale", StringComparison.Ordinal))
                continue;
            string src = File.ReadAllText(file);
            foreach (string call in RaiseCalls)
            {
                for (int at = src.IndexOf(call, StringComparison.Ordinal); at >= 0; at = src.IndexOf(call, at + 1, StringComparison.Ordinal))
                {
                    int lineStart = src.LastIndexOf('\n', at) + 1;
                    string lead = src[lineStart..at].TrimStart();
                    if (lead.StartsWith("//", StringComparison.Ordinal) || lead.StartsWith("*", StringComparison.Ordinal)
                        || lead.Contains("static ", StringComparison.Ordinal) || lead.Contains("public ", StringComparison.Ordinal))
                        continue;   // a comment, or the member's own declaration
                    string statement = StatementAt(src, at);
                    if (!EditionRuleCall.IsMatch(statement)
                        && (statement.Contains("§1989:", StringComparison.Ordinal) || statement.Contains("§1985:", StringComparison.Ordinal)))
                        continue;   // the date family's edition-parameter arms carry both texts
                    string rest = EditionRuleCall.Replace(statement, "");
                    var named = allNames.Where(n => Regex.IsMatch(rest, $"(?<![A-Z0-9-]){Regex.Escape(n)}(?![A-Z0-9-])")).ToList();
                    if (named.Count > 0 && !named.Any(names1989.Contains)) continue;   // only later functions: never run at 85
                    foreach (Match m in Cite2023.Matches(rest))
                    {
                        string s = m.Groups["s"].Value;
                        string after = rest[(m.Index + m.Length)..];
                        if (laterRules.Any(l => l.Section == s && Regex.IsMatch(after, $@"^\s*(?:rule\s*|r|AR){l.Rule}\b")))
                            continue;   // a rule the edition gate keeps from 85
                        bool rule1989 = s == "15.3" || s.StartsWith("8.4.3.3.4", StringComparison.Ordinal)
                            || sections1989.Any(sec => s == sec || s.StartsWith(sec + ".", StringComparison.Ordinal));
                        if (!rule1989) continue;
                        int line = src[..at].Count(c => c == '\n') + 1;
                        offenders.Add($"{Path.GetRelativePath(TestRepo.Root, file)}:{line}  cites {m.Value} with no 1985 rule beside it");
                        break;
                    }
                }
            }
        }
        Assert.True(offenders.Count == 0,
            $"{offenders.Count} run-time message(s) of a rule COBOL-85 has cite only a later edition's clause:" + Environment.NewLine
            + string.Join(Environment.NewLine, offenders.Select(o => "    " + o)) + Environment.NewLine
            + "Cite it through ExceptionState.EditionRule(\"§1989:A.2.<n>.3 AR<k>\", \"<the 2023 rule as printed today>\") (the 1985 rule "
            + "checked by scripts/spec/cite.py --check). See kb/Work/PB6023.");
    }

    /// <summary>The raise statement that starts at <paramref name="at"/>: the text up to the first <c>;</c> outside a string
    /// literal. An interpolation hole's nested literals come in pairs, so a plain toggle on each unescaped quote keeps a
    /// <c>;</c> inside any of them inside.</summary>
    private static string StatementAt(string src, int at)
    {
        var sb = new StringBuilder();
        bool inString = false;
        for (int i = at; i < src.Length; i++)
        {
            char c = src[i];
            if (c == '\\' && inString && i + 1 < src.Length) { sb.Append(c).Append(src[++i]); continue; }
            if (!inString && c == '/' && i + 1 < src.Length && src[i + 1] == '/')
            {
                while (i < src.Length && src[i] != '\n') i++;   // a trailing comment is not the message
                continue;
            }
            if (c == '"') inString = !inString;
            else if (c == ';' && !inString) break;
            sb.Append(c);
        }
        return sb.ToString();
    }
}
