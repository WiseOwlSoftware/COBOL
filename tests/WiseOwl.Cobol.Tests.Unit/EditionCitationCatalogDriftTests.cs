// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Editions.Diagnostics;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>⛔ EVERY DIAGNOSTIC WHOSE CITATION NAMES A LATER EDITION SAYS WHAT IT CITES AT <c>--std 85</c> (kb/Work PB5993, PB4950).
/// Asked of the catalog, not of the refusals a suite happens to run at 85 (the conformance suite's <c>EditionCitationNet</c> sees
/// only those): a descriptor whose <see cref="DiagnosticDescriptor.IsoSection"/> cites a rule of COBOL-2002, 2014 or 2023 and no
/// 1985 rule carries either <see cref="DiagnosticDescriptor.Rule1985"/>, the 1985 rule its message cites at 85 (the 1985 arm of
/// <c>EditionInfo.Cite</c> at its site), or <see cref="DiagnosticDescriptor.No1985Rule"/>, the reason the 1985 text has no such
/// rule (a construct 1985 lacks, refused at 85 by the edition gate before this diagnostic can be reached, or a rule a later edition
/// added). STRICT: there is no list of descriptors still to move (group q of wave 20262131 emptied the ratchet group h laid, 669
/// descriptors on 3a30f8b2f7, 175 on dda8ee6e75, 0 now), so any descriptor that carries neither fails.</summary>
public sealed class EditionCitationCatalogDriftTests
{
    /// <summary>A citation of a rule of a later edition than 1985, in the edition form or the prose form.</summary>
    private static readonly Regex LaterEditionCitation =
        new(@"§(2002|2014|2023):|ISO/IEC 1989:(2002|2014|2023)\b", RegexOptions.Compiled);

    /// <summary>A citation of the 1985 standard or its 1989 intrinsic-function amendment.</summary>
    private static readonly Regex Cobol85Citation = new(@"§(1985|1989):", RegexOptions.Compiled);

    /// <summary>A 1985 rule in the edition form: the locator after <c>§1985:</c> (or the 1989 amendment's <c>§1989:</c>).</summary>
    private static readonly Regex EditionForm1985 = new(@"^§(1985|1989):\S", RegexOptions.Compiled);

    private static bool CitesOnlyALaterEdition(DiagnosticDescriptor d) =>
        LaterEditionCitation.IsMatch(d.IsoSection) && !Cobol85Citation.IsMatch(d.IsoSection);

    [Fact]
    public void EveryLaterEditionDescriptor_CarriesIts1985Rule_OrWhy1985HasNone()
    {
        var missing = DiagnosticCatalog.All
            .Where(d => CitesOnlyALaterEdition(d) && d.Rule1985 is null && d.No1985Rule is null)
            .Select(d => d.Id)
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.True(missing.Count == 0,
            $"{missing.Count} descriptor(s) cite only a later edition's rule and say nothing of 1985. Give each its Rule1985 (the 1985 "
            + "rule its site cites at --std 85 through EditionInfo.Cite, checked with scripts/spec/cite.py --check) or its No1985Rule "
            + "(why the 1985 text has none, with the 1985 clause that would hold it):\n  " + string.Join(", ", missing));
    }

    [Fact]
    public void ADescriptor_CarriesAtMostOneOf1985RuleAndNo1985Rule_AndItsRuleIsA1985Citation()
    {
        var both = DiagnosticCatalog.All.Where(d => d.Rule1985 is not null && d.No1985Rule is not null).Select(d => d.Id).ToList();
        Assert.True(both.Count == 0, "a descriptor carries both its 1985 rule and the reason 1985 has none: " + string.Join(", ", both));
        var malformed = DiagnosticCatalog.All
            .Where(d => d.Rule1985 is { } r && !EditionForm1985.IsMatch(r))
            .Select(d => $"{d.Id}: {d.Rule1985}")
            .ToList();
        Assert.True(malformed.Count == 0,
            "Rule1985 is a 1985 rule in the edition form \"§1985:<locator>[ <rule>]\" (kb/Work PB4887):\n  " + string.Join("\n  ", malformed));
    }
}
