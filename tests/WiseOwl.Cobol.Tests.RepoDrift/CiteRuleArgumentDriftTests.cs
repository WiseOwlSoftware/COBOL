// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ A RULE CITATION NAMES A RULE AT EVERY EDITION: NO CALL OF <c>EditionInfo.CiteRule</c> PASSES AN EMPTY EDITION TEXT (kb/Work
/// PB6175, PB4950).
/// <para><c>CiteRule(y1985, later, y2002, y2014)</c> prints the rule of the edition compiled at, so an empty argument prints an
/// empty citation, <c>()</c>, at that edition: the >>TURN repetition messages did so at <c>--std 85</c>. Where an edition has no
/// such rule because it lacks the construct, the caller asks <c>EditionInfo.CiteRuleFrom2002</c> (the introducing edition's
/// rule stands for COBOL-85, whose construct gate refuses the construct) or, as the >>TURN stage does below COBOL-2002, does
/// not report the message at all. <c>EditionInfo.Cite</c>, which chooses message TEXT, may still choose an empty phrase.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class CiteRuleArgumentDriftTests
{
    private static readonly TreeScope Sources = new(Under: ["src/"], Kinds: [".cs"]);

    private static readonly Regex Call = new(@"\bCiteRule(?:From2002)?\(", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>The arguments of the call whose opening parenthesis ends at <paramref name="open"/>, as written (a named
    /// argument keeps its name): split at the commas of its own depth, past string literals and nested brackets.</summary>
    internal static List<string> ArgumentsAt(string text, int open)
    {
        var args = new List<string>();
        int depth = 0, start = open;
        for (int i = open; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '"')
            {
                for (i++; i < text.Length && text[i] != '"'; i++)
                    if (text[i] == '\\') i++;
                continue;
            }
            if (c is '(' or '[' or '{') depth++;
            else if (c is ')' or ']' or '}')
            {
                if (depth-- == 0)
                {
                    args.Add(text[start..i].Trim());
                    return args;
                }
            }
            else if (c == ',' && depth == 0)
            {
                args.Add(text[start..i].Trim());
                start = i + 1;
            }
        }
        return args;
    }

    /// <summary>Whether a written argument is an empty text: <c>""</c>, <c>$""</c> or <c>string.Empty</c>, named or not.</summary>
    internal static bool IsEmptyText(string argument)
    {
        string value = Regex.Replace(argument, @"^\w+\s*:\s*", "");
        return value is "\"\"" or "$\"\"" or "string.Empty" or "String.Empty";
    }

    /// <summary>Each call in <paramref name="text"/> with an empty edition text, as its line number.</summary>
    internal static IEnumerable<int> EmptyArgumentCalls(string text)
    {
        foreach (Match m in Call.Matches(text))
            if (ArgumentsAt(text, m.Index + m.Length).Any(IsEmptyText))
                yield return text.AsSpan(0, m.Index).Count('\n') + 1;
    }

    [Fact]
    public void NoCiteRuleCall_PassesAnEmptyEditionText()
    {
        var files = TrackedTree.Files(Sources).Where(f => !f.EndsWith(".g.cs", StringComparison.Ordinal)).ToList();
        Assert.True(files.Count > 500, $"the source listing found only {files.Count} C# files; it is broken");
        int calls = 0;
        var found = new List<string>();
        foreach (string f in files)
        {
            string text = File.ReadAllText(Path.Combine(TestRepo.Root, f));
            calls += Call.Matches(text).Count;
            found.AddRange(EmptyArgumentCalls(text).Select(line => $"{f}:{line}"));
        }
        Assert.True(calls > 100, $"only {calls} CiteRule calls found; the scan is broken");
        Assert.True(found.Count == 0, "a CiteRule call passes an empty edition text, which prints an empty citation at that edition; "
            + "cite the edition's own rule, ask CiteRuleFrom2002 for a construct COBOL-85 lacks, or do not report the message where the "
            + "edition has no such rule (kb/Work PB6175):\n  " + string.Join("\n  ", found));
    }

    [Theory]
    [InlineData("edition.CiteRule(\"\", \"§2023:7.3.25.3 SR3\", \"§2002:7.2.19.2 SR4\")", 1)]
    [InlineData("edition.CiteRule(\"§1985:X.5.1 GR4\", \"§2023:13.7.3 SR4\", y2002: \"\")", 1)]
    [InlineData("e.CiteRuleFrom2002(\"§2002:7.2.19.2 SR3\", $\"§2023:{c} SR{n}\", y2014: string.Empty)", 1)]
    [InlineData("edition.CiteRule(F(\"\", 1), \"§2023:13.7.3 SR4\")", 0)]   // an empty text inside a nested call is not an argument
    [InlineData("edition.CiteRule(\"§1985:X.5.1 GR4\", \"a, b (\\\"\\\")\")", 0)]   // a comma and quotes inside a literal
    [InlineData("edition.Cite(\"\", \", which is a qualified-data-name\")", 0)]   // Cite chooses text and may choose none
    public void TheScan_SeesAnEmptyArgument_AndNothingElse(string code, int expected) =>
        Assert.Equal(expected, EmptyArgumentCalls(code).Count());
}
