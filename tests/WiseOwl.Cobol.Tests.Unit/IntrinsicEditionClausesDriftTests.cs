// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A DIAGNOSTIC ABOUT AN INTRINSIC FUNCTION'S ARGUMENT AT <c>--std 2002</c> OR <c>--std 2014</c> CITES THAT EDITION'S OWN CLAUSE, AND THIS READS EVERY
/// NUMBER BACK FROM THE STANDARD (kb/Work PB5922, the 2002 and 2014 directions of PB4950's decision). The argument rules are written as
/// §2023:15.n.3 clauses; <see cref="IntrinsicCitations.Clause"/> turns each into the function's own <c>15.m.2</c> clause of the edition in force
/// (<see cref="IntrinsicCatalog.Paragraph"/> numbers m by the function's place among the edition's functions, and
/// <see cref="IntrinsicCitations.Edition2002"/> / <see cref="IntrinsicCitations.Edition2014"/> renumber the rules an edition numbers differently).
/// The guards compare the derived numbers with the headings of the transcriptions, so a number is never a hand-kept list that drifts, and hold
/// every renumbering to a rule the edition prints.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class IntrinsicEditionClausesDriftTests
{

    private static readonly Regex FunctionHeading = new(@"^#{3,4} 15\.(?<n>\d+) (?<name>[A-Z0-9-]+) function\s*$", RegexOptions.Compiled);
    private static readonly Regex SubHeading = new(@"^#{4,6} 15\.(?<n>\d+)\.(?<sub>\d+) (?<title>.+?)\s*$", RegexOptions.Compiled);
    private static readonly Regex NumberedRule = new(@"^(?<n>\d+)\\\) ", RegexOptions.Compiled);

    private static string[] Lines(int year) => File.ReadAllLines(year switch
    {
        2002 => TestRepo.Specs("ISO_COBOL_2002.md"),
        2014 => TestRepo.Specs("ISO_COBOL_2014.md"),
        2023 => TestRepo.Specs("ISO_COBOL_2023.md"),
        _ => throw new ArgumentOutOfRangeException(nameof(year), year, "the clause numbers are read from the 2002, 2014 and 2023 transcriptions"),
    });

    /// <summary>The function clauses an edition prints: clause number → function.</summary>
    private static Dictionary<int, string> Headings(int year)
    {
        var headings = new Dictionary<int, string>();
        foreach (string line in Lines(year))
            if (FunctionHeading.Match(line) is { Success: true } m)
                headings[int.Parse(m.Groups["n"].Value)] = m.Groups["name"].Value;
        return headings;
    }

    /// <summary>How many numbered rules the argument subclause of each function clause prints (2002 and 2014 <c>15.m.2 Arguments</c>, 2023
    /// <c>15.n.3 Argument rules</c>): clause number → count; a function with no such subclause has no entry.</summary>
    private static Dictionary<int, int> ArgumentRuleCounts(int year)
    {
        string argumentsTitle = year == 2023 ? "Argument" : "Arguments";
        var counts = new Dictionary<int, int>();
        int? inClause = null;
        foreach (string line in Lines(year))
        {
            if (SubHeading.Match(line) is { Success: true } h)
            {
                bool arguments = int.Parse(h.Groups["sub"].Value) == (year == 2023 ? 3 : 2) && h.Groups["title"].Value.StartsWith(argumentsTitle, StringComparison.Ordinal);
                inClause = arguments ? int.Parse(h.Groups["n"].Value) : null;
                if (arguments) counts[inClause!.Value] = 0;
            }
            else if (line.StartsWith("#", StringComparison.Ordinal) && !line.StartsWith("## Page", StringComparison.Ordinal)) inClause = null;
            else if (inClause is { } n && NumberedRule.Match(line) is { Success: true } r)
                counts[n] = Math.Max(counts[n], int.Parse(r.Groups["n"].Value));
        }
        return counts;
    }

    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void EveryFunctionClauseOfTheEdition_HasTheNumberTheCatalogDerivesForIt(int year)
    {
        var headings = Headings(year);
        Assert.True(headings.Count >= 70, $"the transcription of {year} yields {headings.Count} function headings");
        var wrong = new List<string>();
        foreach (var (clause, name) in headings)
            if (IntrinsicCatalog.Paragraph(year, name) != clause)
                wrong.Add($"15.{clause} {name} function: the catalog says {IntrinsicCatalog.Paragraph(year, name)?.ToString() ?? "no clause"}");
        foreach (var (clause, name) in headings)
            if (IntrinsicCatalog.FunctionAt(year, clause) != name)
                wrong.Add($"15.{clause}: the catalog names {IntrinsicCatalog.FunctionAt(year, clause) ?? "no function"}, the standard {name}");
        Assert.True(wrong.Count == 0, $"IntrinsicCatalog.Paragraph disagrees with ISO/IEC 1989:{year}'s headings:\n" + string.Join("\n", wrong));
        Assert.Null(IntrinsicCatalog.FunctionAt(year, headings.Keys.Max() + 1));
    }

    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    public void EveryRenumberedRule_NamesARuleTheEditionPrints(int year)
    {
        var table = year == 2002 ? IntrinsicCitations.Edition2002 : IntrinsicCitations.Edition2014;
        var rules23 = ArgumentRuleCounts(2023);
        var rulesEd = ArgumentRuleCounts(year);
        var wrong = new List<string>();
        foreach (var ((function, rule), other) in table)
        {
            if (IntrinsicCatalog.Paragraph(2023, function) is not { } c23 || !rules23.TryGetValue(c23, out int n23) || rule > n23)
                wrong.Add($"{function} AR{rule}: the 2023 text prints no such rule");
            if (IntrinsicCatalog.Paragraph(year, function) is not { } cEd)
                wrong.Add($"{function}: {year} has no such function");
            else if (other is { } o && (!rulesEd.TryGetValue(cEd, out int nEd) || o > nEd))
                wrong.Add($"{function} AR{rule} -> AR{o}: §{year}:15.{cEd}.2 prints {(rulesEd.TryGetValue(cEd, out int k) ? k : 0)} rules");
        }
        Assert.True(wrong.Count == 0, $"the {year} renumbering table disagrees with the standard:\n" + string.Join("\n", wrong));
    }

    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    public void EveryArgumentRuleOf2023_HasACounterpartOrNoneIsSaid(int year)
    {
        // A rule absent from the table is the SAME rule under the same ordinal, so the edition shall print that ordinal: a function whose 2023
        // clause has more rules than the edition's must say which of them the edition lacks.
        var table = year == 2002 ? IntrinsicCitations.Edition2002 : IntrinsicCitations.Edition2014;
        var rules23 = ArgumentRuleCounts(2023);
        var rulesEd = ArgumentRuleCounts(year);
        var wrong = new List<string>();
        foreach (var (c23, name) in Headings(2023))
        {
            if (IntrinsicCatalog.Paragraph(year, name) is not { } cEd || !rules23.TryGetValue(c23, out int n23)) continue;
            rulesEd.TryGetValue(cEd, out int nEd);
            for (int rule = 1; rule <= n23; rule++)
                if (!table.ContainsKey((name, rule)) && rule > nEd)
                    wrong.Add($"{name} AR{rule}: §{year}:15.{cEd}.2 prints {nEd} rules and the table names no counterpart");
        }
        Assert.True(wrong.Count == 0, $"a 2023 argument rule that {year} cannot hold under its own ordinal:\n" + string.Join("\n", wrong));
    }

    [Fact]
    public void AnArgumentClause_CitesTheEditionInForce()
    {
        EditionInfo at2002 = EditionInfo.Of(2002), at2014 = EditionInfo.Of(2014), at2023 = EditionInfo.Of(2023);
        Assert.Equal("§2023:15.84.3 AR1", IntrinsicCitations.Clause(at2023, "SQRT", "§2023:15.84.3 AR1"));
        Assert.Equal("§2002:15.68.2 AR1", IntrinsicCitations.Clause(at2002, "SQRT", "§2023:15.84.3 AR1"));
        Assert.Equal("§2014:15.78.2 AR1", IntrinsicCitations.Clause(at2014, "SQRT", "§2023:15.84.3 AR1"));
        // A renumbered rule, a list of rules and a chain across two functions.
        Assert.Equal("§2002:15.55.2 AR4", IntrinsicCitations.Clause(at2002, "NUMVAL-C", "§2023:15.68.3 AR3"));
        Assert.Equal("§2014:15.63.2 AR4", IntrinsicCitations.Clause(at2014, "NUMVAL-C", "§2023:15.68.3 AR3"));
        Assert.Equal("§2014:15.63.2 AR1/AR2", IntrinsicCitations.Clause(at2014, "NUMVAL-C", "§2023:15.68.3 AR1/AR2"));
        Assert.Equal("§2002:15.55.2 AR4/AR6", IntrinsicCitations.Clause(at2002, "NUMVAL-C", "§2023:15.68.3 AR3/AR5"));
        // A rule the edition lacks is the whole arguments subclause; the general format is the clause's first subclause.
        Assert.Equal("§2002:15.47.2", IntrinsicCitations.Clause(at2002, "MAX", "§2023:15.59.3 AR3"));
        Assert.Equal("§2002:15.12.1", IntrinsicCitations.Clause(at2002, "BYTE-LENGTH", "§2023:15.14.2"));
        // A function the edition lacks keeps the 2023 clause and says so with the edition gate's phrase.
        string lacks = IntrinsicCitations.Clause(at2002, "TRIM", "§2023:15.96.3 AR2");
        Assert.StartsWith("§2023:15.96.3 AR2", lacks);
        Assert.Contains("(targeting COBOL-2002)", lacks);
        // The argument types and the integer type.
        Assert.Equal("§2002:15.2", IntrinsicCitations.ArgumentTypes(at2002));
        Assert.Equal("§2014:15.3 AR5", IntrinsicCitations.IntegerType(at2014));
        // A text that cites no argument clause throws at 2002 and 2014 and is untouched at 2023 (never a silent 2023 citation at 2002).
        Assert.Throws<ArgumentException>(() => IntrinsicCitations.Clause(at2002, "SQRT", "no clause here"));
        Assert.Equal("no clause here", IntrinsicCitations.Clause(at2023, "SQRT", "no clause here"));
    }
}
