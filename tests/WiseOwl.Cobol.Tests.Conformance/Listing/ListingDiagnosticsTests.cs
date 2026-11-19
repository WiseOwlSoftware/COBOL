// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Editions.Diagnostics;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Json;
using WiseOwl.Cobol.Listing.Text;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The diagnostics of the listing (DESIGN-listing.md section 6.3; step 3 of section 11): every diagnostic the compilation reported is a
/// record (<see cref="CompilerDriver.Result.Diagnostics"/>) whose rendering is the line the compiler prints, and the listing places
/// each on the listing line and column its record names, under that line in the text listing and in the summary at the end, with
/// the citation of the rule its message names as data. Expected lines, columns and citations are read off the test's own source
/// text and the rule the diagnostic enforces, never off a captured listing.
/// </summary>
public sealed class ListingDiagnosticsTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listingdiag");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    /// <summary>A directory under <see cref="_dir"/> that makes the path of a file in it longer than the default page, whatever
    /// the host's temporary directory is (kb/Work PB5931): the source path in a summary row then cannot fit the row, on any host.</summary>
    private string LongDirectory()
    {
        int padding = Math.Max(1, ListingRequest.DefaultPageWidth + 28 - _dir.Length - 1);
        string dir = Path.Combine(_dir, new string('d', padding));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private (CompilerDriver.Result Result, string Source) Compile(string name, string[] lines, int edition = 2023,
        ListingFormats formats = ListingFormats.Text | ListingFormats.Json, bool flagExtensions = false,
        IReadOnlyDictionary<string, string>? copybooks = null, string? within = null)
    {
        string dir = within ?? _dir;
        string source = Path.Combine(dir, name + ".cob");
        File.WriteAllText(source, string.Join("\n", lines));
        foreach (var (book, text) in copybooks ?? new Dictionary<string, string>()) File.WriteAllText(Path.Combine(dir, book), text);
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(source, Path.Combine(dir, name + ".dll"),
            DialectLevel: edition, CopyPaths: [dir], FlagExtensions: flagExtensions, Listing: new ListingRequest(Formats: formats)));
        if (result.Listing is { } listing) ListingJsonContract.AssertHolds(listing);
        return (result, source);
    }

    /// <summary>
    /// A bind error of a data entry: <c>PIC 9(32)</c> describes 32 digit positions, and §2023:13.18.40.3 SR14 says "the number of digit
    /// positions described by character-string-1 shall range from 1 through 31". The diagnostic is placed on the entry's listing
    /// line at the column its record names (the entry's level-number, where the binder's cursor stands for an entry), printed under
    /// that line with a caret and in the summary, and its citation is the rule as data.
    /// </summary>
    /// <remarks>Run in a short directory and in one whose path is longer than the page (kb/Work PB5931): the renderer wraps a summary
    /// row at the page width (<see cref="TextListingRenderer.Indented"/>), so what the test asserts of it is the whole entry,
    /// however it wraps, and never a substring of one unwrapped row, whose fit depends on the length of the host's path.</remarks>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ABindError_IsPlacedOnItsLine_InlineAndInTheSummary_WithTheRuleItCites(bool longPath)
    {
        string work = longPath ? LongDirectory() : _dir;
        string[] lines =
        [
            "       IDENTIFICATION DIVISION.",
            "       PROGRAM-ID. DIAG1.",
            "       DATA DIVISION.",
            "       WORKING-STORAGE SECTION.",
            "       01 BIG PIC 9(32).",
            "       PROCEDURE DIVISION.",
            "           STOP RUN.",
        ];
        var (result, source) = Compile("diag1", lines, within: work);
        Assert.Equal(CompilerDriver.Outcome.BindError, result.Status);
        var listing = Assert.IsType<ListingDocument>(result.Listing);
        int entry = Array.FindIndex(lines, l => l.Contains("9(32)", StringComparison.Ordinal)) + 1;
        var line = listing.Lines.Single(l => l.Inclusion is null && l.FileLine == entry);
        var d = Assert.Single(listing.Diagnostics, d => d.Code == DiagnosticCatalog.DigitCapacityOver31.Code);
        Assert.Equal((ListingSeverity.Error, line.Number, 1, entry), (d.Severity, d.Line, d.File, d.FileLine));
        Assert.Equal(lines[entry - 1].IndexOf("01 BIG", StringComparison.Ordinal) + 1, d.Column);
        Assert.Equal([d.Id], line.Diagnostics);
        Assert.Equal([new ListingCitation(2023, "13.18.40.3", "SR14")], d.Citations);
        // The record the listing placed is the line the compiler printed.
        var record = Assert.Single(result.Diagnostics, r => r.Code == d.Code);
        Assert.Contains(record.ToString(), result.Errors);
        Assert.Equal(record.Message, d.Message);

        // The text listing: under the entry's row a caret at its column, then the arrow row; the summary on a page of its own.
        string text = File.ReadAllText(Path.Combine(work, "diag1.lst"));
        string[] rows = ListingPages.Of(text, ListingSection.Source)[0].Split('\n');
        int row = Array.FindIndex(rows, r => r.EndsWith(lines[entry - 1], StringComparison.Ordinal));
        Assert.Equal(new string(' ', TextListingRenderer.TextColumn - 1 + d.Column!.Value - 1) + "^", rows[row + 1]);
        Assert.StartsWith(new string(' ', TextListingRenderer.TextColumn - 1) + $"==> error {d.Code} (col {d.Column}): ", rows[row + 2], StringComparison.Ordinal);
        string summary = Assert.Single(ListingPages.Of(text, ListingSection.Diagnostics));
        Assert.Contains("Errors: 1   Warnings: 0   Information: 0", summary, StringComparison.Ordinal);
        // The summary entry (DESIGN-listing.md section 6.3): "line  severity CODE file(line,col): message", wrapped at the page width
        // with its further rows indented 8. The entry is the row that opens it and the rows indented under it. They hold exactly
        // the entry's words in order (a break is at a space, or cuts a word no space lets fit, as a long path is), none is wider
        // than the page, and they are the rows the renderer's wrap makes of the entry.
        string entryText = $"{line.Number,6}  error {d.Code} {source}({entry},{d.Column}): {d.Message}";
        string[] summaryRows = summary.Split('\n');
        int first = Array.FindIndex(summaryRows, r => r.StartsWith($"{line.Number,6}  error {d.Code}", StringComparison.Ordinal));
        Assert.True(first >= 0, "no summary row opens the diagnostic's entry");
        string[] entryRows = [summaryRows[first], .. summaryRows.Skip(first + 1).TakeWhile(r => r.StartsWith("        ", StringComparison.Ordinal))];
        Assert.All(entryRows, r => Assert.True(r.Length <= ListingRequest.DefaultPageWidth, $"a summary row is wider than the page: {r}"));
        Assert.Equal(entryText.Replace(" ", "", StringComparison.Ordinal), string.Concat(entryRows).Replace(" ", "", StringComparison.Ordinal));
        Assert.Equal(TextListingRenderer.Indented(entryText, 0, 8, ListingRequest.DefaultPageWidth), entryRows);
        if (longPath) Assert.True(entryRows.Length > 1, "the source path was to be longer than the page, so the entry wraps");

        // The JSON carries the citation as a field.
        var json = JsonNode.Parse(File.ReadAllText(Path.Combine(work, "diag1.lst.json")))!;
        var cited = json["diagnostics"]!.AsArray().Single(x => (string?)x!["code"] == d.Code)!["citations"]!.AsArray().Single()!;
        Assert.Equal((2023, "13.18.40.3", "SR14"), ((int)cited["edition"]!, (string)cited["locator"]!, (string)cited["rule"]!));
    }

    /// <summary>
    /// A diagnosed entry after a supplementary-plane character on its line (listing step 3 review lead R1-1): the record's column is
    /// a CHARACTER position of the line's text (DESIGN-listing.md section 6.3; the schema's "1-based character position"), so the
    /// character, two UTF-16 units, counts one. Expected from the line as written: the level-number of the 32-digit entry, which is
    /// where the binder's cursor stands for an entry (§2023:13.18.40.3 SR14 is the rule it breaks), counted in characters. The caret
    /// row and the inline row read the same column.
    /// </summary>
    [Fact]
    public void AColumn_CountsASupplementaryPlaneCharacter_AsOneCharacter()
    {
        string[] lines =
        [
            "       IDENTIFICATION DIVISION.",
            "       PROGRAM-ID. DIAG2.",
            "       DATA DIVISION.",
            "       WORKING-STORAGE SECTION.",
            "       01 A PIC X(8) VALUE \"\U0001D11E\". 01 BIG PIC 9(32).",
            "       PROCEDURE DIVISION.",
            "           STOP RUN.",
        ];
        var (result, _) = Compile("diag2", lines);
        var listing = Assert.IsType<ListingDocument>(result.Listing);
        var d = Assert.Single(listing.Diagnostics, d => d.Code == DiagnosticCatalog.DigitCapacityOver31.Code);
        string text = lines[4];
        int units = text.IndexOf("01 BIG", StringComparison.Ordinal);
        int characters = text[..units].EnumerateRunes().Count();
        Assert.Equal(units - 1, characters);   // the one supplementary-plane character before it is two units
        Assert.Equal(characters + 1, d.Column);

        string[] rows = ListingPages.Of(File.ReadAllText(Path.Combine(_dir, "diag2.lst")), ListingSection.Source)[0].Split('\n');
        int row = Array.FindIndex(rows, r => r.EndsWith(text, StringComparison.Ordinal));
        Assert.Equal(new string(' ', TextListingRenderer.TextColumn - 1 + characters) + "^", rows[row + 1]);
        Assert.StartsWith(new string(' ', TextListingRenderer.TextColumn - 1) + $"==> error {d.Code} (col {characters + 1}): ", rows[row + 2],
            StringComparison.Ordinal);
    }

    /// <summary>
    /// A copybook copied twice (DESIGN-listing.md section 4.4): the second copy's REPLACING makes its item 32 digits, the first's 5,
    /// so only the second copy is in error, at the copybook's line 1. The listing places the diagnostic in the SECOND block, by the
    /// resultant line its record was located from, although both blocks list that physical line.
    /// </summary>
    [Fact]
    public void ADiagnosticInTheSecondCopyOfACopybook_IsPlacedInTheSecondBlock()
    {
        string[] lines =
        [
            "       IDENTIFICATION DIVISION.",
            "       PROGRAM-ID. DIAG2.",
            "       DATA DIVISION.",
            "       WORKING-STORAGE SECTION.",
            "       COPY TWICE REPLACING ==:N:== BY ==SMALL== ==:P:== BY ==9(5)==.",
            "       COPY TWICE REPLACING ==:N:== BY ==LARGE== ==:P:== BY ==9(32)==.",
            "       PROCEDURE DIVISION.",
            "           STOP RUN.",
        ];
        var (result, _) = Compile("diag2", lines, copybooks: new Dictionary<string, string> { ["TWICE.cpy"] = "       01 :N: PIC :P:.\n" });
        Assert.Equal(CompilerDriver.Outcome.BindError, result.Status);
        var listing = Assert.IsType<ListingDocument>(result.Listing);
        Assert.Equal(2, listing.Inclusions.Count);
        var d = Assert.Single(listing.Diagnostics, d => d.Code == DiagnosticCatalog.DigitCapacityOver31.Code);
        var second = listing.Inclusions.Single(i => i.CopyLine == listing.Lines.Single(l => l.Inclusion is null && l.FileLine == 6).Number);
        var placed = listing.Lines.Single(l => l.Number == d.Line);
        Assert.Equal((second.Id, 1), (placed.Inclusion, placed.FileLine));
        Assert.Equal(second.File, d.File);
        // The first block's line 1, the same physical line, carries nothing.
        Assert.Empty(listing.Lines.Single(l => l.Inclusion == listing.Inclusions.Single(i => i.Id != second.Id).Id && l.FileLine == 1).Diagnostics);
    }

    /// <summary>
    /// ⛔ ONE RECORD PER PRINTED DIAGNOSTIC (DESIGN-listing.md section 6.3): on a front-end error, a bind error and a success with
    /// warnings, every record of <see cref="CompilerDriver.Result.Diagnostics"/> renders a line of the compiler's report (errors in
    /// <see cref="CompilerDriver.Result.Errors"/>, warnings in <see cref="CompilerDriver.Result.Warnings"/>), every line of the report
    /// that has the shape of a diagnostic is one record's rendering, and the listing holds exactly the records, each by its own
    /// severity. A line no record renders names no source (an output refused), which these compilations have none of.
    /// </summary>
    [Theory]
    [InlineData("frontend")]
    [InlineData("bind")]
    [InlineData("warnings")]
    public void EveryPrintedDiagnostic_IsARecord_AndTheListingHoldsEachOne(string shape)
    {
        string[] body = shape switch
        {
            "frontend" => ["       PROCEDURE DIVISION.", "           FOO BAR BAZ.", "           STOP RUN."],
            "bind" => ["       DATA DIVISION.", "       WORKING-STORAGE SECTION.", "       01 BIG PIC 9(32).", "       PROCEDURE DIVISION.",
                "           STOP RUN."],
            _ => ["       DATA DIVISION.", "       WORKING-STORAGE SECTION.", "       01 PACKED PIC 9(3) COMP-3.",
                "       PROCEDURE DIVISION.", "           STOP RUN."],
        };
        var (result, _) = Compile("rec" + shape, ["       IDENTIFICATION DIVISION.", "       PROGRAM-ID. RECS.", .. body],
            formats: ListingFormats.Json, flagExtensions: shape == "warnings");
        Assert.Equal(shape switch { "frontend" => CompilerDriver.Outcome.FrontendError, "bind" => CompilerDriver.Outcome.BindError,
            _ => CompilerDriver.Outcome.Success }, result.Status);
        Assert.NotEmpty(result.Diagnostics);
        if (shape == "warnings") Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCatalog.NonstandardExtensionUsed.Code);

        Assert.Equal(result.Diagnostics.Where(d => d.IsError).Select(d => d.ToString()).Order(StringComparer.Ordinal),
            result.Errors.Where(IsDiagnosticLine).Order(StringComparer.Ordinal));
        Assert.Equal(result.Diagnostics.Where(d => !d.IsError).Select(d => d.ToString()).Order(StringComparer.Ordinal),
            result.Warnings.Where(IsDiagnosticLine).Order(StringComparer.Ordinal));
        Assert.All(result.Errors.Concat(result.Warnings), line => Assert.True(IsDiagnosticLine(line), line));

        var listing = Assert.IsType<ListingDocument>(result.Listing);
        Assert.Equal(result.Diagnostics.Select(d => (d.Code, d.Message, d.IsError)).Order(),
            listing.Diagnostics.Select(d => (d.Code, d.Message, d.Severity == ListingSeverity.Error)).Order());
        Assert.Equal([.. Enumerable.Range(1, listing.Diagnostics.Count)], listing.Diagnostics.Select(d => d.Id));
        // In listing order: placed ones by line and column, then the rest.
        Assert.Equal(listing.Diagnostics.OrderBy(d => d.Line ?? int.MaxValue).ThenBy(d => d.Column ?? 0).Select(d => d.Id),
            listing.Diagnostics.Select(d => d.Id));
        // Each line names exactly the diagnostics placed on it.
        Assert.Equal(listing.Diagnostics.Where(d => d.Line is not null).Select(d => (d.Line!.Value, d.Id)),
            listing.Lines.SelectMany(l => l.Diagnostics.Select(id => (l.Number, id))));
    }

    /// <summary>A line of the compiler's report in the shape of a diagnostic: an optional <c>file(line,col): </c>, then the severity,
    /// a code and a colon.</summary>
    private static bool IsDiagnosticLine(string line) => DiagnosticLine.IsMatch(line);

    private static readonly Regex DiagnosticLine = new(@"^(?:.*\(\d+,\d+\): )?(?:error|warning|info) [A-Z]+[0-9]+: ");
}
