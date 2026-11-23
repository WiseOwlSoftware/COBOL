// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// An Annex A.1 item (a DOC row) documents a determination the standard leaves to the implementor, and its text ends in the
/// rules that leave it: <c>(14.9.27, OPEN statement, General rule 23)</c> (§2023:A.1). A documentation obligation cannot
/// exist before the rule that creates it, so the item's band starts no earlier than the earliest rule row it names
/// (docs/VERSION_TEST_MATRIX_DESIGN.md P2.10, kb/Work PB3320). A band that starts LATER is the adjudicator's reading (the item
/// names an arm or a facility the named rule's earlier text does not state, the rule's own band being the earliest the rule
/// is stated), so only this direction is mechanical.
/// </summary>
/// <remarks>
/// Only an item whose every reference resolves to rule rows is read: an item that names a whole clause (<c>8.1.2, Computer's
/// coded character set</c>) or a clause with no rule number carries no rule to compare, and one whose reference chains two
/// clauses (<c>9.1.13.2, ..., 12.4.5.10, ..., General rule 2</c>) is ambiguous, so neither is guessed.
/// </remarks>
public sealed class DocItemBandDriftTests
{
    /// <summary>The A.1 items whose band starts before a rule they name, each with the determination or edition text that
    /// justifies it: an item at an edition below its rule's is a recorded determination of the behaviour AT that edition. The
    /// scan must still flag each one without this entry (<see cref="EveryExemption_StillCarriesTheEarlierStartItExcuses"/>), so a
    /// stale entry fails.</summary>
    private static readonly Dictionary<string, string> Exempt = new(StringComparer.Ordinal)
    {
        ["DOC-A.1-64"] =
            "§2002:11.8.5.3 GR2 and its default paragraph state the entry convention's implementor-defined meaning and default at 2002; "
            + "the rule it names, §2023:11.9.7.4 GR2, is the 2014 text that adds the COBOL alternative's naming (kb/Work PB3322)",
        ["DOC-A.1-43"] =
            "docs/conformance/7/DOC-A.1-43.md determines the equivalence of a currency symbol outside the repertoire for every edition "
            + "that has the clause form, the bare single-character form from COBOL-85 (golden l1c06_currency_non_cobol_equivalence_85); "
            + "the rule it names, §2023:12.3.7.3 SR21, is the 2002 text on equivalent symbols of two clauses (kb/Work PB3320)",
        ["DOC-A.1-131"] =
            "the owner's determination of 2026-10-23, option (i) (kb/Work PB670): at --std 85 the implementor's own sharing mode restricts "
            + "nobody (golden pb670_cobol85_no_sharing_facility); the rule it names, §2023:14.9.27.4 GR23, is the 2002 sharing text "
            + "(DOC-A.1-77, the same determination, chains whole-clause references and is not read)",
    };

    /// <summary>The reference at the end of an item's text: the last parenthesis group, nested parentheses allowed.</summary>
    private static readonly Regex TrailingReference = new(@"\(((?:[^()]|\([^()]*\))*)\)\s*\.?\s*$", RegexOptions.Compiled);

    /// <summary>A segment's leading clause number: <c>14.9.27, OPEN statement, ...</c> or <c>Clause 7, ...</c>.</summary>
    private static readonly Regex SegmentClause =
        new(@"^\s*(?:and\s+)?(?:Clause\s+)?(\d+(?:\.\d+)*)\s*,", RegexOptions.Compiled);

    /// <summary>One rule mention: <c>General rules 1 and 14</c>, <c>Returned value, rule 2</c>, <c>General rule 3h2</c>.</summary>
    private static readonly Regex RuleMention = new(
        @"(general|syntax|returned value|argument)\s*,?\s*rules?\s*,?\s*((?:\d+[a-z]*\d*(?:\s*,\s*and\s+|\s*,\s*|\s+and\s+)?)+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DottedNumber = new(@"\d+\.\d+", RegexOptions.Compiled);
    private static readonly Regex LetterSuffix = new(@"(?<=\d)[a-z]+\d*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Digits = new(@"\d+", RegexOptions.Compiled);

    private static string KindOf(string word) => word.ToLowerInvariant() switch
    {
        "general" => "GR",
        "syntax" => "SR",
        "returned value" => "RV",
        _ => "AR",
    };

    /// <summary>The rule references an item's text names, as (clause, kind, rule number), or null when any part of the
    /// reference is not a clause with rule numbers.</summary>
    internal static List<(string Clause, string Kind, int Number)>? RuleReferences(string text)
    {
        if (TrailingReference.Match(text) is not { Success: true } reference)
            return null;
        var references = new List<(string, string, int)>();
        foreach (string segment in reference.Groups[1].Value.Split(';'))
        {
            if (SegmentClause.Match(segment) is not { Success: true } clause)
                return null;
            var mentions = RuleMention.Matches(segment);
            if (mentions.Count == 0)
                return null;
            // Every dotted number of the segment but its leading clause belongs to nothing this reads: a second clause
            // (a chained reference, or a trailing "and 8.5.1.5, Algebraic signs") makes the rule numbers ambiguous.
            string rest = SegmentClause.Replace(segment, "", 1);
            foreach (Match m in mentions)
                rest = rest.Replace(m.Value, "", StringComparison.Ordinal);
            if (DottedNumber.IsMatch(rest))
                return null;
            foreach (Match m in mentions)
                foreach (Match n in Digits.Matches(LetterSuffix.Replace(m.Groups[2].Value, "")))
                    references.Add((clause.Groups[1].Value, KindOf(m.Groups[1].Value),
                        int.Parse(n.Value, CultureInfo.InvariantCulture)));
        }
        return references;
    }

    private static int StartOf(string editions) =>
        int.Parse(editions.Split(',')[0], CultureInfo.InvariantCulture);

    /// <summary>The A.1 items whose band starts before the earliest rule row they name.</summary>
    /// <param name="rows">every inventory row</param>
    /// <param name="itemText">DOC row id → the item's text in the rule catalog</param>
    /// <param name="scanned">how many items resolved to rule rows and were compared</param>
    internal static List<string> EarlyItemBands(
        IReadOnlyList<TraceabilityInventory.Row> rows, IReadOnlyDictionary<string, string> itemText, out int scanned)
    {
        var rulesOf = rows.Where(r => r.Kind != "DOC" && r.Editions.Length > 0)
            .ToLookup(r => (r.Kind, r.Ordinal));
        var bad = new List<string>();
        scanned = 0;
        foreach (var item in rows.Where(r => r.Kind == "DOC" && r.Editions.Length > 0))
        {
            if (!itemText.TryGetValue(item.RuleId, out string? text) || RuleReferences(text) is not { } references)
                continue;
            var named = new List<TraceabilityInventory.Row>();
            bool resolved = true;
            foreach (var (clause, kind, number) in references)
            {
                var found = rulesOf[(kind, number)]
                    .Where(r => r.Section == clause || r.Section.StartsWith(clause + ".", StringComparison.Ordinal)).ToList();
                if (found.Count == 0)
                    resolved = false;
                named.AddRange(found);
            }
            if (!resolved)
                continue;
            scanned++;
            var earliest = named.MinBy(r => StartOf(r.Editions))!;
            if (StartOf(item.Editions) < StartOf(earliest.Editions))
                bad.Add($"{item.RuleId}: editions '{item.Editions}' starts before {earliest.RuleId} ('{earliest.Editions}'), a rule "
                        + "its A.1 text names: the obligation to document a determination cannot predate the rule that leaves it "
                        + "to the implementor (docs/VERSION_TEST_MATRIX_DESIGN.md P2.10; kb/Work PB3320)");
        }
        return bad;
    }

    private static Dictionary<string, string> LoadItemText()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestRepo.Docs("rearchitecture", "spec-rule-catalog.json")));
        return doc.RootElement.GetProperty("rules").EnumerateArray()
            .Where(r => r.GetProperty("kind").GetString() == "DOC")
            .ToDictionary(r => r.GetProperty("id").GetString()!, r => r.GetProperty("text").GetString()!, StringComparer.Ordinal);
    }

    /// <summary>Every A.1 item starts no earlier than the earliest rule it names (P2.10).</summary>
    [Fact]
    public void EveryDocItem_StartsNoEarlierThanTheRulesItNames()
    {
        var rows = TraceabilityInventory.Rows;
        var bad = EarlyItemBands(rows, LoadItemText(), out int scanned)
            .Where(b => !Exempt.Keys.Any(id => b.StartsWith(id + ":", StringComparison.Ordinal))).ToList();
        Assert.True(scanned >= 100,
            $"only {scanned} A.1 items resolve to rule rows — the item text's reference shape changed; fix RuleReferences, "
            + "do not lower the floor.");
        Assert.True(bad.Count == 0,
            $"{bad.Count} A.1 item(s) start before a rule they name, over {scanned} compared:{Environment.NewLine}    "
            + string.Join(Environment.NewLine + "    ", bad));
    }

    /// <summary>An exemption stays only while the scan still flags its item.</summary>
    [Fact]
    public void EveryExemption_StillCarriesTheEarlierStartItExcuses()
    {
        var flagged = EarlyItemBands(TraceabilityInventory.Rows, LoadItemText(), out _);
        foreach (string id in Exempt.Keys)
            Assert.True(flagged.Any(b => b.StartsWith(id + ":", StringComparison.Ordinal)),
                $"{id} is exempt from the A.1 band check but starts no earlier than the rules it names: delete its entry.");
    }

    /// <summary>The reference reader and the comparison, on rows of their own.</summary>
    [Fact]
    public void ReferenceReader_AndComparison_AreWhatTheyClaim()
    {
        var refs = RuleReferences("X. (14.9.4, CALL statement, General rule 3b)");
        Assert.Equal([("14.9.4", "GR", 3)], refs);
        Assert.Equal(
            [("7.2.3", "SR", 5), ("7.2.3", "GR", 3)],
            RuleReferences("X. (7.2.3, COPY statement, Syntax rule 5 and General rule 3)"));
        Assert.Equal(
            [("14.9.1", "GR", 1), ("14.9.1", "GR", 14)],
            RuleReferences("X. (14.9.1, ACCEPT statement, General rules 1 and 14)"));
        Assert.Equal([("15.15", "RV", 2)], RuleReferences("X. (15.15, CHAR function, Returned value, rule 2)"));
        Assert.Equal([("12.3.7", "GR", 7)], RuleReferences("X. (12.3.7, SPECIAL-NAMES paragraph, General rule 7c)"));
        // A whole clause, a chained reference and a trailing second clause name no one rule.
        Assert.Null(RuleReferences("X. (8.1.2, Computer's coded character set)"));
        Assert.Null(RuleReferences(
            "X. (9.1.13.2, Successful completion, I-O Status = 06, 12.4.5.10, ORGANIZATION clause, General rule 2)"));
        Assert.Null(RuleReferences("X. (13.18.52, SIGN clause, General rule 4 and 8.5.1.5, Algebraic signs)"));
        Assert.Null(RuleReferences("X. (7.3.9, CALL-CONVENTION directive, General rules 2a and 2b; 8.3.2.2, User-defined words)"));

        TraceabilityInventory.Row Row(string id, string kind, int ordinal, string section, string editions) =>
            new(id, section, kind, ordinal, "", "", "", "", "", editions, "");
        var rule = Row("GR-14.9.4.4-3", "GR", 3, "14.9.4.4", "2002,2014,2023");
        var text = new Dictionary<string, string> { ["DOC-A.1-1"] = "X. (14.9.4, CALL statement, General rule 3b)" };
        Assert.Contains("DOC-A.1-1", Assert.Single(EarlyItemBands(
            [rule, Row("DOC-A.1-1", "DOC", 1, "A.1", "85,2002,2014,2023")], text, out int scanned)));
        Assert.Equal(1, scanned);
        Assert.Empty(EarlyItemBands([rule, Row("DOC-A.1-1", "DOC", 1, "A.1", "2002,2014,2023")], text, out _));
        // The item may start later: that is the adjudicator's reading.
        Assert.Empty(EarlyItemBands([rule, Row("DOC-A.1-1", "DOC", 1, "A.1", "2014,2023")], text, out _));
        // A rule the inventory lacks leaves the item unread.
        Assert.Empty(EarlyItemBands([Row("DOC-A.1-1", "DOC", 1, "A.1", "85,2002,2014,2023")], text, out scanned));
        Assert.Equal(0, scanned);
    }
}
