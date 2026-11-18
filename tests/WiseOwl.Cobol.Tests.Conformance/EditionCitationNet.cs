// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Editions.Diagnostics;
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

    /// <summary>Whether a rendered diagnostic cites a later edition's rule and no 1985 rule.</summary>
    public static bool CitesOnlyALaterEdition(string diagnostic) =>
        LaterEditionCitation.IsMatch(diagnostic) && !Cobol85Citation.IsMatch(diagnostic) && !EditionGate.IsMatch(diagnostic);

    /// <summary>The code of a rendered diagnostic, or <see langword="null"/> when the line carries none.</summary>
    public static string? CodeOf(string diagnostic) => Code.Match(diagnostic) is { Success: true } m ? m.Groups[1].Value : null;

    /// <summary>The lines of <paramref name="diagnostics"/> that cite only a later edition's rule under a code whose descriptor
    /// records no reason 1985 has none: the offences, empty when every line cites the edition in force.</summary>
    public static IReadOnlyList<string> Offences(IEnumerable<string> diagnostics) =>
        [.. diagnostics.Where(CitesOnlyALaterEdition).Where(d => CodeOf(d) is not { } code || !Exempt.Contains(code))];

    /// <summary>Assert the rule for one refusal compiled at <c>--std 85</c> (nothing is asked at another edition).</summary>
    public static void AssertCitesTheEditionInForce(IEnumerable<string> diagnostics, string edition, string subject)
    {
        if (edition != "85") return;
        var offences = Offences(diagnostics);
        Assert.True(offences.Count == 0,
            $"{subject}: a diagnostic at --std 85 cites only a later edition's rule (kb/Work PB4950): cite the 1985 rule through "
            + "EditionInfo.Cite, or record in the descriptor's No1985Rule why 1985 has none:\n  " + string.Join("\n  ", offences));
    }
}
