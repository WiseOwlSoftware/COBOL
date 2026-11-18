// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// ⛔ A LISTING ON EVERY OUTCOME THAT READ THE SOURCE (DESIGN-listing.md sections 4.4 and 4.6; section 10 item 8). The listing's
/// one exit builds from what the compilation deposited, never from which return it took, so each kind of outcome is a case:
/// the sections present, the <see cref="ListingSectionStatus"/> of each, the outcome the header names and the file written.
/// <see cref="Listing1985Tests.AFailedCompilation_StillListsItsSource"/> holds the front-end, bind and missing-source cases of
/// the 1985 minimum; this class holds the rest, and the listed pass of a failed parse (section 4.4). A backend error has no
/// source that reaches it (it is Roslyn refusing generated C#, a compiler defect: <c>IntrinsicFloatLaneArgumentRunDriftTests</c>
/// treats it as one); the exit cannot tell it from the other late returns, and <c>ListingOutcomeMapTests</c> of the Unit assembly
/// holds the outcome map total.
/// </summary>
public sealed class ListingOutcomeTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listingoutcome");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    private const string Program = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. OUTCOME.
               PROCEDURE DIVISION.
               MAIN-PARA.
                   DISPLAY "OUTCOME".
                   STOP RUN.
        """;

    private string Source(string text = Program)
    {
        string source = Path.Combine(_dir, "outcome.cob");
        File.WriteAllText(source, text);
        return source;
    }

    /// <summary>Compile, and hold every listing produced to the JSON contract (DESIGN-listing.md section 10 item 5).</summary>
    private static CompilerDriver.Result Compile(CompilerDriver.Options options)
    {
        var result = CompiledProgramCache.CompileCold(options);
        if (result.Listing is { } listing) ListingJsonContract.AssertHolds(listing);
        return result;
    }

    /// <summary>Every section (DESIGN-listing.md section 4.3): the source (the source text was read), the programs (the parser built a
    /// tree) and the diagnostics (always: what was reported).</summary>
    private static void AssertEverySectionProduced(ListingDocument listing) =>
        Assert.Equal([(ListingSection.Source, true, (string?)null), (ListingSection.Programs, true, null), (ListingSection.Diagnostics, true, null),
             (ListingSection.Options, true, null), (ListingSection.Statistics, true, null)],
            listing.Sections.Select(s => (s.Section, s.Produced, s.Reason)));

    /// <summary>Check-only (a library caller's parse and bind, no code generated) is a success that still lists.</summary>
    [Fact]
    public void CheckOnly_ListsTheSource()
    {
        string source = Source();
        var result = Compile(new CompilerDriver.Options(source, DialectLevel: 85, CheckOnly: true, Listing: new ListingRequest()));
        Assert.Equal(CompilerDriver.Outcome.Success, result.Status);
        var listing = Assert.IsType<ListingDocument>(result.Listing);
        Assert.Equal(ListingOutcome.Success, listing.Header.Outcome);
        AssertEverySectionProduced(listing);
        Assert.Equal(File.ReadAllText(source).Split('\n').Length, listing.Lines.Count);
        Assert.Equal("OUTCOME", Assert.Single(listing.Programs).Name);
        string lst = Path.Combine(_dir, "outcome" + ListingOutput.Extension(ListingFormats.Text));
        Assert.Equal([Path.GetFullPath(lst)], result.OutputFiles);
        Assert.False(File.Exists(Path.Combine(_dir, "outcome.dll")));
    }

    /// <summary>The host refuses the assembly's write (the last return of the compilation): the outcome is OutputNotWritable and
    /// the listing, whose own path is writable, is still written (DESIGN-listing.md section 4.6).</summary>
    [Fact]
    public void ARefusedAssemblyWrite_StillWritesTheListing()
    {
        string source = Source();
        // A directory where the assembly would be written: the host refuses the write.
        string dll = Path.Combine(_dir, "outcome.dll");
        Directory.CreateDirectory(dll);
        var result = Compile(new CompilerDriver.Options(source, dll, DialectLevel: 85, Listing: new ListingRequest()));
        Assert.Equal(CompilerDriver.Outcome.OutputNotWritable, result.Status);
        Assert.Contains(result.Errors, e => e.StartsWith("cannot write the output of ", StringComparison.Ordinal));
        var listing = Assert.IsType<ListingDocument>(result.Listing);
        Assert.Equal(ListingOutcome.OutputNotWritable, listing.Header.Outcome);
        AssertEverySectionProduced(listing);
        string lst = Path.GetFullPath(Path.Combine(_dir, "outcome" + ListingOutput.Extension(ListingFormats.Text)));
        Assert.Contains(lst, result.OutputFiles);
        Assert.Contains("DISPLAY \"OUTCOME\".", File.ReadAllText(lst), StringComparison.Ordinal);
    }

    /// <summary>An output that names no file in a directory is refused before the source is read: nothing to list.</summary>
    [Fact]
    public void AnOutputRefusedBeforeTheSourceIsRead_ListsNothing()
    {
        string source = Source();
        string root = Path.GetPathRoot(Path.GetFullPath(_dir))!;
        var result = Compile(new CompilerDriver.Options(source, root, DialectLevel: 85, Listing: new ListingRequest()));
        Assert.Equal(CompilerDriver.Outcome.OutputNotWritable, result.Status);
        Assert.Null(result.Listing);
        Assert.Empty(result.OutputFiles);
    }

    /// <summary>A listing the host refuses to write is one error; the compilation's outcome is unchanged, and the document is
    /// still returned (DESIGN-listing.md section 4.6).</summary>
    [Fact]
    public void ARefusedListingWrite_IsOneError_AndChangesNoOutcome()
    {
        string source = Source();
        string missing = Path.Combine(_dir, "no-such-directory", "outcome");
        var result = Compile(new CompilerDriver.Options(source, Path.Combine(_dir, "outcome.dll"), DialectLevel: 85,
            Listing: new ListingRequest(OutputBase: missing)));
        Assert.Equal(CompilerDriver.Outcome.Success, result.Status);
        string error = Assert.Single(result.Errors);
        Assert.StartsWith($"cannot write the listing '{missing}{ListingOutput.Extension(ListingFormats.Text)}': ", error, StringComparison.Ordinal);
        var listing = Assert.IsType<ListingDocument>(result.Listing);
        Assert.Equal(ListingOutcome.Success, listing.Header.Outcome);
        AssertEverySectionProduced(listing);
        Assert.DoesNotContain(result.OutputFiles, f => f.EndsWith(ListingOutput.Extension(ListingFormats.Text), StringComparison.Ordinal));
        Assert.True(File.Exists(Path.Combine(_dir, "outcome.dll")));
    }

    /// <summary>
    /// The listed pass of a failed parse (DESIGN-listing.md section 4.4; kb/Work PB1066's shape): a <c>&gt;&gt;SOURCE FORMAT IS
    /// FREE</c> in an exception-checking PERFORM's WHEN phrase and a syntax error after END-PERFORM. The first pass reads the
    /// lines after the directive in free form and fails; its recovered tree places §2023:14.9.28.4 GR14's implicit POP before
    /// END-PERFORM, so the front end re-runs speculatively with the fixed form restored there, fails again on the program's own
    /// error, and reports the FIRST pass's diagnostics. The listing describes that same pass: the lines after the directive are
    /// listed in the form the reported pass read them in (free), not the re-run's (fixed from END-PERFORM on), and the line the
    /// reported syntax error names is a listed line holding the offending word.
    /// </summary>
    [Fact]
    public void AFailedParse_ListsThePassWhoseDiagnosticsAreReported()
    {
        string[] lines =
        [
            "       IDENTIFICATION DIVISION.",
            "       PROGRAM-ID. PB1066L.",
            "       PROCEDURE DIVISION.",
            "           PERFORM",
            "               DISPLAY \"IMP1\"",
            "           WHEN EC-SIZE",
            "       >>SOURCE FORMAT IS FREE",
            "               DISPLAY \"HANDLER\"",
            "           END-PERFORM",
            "           FOO BAR BAZ.",
            "           STOP RUN.",
        ];
        string source = Source(string.Join("\n", lines));
        var result = Compile(new CompilerDriver.Options(source, Path.Combine(_dir, "outcome.dll"), Listing: new ListingRequest()));
        Assert.Equal(CompilerDriver.Outcome.FrontendError, result.Status);
        // The reported diagnostics are the first pass's: the program's own error, named.
        Assert.Contains(result.Errors, e => e.Contains("'FOO'", StringComparison.Ordinal));

        var listing = Assert.IsType<ListingDocument>(result.Listing);
        Assert.Equal(ListingOutcome.FrontendError, listing.Header.Outcome);
        Assert.Equal(lines.Length, listing.Lines.Count);
        int directive = Array.FindIndex(lines, l => l.Contains(">>SOURCE", StringComparison.Ordinal)) + 1;   // 1-based
        foreach (var line in listing.Lines)
            Assert.Equal(line.FileLine > directive ? ListingLineForm.Free : ListingLineForm.Fixed, line.Form);
        int foo = Array.FindIndex(lines, l => l.Contains("FOO", StringComparison.Ordinal)) + 1;
        Assert.Contains(result.Errors, e => e.Contains($"outcome.cob({foo},", StringComparison.Ordinal) && e.Contains("'FOO'", StringComparison.Ordinal));
        var fooLine = listing.Lines.Single(l => l.FileLine == foo);
        Assert.Contains("FOO", fooLine.Text, StringComparison.Ordinal);
        // Step 3's anchoring (section 4.4): each reported syntax error is placed on the line, and at the column, its record names in
        // the listed pass; the one naming FOO on FOO's line, at FOO (column 12 of the line as written).
        var error = Assert.Single(listing.Diagnostics, d => d.Message.Contains("'FOO'", StringComparison.Ordinal));
        Assert.Equal((fooLine.Number, 1, foo, lines[foo - 1].IndexOf("FOO", StringComparison.Ordinal) + 1),
            (error.Line, error.File, error.FileLine, error.Column));
        Assert.Contains(error.Id, fooLine.Diagnostics);
        foreach (var record in result.Diagnostics.Where(d => d.IsError))
        {
            var at = Assert.NotNull(record.Location);
            var placed = Assert.Single(listing.Diagnostics, d => d.Message == record.Message && d.Code == record.Code);
            Assert.Equal((at.Line + 1, at.Column + 1), (placed.FileLine, placed.Column));
        }
    }
}
