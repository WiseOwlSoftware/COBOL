// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Tests.Shared;
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
            // The list's items are cited by the printed page they stand on, p.XVII-81 to p.XVII-86: an item number is no rule
            // designator of scripts/spec/citation.py (kb/Work PB5449).
            Assert.True(Enumerable.Range(81, 6).Any(p => row.Citation.Contains("p.XVII-" + p, StringComparison.Ordinal)),
                $"{row.Id} is obsolete at 85 but its citation names no printed page of the obsolete list (§1985:p.XVII-81 to p.XVII-86): {row.Citation}");
    }

    [Fact]
    public void AHiddenUseForDebuggingSection_IsReportedAtItsPhysicalColumn_NotTheResultantOne()
    {
        // §1985:p.XVII-86 (obsolete list item 18): the section the mode makes comment is still a use of the Debug module. Its COBOL0903 sits where the
        // reader sees the section header: line 11, column 8 of the fixed-form source (kb/Work PB5986, review R1-1).
        string source = string.Join('\n',
            "       IDENTIFICATION DIVISION.", "       PROGRAM-ID. OUTR.", "       PROCEDURE DIVISION.", "       M0.",
            "           CALL \"INNR\".", "           STOP RUN.", "       IDENTIFICATION DIVISION.", "       PROGRAM-ID. INNR.",
            "       PROCEDURE DIVISION.", "       DECLARATIVES.", "       DBG-SEC SECTION.",
            "           USE FOR DEBUGGING ON ALL PROCEDURES.", "       DBG-PARA.", "           DISPLAY \"DBG\".",
            "       END DECLARATIVES.", "       MAIN SECTION.", "       M1.", "           DISPLAY \"IN\".",
            "           EXIT PROGRAM.", "       END PROGRAM INNR.", "       END PROGRAM OUTR.") + "\n";
        string dir = ScratchDirectory.Create("obs85");
        try
        {
            string src = Path.Combine(dir, "hidden.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, EditionInfo.Of(85),
                SourceFormat: WiseOwl.Cobol.Frontend.Preprocessor.InitialReferenceFormat.Fixed));
            string warning = Assert.Single(r.Warnings, w => w.Contains("COBOL0903", StringComparison.Ordinal));
            Assert.Contains("hidden.cob(11,8)", warning, StringComparison.Ordinal);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]
    public void AnObsoleteElementStaysInForce_WhereTheStandardRequiresItsSupport()
    {
        // §1985:I.1.5.2.3: "required to support obsolete language elements": obsolete is IN FORCE, removed is not.
        foreach (var row in ObsoleteIn1985())
        {
            Assert.Equal(ConstructAvailability.Obsolete, row.Window.StatusAt(FirstEdition));
            Assert.True(row.Window.IsInForceAt(FirstEdition), $"{row.Id} must remain supported at COBOL-{FirstEdition}");
            Assert.True(row.RemovedIn is { } removed && !row.Window.IsInForceAt(removed), $"{row.Id} must be gone from its removal edition");
        }
    }
}
