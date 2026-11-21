// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.RepoDrift;

/// <summary>
/// ⛔ A ROW'S <c>EDITIONS AS ADJUDICATED</c> SENTENCE AND ITS <c>editions</c> FIELD AGREE (kb/Work PB672, PB659). A traceability-inventory
/// row states its edition reach twice: the <c>editions</c> field, which every per-edition view and burn-down reads, and the
/// adjudicator's <c>EDITIONS AS ADJUDICATED:</c> sentence in <c>notes</c>. The sentence is the adjudication and the field is derived
/// from it, so every edition the sentence names is in the field; a field narrower than its sentence hides a rule from an edition
/// it was adjudicated at (kb/Work PB672 measured five such rows). Every sentence is read by <see cref="SaidEditions"/>, which knows
/// the forms the rows write; a sentence it cannot read fails here with its row, so a new form is taught to the reader, never skipped.
/// </summary>
public sealed partial class InventoryEditionsSentenceDriftTests
{
    private const string Marker = "EDITIONS AS ADJUDICATED";

    [Fact]
    public void EveryEditionTheSentenceNames_IsInTheRowsEditionsField()
    {
        string dir = TestRepo.VersionMatrix("inventory");
        var wrong = new List<string>();
        int sentences = 0;
        foreach (string file in Directory.EnumerateFiles(dir, "*.json").Order(StringComparer.Ordinal))
        {
            using var doc = JsonDocument.Parse(File.ReadAllBytes(file));
            var row = doc.RootElement;
            string notes = row.TryGetProperty("notes", out var n) ? n.GetString() ?? "" : "";
            string field = row.TryGetProperty("editions", out var e) ? e.GetString() ?? "" : "";
            string id = row.GetProperty("rule-id").GetString()!;
            foreach (string sentence in SentencesOf(notes))
            {
                sentences++;
                var said = SaidEditions(sentence);
                if (said is null) { wrong.Add($"{id}: the sentence is in no form the reader knows: \"{Head(sentence)}\""); continue; }
                var reach = FieldEditions(field);
                if (!said.IsSubsetOf(reach))
                    wrong.Add($"{id}: the sentence names {string.Join(",", said.Order())} but the field is '{field}': \"{Head(sentence)}\"");
            }
        }
        Assert.True(sentences > 100, $"only {sentences} {Marker} sentences were read under {dir}");
        Assert.True(wrong.Count == 0, $"{wrong.Count} row(s) whose {Marker} sentence and editions field disagree:\n" + string.Join("\n", wrong));
    }

    /// <summary>The reader is total over the forms it knows and refuses the rest; a field narrower than its sentence is caught (the
    /// guard proven to fail on a deliberately narrowed field, kb/Work PB672).</summary>
    [Theory]
    [InlineData("1985/2002/2014/2023 (FORMAT 2).", "85,2002,2014,2023")]
    [InlineData("1985 . 2002 . 2014 . 2023", "85,2002,2014,2023")]
    [InlineData("2002 / 2014 / 2023 - the SHARING clause", "2002,2014,2023")]
    [InlineData("85 / 2002 / 2014 / 2023. The clause carries no edition marker", "85,2002,2014,2023")]
    [InlineData("2026-11-10: in force at 85, the CONTINUE statement", "85")]
    [InlineData("unchanged - 2002 - 2014 - 2023 (Annex A.4.7)", "2002,2014,2023")]
    [InlineData("unchanged from the prior verdict - 85,2002,2014,2023; the 2023 text", "85,2002,2014,2023")]
    [InlineData("widened from 2014,2023 to 2002,2014,2023. The previous adjudication", "2002,2014,2023")]
    [InlineData("GR3a/GR3b: 1985/2002/2014/2023. GR3c-g: 2002+ || editions", "85,2002,2014,2023")]
    [InlineData("the general format itself is present at all four editions; its lock phrases", "85,2002,2014,2023")]
    [InlineData("2014/2023 identical for every leg.", "2014,2023")]
    public void TheReader_ReadsEachFormTheRowsWrite(string sentence, string expected)
        => Assert.Equal(FieldEditions(expected), SaidEditions(sentence));

    [Fact]
    public void ANarrowedField_IsCaught_AndAnUnreadableSentence_IsRefused()
    {
        Assert.False(SaidEditions("1985/2002/2014/2023")!.IsSubsetOf(FieldEditions("2002,2014,2023")));
        Assert.Null(SaidEditions("as adjudicated before"));
    }

    /// <summary>Each sentence a row's notes carry after the marker (a row may carry several, one per adjudication).</summary>
    private static IEnumerable<string> SentencesOf(string notes)
    {
        for (int at = notes.IndexOf(Marker, StringComparison.Ordinal); at >= 0; at = notes.IndexOf(Marker, at + Marker.Length, StringComparison.Ordinal))
            yield return notes[(at + Marker.Length)..].TrimStart(':', ' ');
    }

    private static HashSet<int> FieldEditions(string field) =>
        [.. field.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Year)];

    private static int Year(string y) => y is "85" or "1985" ? 85 : int.Parse(y, System.Globalization.CultureInfo.InvariantCulture);

    private static readonly int[] All = [85, 2002, 2014, 2023];

    /// <summary>The editions the sentence names, or null when it is in no form the reader knows. The forms: a leading list
    /// (<c>1985/2002/2014/2023</c>, any of / . , - between, <c>85</c> or <c>1985</c>), optionally after <c>unchanged</c> or
    /// <c>unchanged from the prior verdict</c>; <c>widened from LIST to LIST</c> (the second); labelled parts
    /// (<c>GR3a/GR3b: LIST. GR3c-g: 2002+</c>, their union, <c>N+</c> being N and every later edition); the 1985 adjudications'
    /// <c>yyyy-mm-dd: in force at 85</c>; and <c>all four editions</c>.</summary>
    internal static HashSet<int>? SaidEditions(string sentence)
    {
        if (InForceAt85().IsMatch(sentence)) return [85];
        if (sentence.Contains("all four editions", StringComparison.Ordinal)) return [.. All];
        if (Widened().Match(sentence) is { Success: true } widened) return ListOf(widened.Groups["to"].Value);
        if (Lead().Match(sentence) is { Success: true } lead) return ListOf(lead.Groups["list"].Value);
        var parts = Labelled().Matches(sentence);
        if (parts.Count > 0 && parts[0].Index == 0)
        {
            HashSet<int> union = [];
            foreach (Match part in parts) union.UnionWith(ListOf(part.Groups["list"].Value));
            return union;
        }
        return null;
    }

    private static HashSet<int> ListOf(string list)
    {
        HashSet<int> said = [];
        foreach (Match m in Edition().Matches(list))
        {
            int y = Year(m.Groups["y"].Value);
            if (m.Groups["plus"].Success) said.UnionWith(All.Where(a => a >= y));
            else said.Add(y);
        }
        return said;
    }

    private static string Head(string sentence) => sentence.Length <= 120 ? sentence : sentence[..120] + "…";

    private const string Ed = @"(?:1985|85|2002|2014|2023)\+?";

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}:\s*in force at 85\b")]
    private static partial Regex InForceAt85();

    [GeneratedRegex(@"^widened from " + Ed + @"(?:\s*,\s*" + Ed + @")* to (?<to>" + Ed + @"(?:\s*,\s*" + Ed + @")*)")]
    private static partial Regex Widened();

    [GeneratedRegex(@"^(?:unchanged(?: from the prior verdict)?\s*[-–:]\s*)?(?<list>" + Ed + @"(?:\s*(?:/|\.|,|-|–)\s*" + Ed + @")*)(?!\d)")]
    private static partial Regex Lead();

    [GeneratedRegex(@"(?:^|\.\s+)[A-Z][\w/-]*:\s*(?<list>" + Ed + @"(?:\s*(?:/|,)\s*" + Ed + @")*)")]
    private static partial Regex Labelled();

    [GeneratedRegex(@"(?<y>1985|85|2002|2014|2023)(?<plus>\+)?")]
    private static partial Regex Edition();
}
