// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ A DIAGNOSTIC AT <c>--std 85</c> CITES THE 1985 RULE (kb/Work PB4950, the owner's decision of 2026-11-07: "a
/// diagnostic cite the rule of the edition the program was compiled under"). Asked of the RENDERED text of every
/// diagnostic, error or warning, of every refusal the wiseowl manifest runs at 85: a diagnostic that cites a rule of a
/// later edition (<c>§2002:</c>, <c>§2014:</c>, <c>§2023:</c> or the prose <c>ISO/IEC 1989:2002</c>) and no 1985 rule
/// fails here, unless
/// <list type="bullet">
/// <item>it is an EDITION GATE, the refusal of a construct the 1985 standard does not have, whose message says so by
/// naming the edition it targets ("... requires COBOL-2002 (targeting COBOL-85)", the one phrase of
/// <c>ConstructRegistry.Check</c>, the intrinsic and exception-name gates, the digit-capacity gate, and a rule of a
/// construct COBOL-2002 introduced that still fires at 85). A construct 1985 lacks has no 1985 rule, so the message cites
/// the edition where it begins (the PB4950 design's edition-gate arm), or</item>
/// <item>its descriptor records that the rule has no 1985 counterpart (<see cref="DiagnosticDescriptor.No1985Rule"/>,
/// the reason written in the catalog).</item>
/// </list>
/// <para>There is no allowance list: the sweep of kb/Work PB4950 emptied the ratchet the net was laid with (298 codes, 1638
/// lines on 2026-11-17), so a later-edition-only citation under ANY code, in any refusal, fails.</para>
/// A 1985 citation is <c>§1985:</c> (the 1985 standard) or <c>§1989:</c> (its 1989 intrinsic-function amendment).
/// </summary>
public static class EditionCitationNet
{
    /// <summary>A citation of a rule of a later edition than 1985, in the edition form or the prose form.</summary>
    private static readonly Regex LaterEditionCitation =
        new(@"§(2002|2014|2023):|ISO/IEC 1989:(2002|2014|2023)\b", RegexOptions.Compiled);

    /// <summary>A citation of the 1985 standard or its 1989 amendment.</summary>
    private static readonly Regex Cobol85Citation = new(@"§(1985|1989):", RegexOptions.Compiled);

    /// <summary>The edition gate's own words: the refusal names the edition it targets.</summary>
    private static readonly Regex EditionGate = new(@"\(targeting COBOL-85\)", RegexOptions.Compiled);

    /// <summary>The emitted code of a rendered diagnostic line.</summary>
    private static readonly Regex Code = new(@"\b(?:error|warning) (COBOL\d{4}):", RegexOptions.Compiled);

    /// <summary>The codes whose every descriptor records that its rule has no 1985 counterpart.</summary>
    public static IReadOnlySet<string> Exempt { get; } = DiagnosticCatalog.All
        .GroupBy(d => d.Code, StringComparer.Ordinal)
        .Where(g => g.All(d => d.No1985Rule is not null))
        .Select(g => g.Key)
        .ToHashSet(StringComparer.Ordinal);

    /// <summary>The 1985 clauses of each code whose every descriptor records the 1985 rule its message cites at 85
    /// (<see cref="DiagnosticDescriptor.Rule1985"/>, several joined by <c> / </c>; kb/Work PB5993): asked at the clause, so a family
    /// descriptor whose message names another rule of a clause it records is not mis-recorded.</summary>
    public static IReadOnlyDictionary<string, string[]> Rules1985 { get; } = DiagnosticCatalog.All
        .GroupBy(d => d.Code, StringComparer.Ordinal)
        .Where(g => g.All(d => d.Rule1985 is not null))
        .ToDictionary(g => g.Key,
            g => g.SelectMany(d => d.Rule1985!.Split(" / ", StringSplitOptions.TrimEntries)).Select(r => r.Split(' ')[0])
                .Distinct(StringComparer.Ordinal).ToArray(),
            StringComparer.Ordinal);

    /// <summary>The lines whose code's descriptors record their 1985 rule and that cite a 1985 rule but none of those: the catalog
    /// says one rule and the message another (kb/Work PB5993).</summary>
    public static IReadOnlyList<string> MisRecorded(IEnumerable<string> diagnostics) =>
        [.. diagnostics.Where(d => Cobol85Citation.IsMatch(d) && CodeOf(d) is { } code
            && Rules1985.TryGetValue(code, out var rules) && !rules.Any(r => d.Contains(r, StringComparison.Ordinal)))];

    /// <summary>The lines whose code's every descriptor records that 1985 has no such rule (<see cref="Exempt"/>) and that cite a
    /// 1985 rule all the same: the catalog says one thing and the message another (kb/Work PB5993, group q's audit: COBOL2159 at
    /// 85 cited §1985:X.4.5.3 SR2 under a descriptor that said the 1985 text has no rule of it). The fix is the descriptor's
    /// <see cref="DiagnosticDescriptor.Rule1985"/>, never a message that drops its 1985 rule.</summary>
    public static IReadOnlyList<string> Contradicted(IEnumerable<string> diagnostics) =>
        [.. diagnostics.Where(d => Cobol85Citation.IsMatch(d) && CodeOf(d) is { } code && Exempt.Contains(code))];

    /// <summary>Whether a rendered diagnostic cites a later edition's rule and no 1985 rule.</summary>
    public static bool CitesOnlyALaterEdition(string diagnostic) =>
        LaterEditionCitation.IsMatch(diagnostic) && !Cobol85Citation.IsMatch(diagnostic) && !EditionGate.IsMatch(diagnostic);

    /// <summary>The code of a rendered diagnostic, or <see langword="null"/> when the line carries none.</summary>
    public static string? CodeOf(string diagnostic) => Code.Match(diagnostic) is { Success: true } m ? m.Groups[1].Value : null;

    /// <summary>The lines of <paramref name="diagnostics"/> that cite only a later edition's rule under a code whose descriptor
    /// records no reason 1985 has none: the offences, empty when every line cites the edition in force.</summary>
    public static IReadOnlyList<string> Offences(IEnumerable<string> diagnostics) =>
        [.. diagnostics.Where(CitesOnlyALaterEdition).Where(d => CodeOf(d) is not { } code || !Exempt.Contains(code))];

    /// <summary>A citation of a rule of an edition later than COBOL-2002, edition form or prose form.</summary>
    private static readonly Regex After2002Citation = new(@"§(2014|2023):|ISO/IEC 1989:(2014|2023)\b", RegexOptions.Compiled);

    /// <summary>A citation of a rule of an edition later than COBOL-2014.</summary>
    private static readonly Regex After2014Citation = new(@"§2023:|ISO/IEC 1989:2023\b", RegexOptions.Compiled);

    /// <summary>Whether a rendered diagnostic compiled at <paramref name="edition"/> (2002 or 2014) cites a later edition's rule and
    /// none of its own edition's (kb/Work PB5922, the 2002 and 2014 directions of PB4950's decision): its own citation is
    /// <c>§2002:</c> or <c>§2014:</c>; an edition gate names the edition it targets ("(targeting COBOL-2002)").</summary>
    public static bool CitesOnlyALaterEditionAt(string diagnostic, string edition) => edition switch
    {
        "2002" => After2002Citation.IsMatch(diagnostic) && !diagnostic.Contains("§2002:", StringComparison.Ordinal)
            && !diagnostic.Contains("(targeting COBOL-2002)", StringComparison.Ordinal),
        "2014" => After2014Citation.IsMatch(diagnostic) && !diagnostic.Contains("§2014:", StringComparison.Ordinal)
            && !diagnostic.Contains("(targeting COBOL-2014)", StringComparison.Ordinal),
        _ => throw new ArgumentOutOfRangeException(nameof(edition), edition, "the net's 2002 and 2014 arm asks only those editions"),
    };

    /// <summary>⛔ THE OPEN SWEEP AT 2002 AND 2014 (kb/Work PB6118): the codes whose message, at that edition, still cites only a later
    /// edition's rule in some refusal of the wiseowl manifest. A RATCHET, never an allowance: a code leaves when its sites cite their
    /// edition's rule, and no code joins (a new descriptor, or a code that already cites its edition, fails here). Read from
    /// <c>tests/WiseOwl.Cobol.Tests.Conformance/edition-citation-sweep.json</c>.</summary>
    public static IReadOnlyDictionary<string, IReadOnlySet<string>> OpenSweep { get; } = LoadOpenSweep();

    private static Dictionary<string, IReadOnlySet<string>> LoadOpenSweep()
    {
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(SweepFile));
        return doc.RootElement.EnumerateObject().ToDictionary(e => e.Name,
            e => (IReadOnlySet<string>)e.Value.EnumerateArray().Select(c => c.GetString()!).ToHashSet(StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    /// <summary>The MEASURE of the open sweep (kb/Work PB6118): with this variable naming a directory, the 2002 and 2014 arm writes each
    /// offence to <c>&lt;directory&gt;/&lt;edition&gt;.txt</c> (the refusal, a tab, the line) and asserts nothing, so one run of the
    /// manifest's refusals lists every site the sweep has left. Never set in a gate.</summary>
    public const string RecordVariable = "WISEOWL_COBOL_RECORD_CITATION_SWEEP";

    private static readonly Lock RecordLock = new();

    /// <summary>The open sweep's file.</summary>
    public static string SweepFile => TestRepo.At("tests", "WiseOwl.Cobol.Tests.Conformance", "edition-citation-sweep.json");

    /// <summary>The lines of <paramref name="diagnostics"/>, compiled at 2002 or 2014, that cite only a later edition's rule under a
    /// code the open sweep does not hold.</summary>
    public static IReadOnlyList<string> OffencesAt(IEnumerable<string> diagnostics, string edition) =>
        [.. diagnostics.Where(d => CitesOnlyALaterEditionAt(d, edition))
            .Where(d => CodeOf(d) is not { } code || !OpenSweep.TryGetValue(edition, out var open) || !open.Contains(code))];

    /// <summary>Assert the rule for one refusal: at <c>--std 85</c> its diagnostics cite the 1985 rule (kb/Work PB4950), at
    /// <c>--std 2002</c> and <c>--std 2014</c> their own edition's (kb/Work PB5922); nothing is asked at 2023.</summary>
    public static void AssertCitesTheEditionInForce(IEnumerable<string> diagnostics, string edition, string subject)
    {
        if (edition is "2002" or "2014")
        {
            var later = OffencesAt(diagnostics, edition);
            if (Environment.GetEnvironmentVariable(RecordVariable) is { Length: > 0 } dir && later.Count > 0)
            {
                lock (RecordLock) File.AppendAllLines(Path.Combine(dir, $"{edition}.txt"), later.Select(l => $"{subject}\t{l}"));
                return;
            }
            Assert.True(later.Count == 0,
                $"{subject}: a diagnostic at --std {edition} cites only a later edition's rule (kb/Work PB5922): cite the "
                + $"{edition} rule through EditionInfo.Cite's y{edition} argument:\n  " + string.Join("\n  ", later));
            return;
        }
        if (edition != "85") return;
        var offences = Offences(diagnostics);
        Assert.True(offences.Count == 0,
            $"{subject}: a diagnostic at --std 85 cites only a later edition's rule (kb/Work PB4950): cite the 1985 rule through "
            + "EditionInfo.Cite, or record in the descriptor's No1985Rule why 1985 has none:\n  " + string.Join("\n  ", offences));
        var misRecorded = MisRecorded(diagnostics);
        Assert.True(misRecorded.Count == 0,
            $"{subject}: a diagnostic at --std 85 cites a 1985 rule its descriptor's Rule1985 does not record (kb/Work PB5993): make "
            + "the descriptor's Rule1985 the rule the message cites:\n  " + string.Join("\n  ", misRecorded));
        var contradicted = Contradicted(diagnostics);
        Assert.True(contradicted.Count == 0,
            $"{subject}: a diagnostic at --std 85 cites a 1985 rule under a code whose descriptor says 1985 has none (No1985Rule; "
            + "kb/Work PB5993): record the rule in the descriptor's Rule1985 instead:\n  " + string.Join("\n  ", contradicted));
    }
}
