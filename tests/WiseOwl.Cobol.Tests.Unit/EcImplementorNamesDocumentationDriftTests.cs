// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime.Exceptions;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE IMPLEMENTOR-DEFINED EXCEPTION-NAME DOCUMENTATION (docs/CONFORMANCE.md §7, Annex A.1 items 99 and 100) IS HELD TO
/// THE CATALOG IT DOCUMENTS (kb/Work PB1531, PB4453). ISO §14.6.13.1.1 leaves "the action to be taken, the fatality, and when any of
/// these exceptions are raised" of an implementor-defined exception-name to the implementor, and A.1 requires what the
/// implementor provides to be documented; the catalog (<see cref="ExceptionCatalog"/>) is the one machine form of the names
/// a program can write. Item 99 says no <c>EC-IMP-suffix</c> exists and item 100 lists the level-2 <c>-IMP</c> names Table 13
/// defines and says each is fatal. THIS CLASS holds the catalog's side (22 names, every one fatal, no <c>EC-IMP-</c> row), and
/// the documents' side is <c>scripts/docchecks/facts.py</c> (<c>exception_name_findings</c>), which reads the catalog's names
/// from <c>tests/doc-facts/compiler-facts.json</c>: no compiler test opens the Markdown.
/// </summary>
public sealed class EcImplementorNamesDocumentationDriftTests
{
    /// <summary>A.1 item 100's population is Table 13's 22 level-2 <c>-IMP</c> exception-names, each fatal because the
    /// catalog treats every one as fatal. The names themselves are the facts file's, which the audit holds item 100 to.</summary>
    [Fact]
    public void Item100_NamesExactlyTheCatalogsImplementorDefinedLevel2Names()
    {
        var provided = ExceptionCatalog.Level3Rows.Where(i => i.Fatality == EcFatality.Imp).ToList();
        Assert.Equal(22, provided.Count);   // Table 13's EC-level-2-IMP rows — the population is asserted before the audit matches it
        Assert.Equal(22, provided.Select(i => i.Name).Distinct(StringComparer.Ordinal).Count());
        Assert.All(provided, i => Assert.True(i.IsFatal, i.Name));
    }

    /// <summary>A.1 item 99 says this implementation provides no <c>EC-IMP-suffix</c> name, and the catalog agrees: no
    /// level-3 row starts <c>EC-IMP-</c> and the name resolves to nothing (the refusal itself is COBOL0711, witnessed by the
    /// <c>pb1531-ec-imp-suffix-*</c> negative goldens).</summary>
    [Fact]
    public void Item99_NoEcImpSuffixNameExists()
    {
        Assert.DoesNotContain(ExceptionCatalog.Level3Rows, i => i.Name.StartsWith("EC-IMP-", StringComparison.Ordinal));
        Assert.False(ExceptionCatalog.TryGet("EC-IMP-WIBBLE", out _));
        Assert.True(ExceptionCatalog.IsImplementorSuffixForm("EC-IMP-WIBBLE"));   // the SPELLING stays reserved (§8.12)
    }
}
