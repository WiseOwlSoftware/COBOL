// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Common;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Compiler.Listing;
using WiseOwl.Cobol.Listing.Text;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The binder's diagnostics as records (DESIGN-listing.md section 6.3): <see cref="EditionContext"/> keeps each error and warning
/// as the front end's <see cref="Diagnostic"/>, its rendered channels are views of those records through the one
/// <see cref="Diagnostic.Render"/>, and a record located through the line map keeps the resultant line and frame the listing
/// places it by. Each expected line is written from the format <c>file(line,col): severity CODE: message</c>.
/// </summary>
public sealed class DiagnosticRecordsTests
{
    /// <summary>A map of three resultant lines: line 2 is physical line 7 of <c>a.cob</c>, the others lines 1 and 9.</summary>
    private static SourceLineMap Map() => new(
        [new LineOrigin(new SourceOrigin("a.cob", 1)), new LineOrigin(new SourceOrigin("a.cob", 7)), new LineOrigin(new SourceOrigin("a.cob", 9))]);

    [Fact]
    public void AnErrorAndAWarning_AreRecords_AndTheChannelsRenderThem()
    {
        var map = Map();
        var edition = new EditionContext(2023) { LineMap = map, SourceFile = "a.cob" };
        using (edition.Sink.At(new DiagnosticCursor(2, 4)))
        {
            edition.Error("COBOL0001", "an error");
            edition.Warning("COBOL0903", "a warning");
            edition.Error("COBOL0001", "an error");   // the same fact again: one record
        }
        edition.Error("COBOL0002", "about the whole unit");   // no cursor: no location

        Assert.Equal(["a.cob(7,5): error COBOL0001: an error", "error COBOL0002: about the whole unit"], edition.Diagnostics);
        Assert.Equal(["a.cob(7,5): warning COBOL0903: a warning"], edition.Warnings);
        Assert.True(edition.HasErrors);
        Assert.Equal(3, edition.Records.Count);
        var first = edition.Records[0];
        Assert.Equal(("COBOL0001", DiagnosticSeverity.Error, "an error"), (first.Code, first.Severity, first.Message));
        var at = Assert.NotNull(first.Location);
        Assert.Equal(("a.cob", 6, 4), (at.FileName, at.Line, at.Column));
        // The resultant line the cursor named, and the frame of the map that located it (section 6.3's placement).
        Assert.Equal((2, map.Frame), (at.ResultantLine, at.Frame));
        Assert.Null(edition.Records[2].Location);
        Assert.Equal(edition.Records.Where(r => r.IsError).Select(r => r.ToString()), edition.Diagnostics);
    }

    /// <summary>A location the map did not locate (an identity origin, a constructed location) names no resultant line and no frame,
    /// and every map has a frame of its own.</summary>
    [Fact]
    public void OnlyTheMapLocatesAResultantLine()
    {
        var map = Map();
        Assert.Equal((0, 0), (new SourceLocation("a.cob", 0, 6, 4).ResultantLine, new SourceLocation("a.cob", 0, 6, 4).Frame));
        Assert.Equal((0, 0), (new SourceOrigin("a.cob", 7).ToLocation(4).ResultantLine, new SourceOrigin("a.cob", 7).ToLocation(4).Frame));
        var beyond = map.Locate(9, 0, "a.cob");   // outside the map: the fallback file, no frame
        Assert.Equal((0, 0), (beyond.ResultantLine, beyond.Frame));
        Assert.NotEqual(map.Frame, Map().Frame);
        Assert.NotEqual(0, map.Frame);
    }

    /// <summary>The listing's severity map is total: every compiler severity maps to the listing severity of its own name.</summary>
    [Fact]
    public void TheSeverityMap_IsTotal()
    {
        foreach (var severity in Enum.GetValues<DiagnosticSeverity>())
            Assert.Equal(severity.ToString(), DiagnosticSectionBuilder.SeverityOf(severity).ToString());
        Assert.Equal(Enum.GetValues<DiagnosticSeverity>().Length, Enum.GetValues<ListingSeverity>().Length);
    }

    /// <summary>The listing prints each diagnostic's severity in the word the compiler's own diagnostic line prints it in (DESIGN-listing.md
    /// section 6.3: the inline row and the summary repeat the record the compiler printed), so a word changed in one place and not the
    /// other is red here (listing step 3 review lead R1-2). The words are the ones the compiler's diagnostic line is documented with.</summary>
    [Fact]
    public void TheSeverityWords_AreTheCompilersWords()
    {
        Assert.Equal(["error", "warning", "info"],
            new[] { DiagnosticSeverity.Error, DiagnosticSeverity.Warning, DiagnosticSeverity.Info }.Select(Diagnostic.SeverityWord));
        foreach (var severity in Enum.GetValues<DiagnosticSeverity>())
            Assert.Equal(Diagnostic.SeverityWord(severity), TextListingRenderer.SeverityWord(DiagnosticSectionBuilder.SeverityOf(severity)));
    }
}
