// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Frontend;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Listing;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// ⛔ NO LISTING ASKED, NO LISTING WORK — BY STRUCTURE (DESIGN-listing.md section 9). A compilation whose
/// <c>Options.Listing</c> is null builds the front end the driver builds (<see cref="CompilerDriver.NewFrontend"/>) with no
/// listing recording: every run of the text stages traces into <see cref="TextStageTrace.None"/>, the parse collects no
/// <see cref="SourceUnitFacts"/>, no file is hashed and no clock is read for a program without WHEN-COMPILED; and the
/// compilation returns no listing and writes no <c>.lst</c>. With a listing asked, the same calls record each fact, so the
/// test can tell the two apart (each assertion below fails on the listing side).
/// </summary>
public sealed class ListingOffDoesNoWorkDriftTests : WiseOwlCobolTestBase
{
    private const string Source = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. NOWORK.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
                   COPY NOWORKC.
               PROCEDURE DIVISION.
               MAIN-PARA.
                   DISPLAY X.
                   STOP RUN.
        """;

    private (CompilerDriver.Options Options, string Source) Write(WiseOwl.Cobol.Listing.ListingRequest? listing)
    {
        string src = Path.Combine(TempDir, "nowork.cob");
        File.WriteAllText(src, Source);
        File.WriteAllText(Path.Combine(TempDir, "NOWORKC.cpy"), "       01 X PIC X(4) VALUE \"ABCD\".\n");
        return (new CompilerDriver.Options(src, DialectLevel: 85, CopyPaths: [TempDir], Listing: listing), src);
    }

    [Fact]
    public void WithoutAListing_TheFrontEndRecordsNothing()
    {
        var (options, src) = Write(listing: null);
        var inputs = new CompilationInputs();
        var frontend = CompilerDriver.NewFrontend(options, inputs, recordsListing: options.Listing is not null);
        Assert.False(frontend.RecordsListing);
        Assert.NotNull(frontend.Parse(src, new DiagnosticBag()));
        Assert.Same(TextStageTrace.None, frontend.ListingTrace);
        Assert.Null(frontend.UnitFacts);
        Assert.Empty(inputs.FileDigests);
        Assert.False(inputs.ReadsCompilationTime);
    }

    /// <summary>The control: with a listing asked, the same front end records the inclusion and the units.</summary>
    [Fact]
    public void WithAListing_TheFrontEndRecords()
    {
        var (options, src) = Write(new WiseOwl.Cobol.Listing.ListingRequest());
        var inputs = new CompilationInputs { DigestsFiles = true };
        var frontend = CompilerDriver.NewFrontend(options, inputs, recordsListing: options.Listing is not null);
        Assert.NotNull(frontend.Parse(src, new DiagnosticBag()));
        Assert.NotSame(TextStageTrace.None, frontend.ListingTrace);
        Assert.Single(frontend.ListingTrace.Inclusions);
        Assert.Equal("NOWORK", Assert.Single(frontend.UnitFacts!.Units).Name);
        Assert.Equal(2, inputs.FileDigests.Count);
    }

    [Fact]
    public void WithoutAListing_TheCompilationReturnsAndWritesNone()
    {
        var (options, _) = Write(listing: null);
        var result = CompilerDriver.Compile(options);
        Assert.True(result.Success, string.Join("\n", result.Errors));
        Assert.Null(result.Listing);
        Assert.DoesNotContain(result.OutputFiles, f => f.EndsWith(ListingRequest.TextExtension, StringComparison.Ordinal));
        Assert.False(result.Inputs.ReadsCompilationTime);
        Assert.Empty(result.Inputs.FileDigests);
    }
}
