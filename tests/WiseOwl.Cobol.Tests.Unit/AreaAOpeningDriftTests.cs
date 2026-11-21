// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EVERY INTEGER OPERAND YIELDS TO THE NEXT ENTRY'S LEVEL-NUMBER (kb/Work PB6011). In the COBOL-85 reference format an
/// integer that begins its fixed-form line in area A inside the data division is a level-number (§1985:IV.7.4: "Each new
/// data description entry may begin any number of positions to the right of margin A"; an entry continued on a later line
/// goes on in area B, §1985:IV.7.3.3), so no clause of the entry before it may take it as an operand: the parser asks
/// <c>CobolParserCoreBase.nextEntryOpensHere</c> at the left edge of every grammar alternative that BEGINS with an
/// INTEGERLIT, and a separator period forgotten before the level-number is then reported once, where it is missing. A new
/// alternative that takes an integer without the predicate would quietly read the next entry's level-number as an operand
/// again (<c>VALUE "A" 01 B</c>), so this scrape holds the inventory: every such alternative carries the predicate, and the
/// rules holding one are exactly the reviewed set, so a new one fails until it is decided here.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class AreaAOpeningDriftTests
{
    /// <summary>The rules whose INTEGERLIT-led alternatives are operands, each guarded.</summary>
    private static readonly string[] GuardedRules = ["integerLiteral", "numericLiteralCore", "residueInteger", "signedIntegerLiteral"];

    /// <summary>The rules that take an INTEGERLIT that is no operand of a data entry, and why the predicate is not theirs:
    /// <c>levelNumber</c> IS the level-number the predicate yields to; <c>procedureName</c> is a paragraph-name or section-name
    /// of digits, which stands in the procedure division, where the predicate is false.</summary>
    private static readonly string[] ExemptRules = ["levelNumber", "procedureName"];

    private const string Guard = "!nextEntryOpensHere()";

    private static string Body(string path) =>
        Regex.Replace(File.ReadAllText(path), @"//[^\r\n]*", string.Empty);

    /// <summary>The top-level alternatives of a rule body: split on the <c>|</c> outside every parenthesis and brace.</summary>
    private static IEnumerable<string> Alternatives(string body)
    {
        int depth = 0, start = 0;
        for (int i = 0; i < body.Length; i++)
        {
            switch (body[i])
            {
                case '(' or '{': depth++; break;
                case ')' or '}': depth--; break;
                case '|' when depth == 0:
                    yield return body[start..i];
                    start = i + 1;
                    break;
            }
        }
        yield return body[start..];
    }

    /// <summary>Does the alternative BEGIN with an INTEGERLIT: before its first one stand only predicates, an optional sign and
    /// element labels?</summary>
    private static bool LeadsWithAnInteger(string alternative)
    {
        var m = Regex.Match(alternative, @"\bINTEGERLIT\b");
        if (!m.Success) return false;
        string before = alternative[..m.Index];
        before = Regex.Replace(before, @"\{[^{}]*\}\?", " ");
        before = Regex.Replace(before, @"\(\s*PLUS\s*\|\s*MINUS\s*\)\?", " ");
        before = Regex.Replace(before, @"\b\w+\s*=\s*$", " ");
        return before.Trim().Length == 0;
    }

    [Fact]
    public void EveryAlternativeThatBeginsWithAnInteger_YieldsToTheNextEntrysLevelNumber()
    {
        var guarded = new SortedSet<string>(StringComparer.Ordinal);
        var unguarded = new List<string>();
        var holders = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.GetFiles(TestRepo.Src("WiseOwl.Cobol.Frontend", "Grammar"), "*.g4", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file) == "CobolLexer.g4") continue;
            foreach (Match rule in Regex.Matches(Body(file), @"^(?<name>[a-z]\w*)\s*:(?<body>.*?)^\s*;",
                         RegexOptions.Multiline | RegexOptions.Singleline))
            {
                string name = rule.Groups["name"].Value;
                string body = rule.Groups["body"].Value;
                if (!Regex.IsMatch(body, @"\bINTEGERLIT\b")) continue;
                holders.Add(name);
                if (ExemptRules.Contains(name)) continue;
                foreach (string alternative in Alternatives(body).Where(LeadsWithAnInteger))
                {
                    if (alternative.Contains(Guard, StringComparison.Ordinal)) guarded.Add(name);
                    else unguarded.Add($"{name}: {Regex.Replace(alternative.Trim(), @"\s+", " ")}");
                }
            }
        }
        Assert.True(unguarded.Count == 0, "an alternative begins with an INTEGERLIT and does not ask "
            + $"{{{Guard}}}?, so it would read the next entry's level-number as an operand (kb/Work PB6011):\n"
            + string.Join("\n", unguarded));
        Assert.Equal(string.Join(" ", GuardedRules), string.Join(" ", guarded));
        // A scrape that found nothing cannot pass: the exempt rules must still be found holding their integer.
        Assert.True(ExemptRules.All(holders.Contains), "the scrape no longer finds the exempt rules: it is blind");
    }
}
