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
/// <item>its code is on the tracked allowlist <c>conformance-suite/wiseowl/edition-citation-pending-1985.json</c>,
/// each entry a code the PB4950 sweep has not yet reached, measured when the net was laid. A NEW code that cites only
/// a later edition at 85 fails at once; the change that fixes a listed code removes its entry.</item>
/// </list>
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

    /// <summary>The tracked allowlist: code → why it is still pending (each entry names kb/Work PB4950).</summary>
    public static IReadOnlyDictionary<string, string> Pending { get; } = LoadPending();

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

    /// <summary>Assert that every diagnostic of a refusal compiled at <c>--std 85</c> cites the 1985 rule, or is
    /// exempt by its catalog row, or is pending on the tracked allowlist.</summary>
    public static void AssertCitesTheEditionInForce(IEnumerable<string> diagnostics, string edition, string subject)
    {
        if (edition != "85") return;
        var offenders = diagnostics.Where(CitesOnlyALaterEdition)
            .Where(d => CodeOf(d) is not { } code || !(Exempt.Contains(code) || Pending.ContainsKey(code)))
            .ToList();
        Assert.True(offenders.Count == 0,
            $"{subject}: {offenders.Count} diagnostic(s) at --std 85 cite only a later edition's rule (kb/Work PB4950): "
            + "cite the 1985 rule through EditionContext.Cite, or record in the descriptor's No1985Rule why 1985 has "
            + "none:\n" + string.Join("\n", offenders));
    }

    private static IReadOnlyDictionary<string, string> LoadPending()
    {
        string path = TestRepo.Suite("wiseowl", "edition-citation-pending-1985.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var pending = doc.RootElement.GetProperty("pending").EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString()!, StringComparer.Ordinal);
        // Every entry is tracked work: its reason names the note that owns it, so the list is never a register of its own.
        string? untracked = pending.FirstOrDefault(p => !p.Value.Contains("kb/Work PB4950", StringComparison.Ordinal)).Key;
        if (untracked is not null)
            throw new InvalidDataException($"{path}: the pending entry {untracked} names no kb/Work PB4950");
        return pending;
    }
}
