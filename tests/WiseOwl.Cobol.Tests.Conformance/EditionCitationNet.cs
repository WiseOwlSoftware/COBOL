// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
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
/// <c>ConstructRegistry.Check</c>, the intrinsic and exception-name gates and the digit-capacity gate). A construct 1985
/// lacks has no 1985 rule, so the message cites the edition where it begins (the PB4950 design's edition-gate arm), or</item>
/// <item>its descriptor records that the rule has no 1985 counterpart (<see cref="DiagnosticDescriptor.No1985Rule"/>,
/// the reason written in the catalog), or</item>
/// <item>it is COUNTED on the RATCHET <c>conformance-suite/wiseowl/edition-citation-pending-1985.json</c>: code → refusal id
/// → the exact number of such lines that refusal draws under that code, measured when the net was laid (kb/Work PB4950's
/// remaining work; the file names that note as its owner).</item>
/// </list>
/// <para>⛔ THE RATCHET IS EXACT, PER REFUSAL AND CODE (kb/Work PB4950 review R1-1, after the tolerated rows of
/// <c>docs/rearchitecture/architecture-edges.json</c>). A count that GROWS is a new offending site and fails; a count that
/// SHRINKS fails until the file is lowered to it ("lower the count"), and a count that reaches 0 fails until the entry is
/// removed; an entry whose refusal is no 85 refusal any more fails <see cref="StaleEntries"/>. So the file only shrinks, and a
/// new later-edition-only citation under a LISTED code, in any refusal, fails as surely as one under a new code. The key is
/// (refusal, code), not (code, printed citation): a refusal is one program, so a second site of a code within it raises
/// that refusal's count, and keying by the citation would let a site that prints a citation already listed for the code
/// elsewhere pass.</para>
/// A 1985 citation is <c>§1985:</c> (the 1985 standard) or <c>§1989:</c> (its 1989 intrinsic-function amendment).
/// </summary>
public static class EditionCitationNet
{
    /// <summary>The ratchet file, relative to the suite.</summary>
    private static readonly string[] RatchetPath = ["wiseowl", "edition-citation-pending-1985.json"];

    /// <summary>A citation of a rule of a later edition than 1985, in the edition form or the prose form.</summary>
    private static readonly Regex LaterEditionCitation =
        new(@"§(2002|2014|2023):|ISO/IEC 1989:(2002|2014|2023)\b", RegexOptions.Compiled);

    /// <summary>A citation of the 1985 standard or its 1989 amendment.</summary>
    private static readonly Regex Cobol85Citation = new(@"§(1985|1989):", RegexOptions.Compiled);

    /// <summary>The edition gate's own words: the refusal names the edition it targets.</summary>
    private static readonly Regex EditionGate = new(@"\(targeting COBOL-85\)", RegexOptions.Compiled);

    /// <summary>The emitted code of a rendered diagnostic line.</summary>
    private static readonly Regex Code = new(@"\b(?:error|warning) (COBOL\d{4}):", RegexOptions.Compiled);

    /// <summary>The ratchet: code → refusal id → the counted lines still pending.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> Pending { get; } = LoadPending();

    /// <summary>The codes whose every descriptor records that its rule has no 1985 counterpart.</summary>
    public static IReadOnlySet<string> Exempt { get; } = DiagnosticCatalog.All
        .GroupBy(d => d.Code, StringComparer.Ordinal)
        .Where(g => g.All(d => d.No1985Rule is not null))
        .Select(g => g.Key)
        .ToHashSet(StringComparer.Ordinal);

    /// <summary>Whether a rendered diagnostic cites a later edition's rule and no 1985 rule.</summary>
    public static bool CitesOnlyALaterEdition(string diagnostic) =>
        LaterEditionCitation.IsMatch(diagnostic) && !Cobol85Citation.IsMatch(diagnostic) && !EditionGate.IsMatch(diagnostic);

    /// <summary>The code of a rendered diagnostic, or <see langword="null"/> when the line carries none.</summary>
    public static string? CodeOf(string diagnostic) => Code.Match(diagnostic) is { Success: true } m ? m.Groups[1].Value : null;

    /// <summary>The ratchet's verdict for refusal <paramref name="id"/>: one line per code whose offending count differs from
    /// the count the file records for that refusal (empty when every count matches).</summary>
    public static IReadOnlyList<string> RatchetFindings(IEnumerable<string> diagnostics, string id)
    {
        var offending = diagnostics.Where(CitesOnlyALaterEdition)
            .Where(d => CodeOf(d) is not { } code || !Exempt.Contains(code))
            .GroupBy(d => CodeOf(d) ?? "(no code)", StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var codes = offending.Keys
            .Concat(Pending.Where(p => p.Value.ContainsKey(id)).Select(p => p.Key))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        var findings = new List<string>();
        foreach (string code in codes)
        {
            int actual = offending.TryGetValue(code, out var lines) ? lines.Count : 0;
            int counted = Pending.TryGetValue(code, out var cases) && cases.TryGetValue(id, out int n) ? n : 0;
            if (actual > counted)
                findings.Add($"{code}: {actual} line(s) cite only a later edition's rule where the ratchet counts {counted} — a NEW "
                    + "site: cite the 1985 rule through EditionInfo.Cite, or record in the descriptor's No1985Rule why 1985 has "
                    + "none:\n  " + string.Join("\n  ", lines!));
            else if (actual < counted)
                findings.Add(actual == 0
                    ? $"{code}: no line cites only a later edition any more, and the ratchet counts {counted}: remove the entry "
                      + $"\"{id}\" of {code} from the ratchet"
                    : $"{code}: {actual} line(s) cite only a later edition where the ratchet counts {counted}: lower the count of "
                      + $"\"{id}\" under {code} to {actual}");
        }
        return findings;
    }

    /// <summary>Assert the ratchet for one refusal compiled at <c>--std 85</c> (nothing is asked at another edition).</summary>
    public static void AssertCitesTheEditionInForce(IEnumerable<string> diagnostics, string edition, string id, string subject)
    {
        if (edition != "85") return;
        var findings = RatchetFindings(diagnostics, id);
        Assert.True(findings.Count == 0,
            $"{subject}: the 1985 citation ratchet (kb/Work PB4950; {string.Join("/", RatchetPath)}):\n"
            + string.Join("\n", findings));
    }

    /// <summary>The ratchet's entries that name no refusal of <paramref name="refusalsAt85"/> (the ids the wiseowl manifest
    /// compiles at 85): an entry whose refusal is gone or no longer refused at 85 would otherwise never be asked.</summary>
    public static IReadOnlyList<string> StaleEntries(IReadOnlySet<string> refusalsAt85) =>
        [.. Pending.SelectMany(p => p.Value.Keys.Where(id => !refusalsAt85.Contains(id)).Select(id => $"{p.Key} / {id}"))
            .Order(StringComparer.Ordinal)];

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> LoadPending()
    {
        string path = TestRepo.Suite(RatchetPath);
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        // The file is tracked work: it names the note that owns it, so it is never a register of its own.
        if (doc.RootElement.GetProperty("owner").GetString() != "kb/Work PB4950")
            throw new InvalidDataException($"{path}: the ratchet's owner shall be kb/Work PB4950");
        var pending = new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.Ordinal);
        foreach (JsonProperty code in doc.RootElement.GetProperty("pending").EnumerateObject())
        {
            var cases = code.Value.EnumerateObject().ToDictionary(c => c.Name, c => c.Value.GetInt32(), StringComparer.Ordinal);
            if (cases.Count == 0 || cases.Values.Any(n => n <= 0))
                throw new InvalidDataException($"{path}: {code.Name} holds an empty entry or a count below 1; remove it");
            pending[code.Name] = cases;
        }
        return pending;
    }
}
