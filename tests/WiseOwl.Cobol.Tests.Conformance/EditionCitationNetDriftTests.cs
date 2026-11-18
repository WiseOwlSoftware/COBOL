// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions.Diagnostics;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>The 1985 citation rule's own arithmetic (kb/Work PB4950), against planted diagnostics: a later-edition-only
/// citation fails under any code, and each of the three ways out (a 1985 citation beside it, the edition gate's words, a
/// descriptor's <see cref="DiagnosticDescriptor.No1985Rule"/>) passes.</summary>
public sealed class EditionCitationNetDriftTests
{
    [Fact]
    public void ALaterEditionOnlyCitation_Fails_AndEachWayOutPasses()
    {
        // A code with a 1985 rule: no descriptor of it records No1985Rule.
        string code = DiagnosticCatalog.All.First(d => !EditionCitationNet.Exempt.Contains(d.Code)).Code;
        string offence = $"prog.cob(1,8): error {code}: planted (ISO §2023:14.9.37.3 SR7)";
        Assert.Equal([offence], EditionCitationNet.Offences([offence]));
        Assert.Single(EditionCitationNet.Offences([$"prog.cob(1,8): warning {code}: planted (ISO/IEC 1989:2002 A.1.2.2 SR6)"]));

        // The 1985 citation beside it, or the 1989 amendment's.
        Assert.Empty(EditionCitationNet.Offences([$"prog.cob(1,8): error {code}: planted (ISO §2023:14.9.37.3 SR7; §1985:VI.6.22.3 SR1)"]));
        Assert.Empty(EditionCitationNet.Offences([$"prog.cob(1,8): error {code}: planted (ISO §1989:A.2.14.3 AR1)"]));
        // The edition gate's own words.
        Assert.Empty(EditionCitationNet.Offences(
            [$"prog.cob(1,8): error {code}: X requires COBOL-2002 (targeting COBOL-85) (ISO §2023:14.9.37.3 SR7)"]));
        // A descriptor that records why 1985 has no rule.
        string exempt = Assert.IsType<string>(EditionCitationNet.Exempt.Order(StringComparer.Ordinal).FirstOrDefault());
        Assert.Empty(EditionCitationNet.Offences([$"prog.cob(1,8): error {exempt}: planted (ISO §2023:14.9.37.3 SR7)"]));
    }
}
