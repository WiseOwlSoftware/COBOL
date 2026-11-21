// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// §1985:I.1.5.2.3: "A conforming implementation of Standard COBOL is required to support obsolete language elements" and "must
/// provide a warning mechanism ... to indicate ... that a program contains obsolete language elements" (kb/Work PB5986). The
/// warning is the construct registry's COBOL0903 verdict, driven by a row's <c>ObsoleteIn</c>; this class holds the rows that
/// carry <c>ObsoleteIn</c> 85 to the standard's list (§1985:C.1) and to the support the standard requires. The documentation half
/// ("Documentation associated with an implementation must identify all obsolete language elements") is the doc audit
/// <c>scripts/docchecks/manual.py</c> (the user manual's table against these same rows), because no compiler test opens a Markdown
/// file (kb/Work PB4453).
/// </summary>
public sealed class ObsoleteElementsDriftTests
{
    private const int FirstEdition = 85;

    private static IEnumerable<ConstructDialectStatus> ObsoleteIn1985() =>
        ConstructRegistry.Entries.Where(e => e.ObsoleteIn == FirstEdition);

    [Fact]
    public void EveryRowObsoleteAt85_CitesTheStandardsObsoleteElementList()
    {
        Assert.NotEmpty(ObsoleteIn1985());
        foreach (var row in ObsoleteIn1985())
            Assert.True(row.Citation.Contains("§1985:C.1 item", StringComparison.Ordinal),
                $"{row.Id} is obsolete at 85 but its citation names no item of §1985:C.1: {row.Citation}");
    }

    [Fact]
    public void AnObsoleteElementStaysInForce_WhereTheStandardRequiresItsSupport()
    {
        // §1985:I.1.5.2.3: "required to support obsolete language elements": obsolete is IN FORCE, removed is not.
        foreach (var row in ObsoleteIn1985())
        {
            Assert.Equal(ConstructAvailability.Obsolete, row.StatusAt(FirstEdition));
            Assert.True(row.IsInForceAt(FirstEdition), $"{row.Id} must remain supported at COBOL-{FirstEdition}");
            Assert.True(row.RemovedIn is { } removed && !row.IsInForceAt(removed), $"{row.Id} must be gone from its removal edition");
        }
    }
}
