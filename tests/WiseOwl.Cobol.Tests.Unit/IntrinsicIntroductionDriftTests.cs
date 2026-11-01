// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE EDITION EACH INTRINSIC FUNCTION WAS INTRODUCED IN IS READ FROM THE OLDER STANDARDS' OWN NEW-FUNCTION LISTS,
/// NEVER FROM <see cref="IntrinsicCatalog"/> ITSELF (kb/Work PB4296).
/// </summary>
/// <remarks>
/// <para>
/// <c>specs/ISO_COBOL.md</c> is the 2023 text: its section 8.11 lists the intrinsic NAMES with no edition data and its
/// Annex E covers only 2014 to 2023, so a window below 2023 can be checked against nothing the repository tracks. That
/// is how <c>FUNCTION ABS</c> sat at <c>IntroducedIn 2014</c> although ISO/IEC 1989:2002 Annex F.2 item 78 heads its
/// new-function list with it, and why the wrong year then spread to three traceability rows
/// (<c>SpecTraceabilityInventoryDriftTests.EveryFunctionRule_StartsNoEarlierThanItsFunction</c> trusts the catalog) and
/// to four goldens. <c>tests/version-matrix/intrinsic-introductions.json</c> commits the evidence — each edition's list
/// with the passage it was read from, since the 2002 and 2009 drafts are not tracked in git — and this class holds the
/// catalog to it in both directions, so the next window cannot be mis-dated by a hand edit of one row.
/// </para>
/// <para>
/// The 2023 list is the one this repository can re-derive: it is Annex E.2 item 13 of the specification, re-read on
/// every run, so the committed list cannot drift from the text it quotes.
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class IntrinsicIntroductionDriftTests
{
    private static List<(int Edition, string Source, List<string> Functions)> Load()
    {
        string path = TestRepo.VersionMatrix("intrinsic-introductions.json");
        Assert.True(File.Exists(path), $"missing: {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return [.. doc.RootElement.GetProperty("editions").EnumerateArray().Select(e => (
            e.GetProperty("edition").GetInt32(),
            e.GetProperty("source").GetString()!,
            e.GetProperty("functions").EnumerateArray().Select(f => f.GetString()!).ToList()))];
    }

    /// <summary>Every name the clause 15 headings define, from the specification (the population the catalog must cover).</summary>
    private static List<string> SpecFunctions() =>
        [.. SpecClauseText.FunctionSections(File.ReadAllText(TestRepo.Specs("ISO_COBOL.md"))).Select(f => f.Name)];

    [Fact]
    public void EveryFunction_IsIntroducedInExactlyTheEditionTheCommittedListsName()
    {
        var lists = Load();
        Assert.Equal([85, 2002, 2014, 2023], lists.Select(l => l.Edition));
        foreach (var l in lists)
            Assert.False(string.IsNullOrWhiteSpace(l.Source), $"the {l.Edition} list names no source passage");

        var introducedBy = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        foreach (var (edition, _, functions) in lists)
            foreach (string f in functions)
            {
                if (!introducedBy.TryGetValue(f, out var seen))
                    introducedBy[f] = seen = [];
                seen.Add(edition);
            }

        var problems = new List<string>();
        foreach (var (name, at) in introducedBy.OrderBy(p => p.Key, StringComparer.Ordinal))
            if (at.Count > 1)
                problems.Add($"{name} is in the introduction lists of {string.Join(" and ", at)} - a function is introduced once");

        var specNames = SpecFunctions();
        Assert.True(specNames.Count >= 90,
            $"only {specNames.Count} clause 15 function headings found in specs/ISO_COBOL.md - the heading shape changed; fix SpecClauseText.FunctionSections, do not lower the floor.");

        foreach (string name in specNames)
        {
            if (!introducedBy.TryGetValue(name, out var at))
            {
                problems.Add($"{name} is defined by clause 15 but is in no committed introduction list - add it to the edition whose new-function list names it");
                continue;
            }
            if (!IntrinsicCatalog.TryGet(name, out var sig))
                problems.Add($"{name} is defined by clause 15 and listed as introduced in {at[0]}, but IntrinsicCatalog has no row for it");
            else if (sig.IntroducedIn != at[0])
                problems.Add($"{name}: IntrinsicCatalog says IntroducedIn {sig.IntroducedIn}, the committed list of the standard says {at[0]}");
        }
        foreach (string name in introducedBy.Keys.Except(specNames, StringComparer.Ordinal).Order(StringComparer.Ordinal))
            problems.Add($"{name} is in the committed introduction lists but clause 15 of specs/ISO_COBOL.md defines no such function");

        Assert.True(problems.Count == 0,
            $"IntrinsicCatalog's IntroducedIn disagrees with tests/version-matrix/intrinsic-introductions.json over {specNames.Count} functions:\n  "
            + string.Join("\n  ", problems));
    }

    /// <summary>
    /// The 2023 list is Annex E.2 item 13's "the following new intrinsic function names" bullets, re-read from the
    /// specification: the only list of the four this repository can derive rather than quote.
    /// </summary>
    [Fact]
    public void The2023List_IsAnnexE2Item13_OfTheSpecification()
    {
        string[] lines = SpecClauseText.Lines();
        int at = Array.FindIndex(lines, l => l.Contains("FUNCTION ALL INTRINSIC and new intrinsic functions", StringComparison.Ordinal));
        Assert.True(at >= 0, "Annex E.2 item 13 ('FUNCTION ALL INTRINSIC and new intrinsic functions') is not in specs/ISO_COBOL.md - the heading changed; fix this guard, do not delete it.");
        var bullet = new Regex(@"^\s*[—-]\s*([A-Z][A-Z0-9-]*)\s*$", RegexOptions.CultureInvariant);
        var fromSpec = new List<string>();
        for (int i = at + 1; i < lines.Length && fromSpec.Count < 64; i++)
        {
            if (lines[i].Trim().Length == 0) continue;
            if (bullet.Match(lines[i]) is not { Success: true } m) break;
            fromSpec.Add(m.Groups[1].Value);
        }
        Assert.True(fromSpec.Count >= 7, $"only {fromSpec.Count} names read from Annex E.2 item 13 - the bullet shape changed; fix the reader.");
        var committed = Load().Single(l => l.Edition == 2023).Functions;
        Assert.Equal(fromSpec.Order(StringComparer.Ordinal), committed.Order(StringComparer.Ordinal));
    }
}
