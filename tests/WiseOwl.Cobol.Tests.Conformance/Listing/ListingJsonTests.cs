// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text.Json.Nodes;
using WiseOwl.Cobol;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Json;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The JSON listing end to end (DESIGN-listing.md sections 7.3 and 10 item 5): a compilation that asks for text and JSON
/// writes <c>BASE.lst</c> and <c>BASE.lst.json</c>, both renderings of the one document the compiler built. Every expected value
/// comes from the input files the test wrote, from a rule, or from the committed schema, never from a captured listing:
/// <list type="bullet">
/// <item>the JSON is valid against <c>listing-v1.schema.json</c> and names it (<c>$schema</c>, <c>schemaVersion</c>);</item>
/// <item>§1985:IV.7.1 and §1985:IV.7.4: each line's <c>text</c> is the file's own physical line, and each file's <c>sha256</c> is
/// the digest of the bytes the test wrote;</item>
/// <item>§1985:VI.3.4.4 GR1: the DATE-COMPILED paragraph "is replaced during compilation with a paragraph of the form"
/// <c>DATE-COMPILED. current date.</c>; the line's <c>replaced</c> holds that paragraph at the instant the header's
/// <c>compiledAt</c> names, while its <c>text</c> keeps what was written.</item>
/// </list>
/// </summary>
public sealed class ListingJsonTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("listingjson");

    public void Dispose() => ScratchDirectory.Remove(_dir);

    private const string Main = """
        000100 IDENTIFICATION DIVISION.
        000200 PROGRAM-ID. JSON85.
        000300 DATE-COMPILED. WHENEVER.
        000400 DATA DIVISION.
        000500 WORKING-STORAGE SECTION.
        000600 01 GROUP-A.
        000700     COPY JREC.
        000800 PROCEDURE DIVISION.
        000900 MAIN-PARA.
        001000     DISPLAY FIELD-1.
        001100     STOP RUN.
        """;

    private const string Rec = """
        000100     05 FIELD-1 PIC X(4) VALUE "JSON".
        000200*    A COMMENT IN THE LIBRARY TEXT
        """;

    private (CompilerDriver.Result Result, string Source, string Copybook) Compile(ListingRequest request, string main = Main)
    {
        string source = Path.Combine(_dir, "json85.cob"), copybook = Path.Combine(_dir, "JREC.cpy");
        File.WriteAllText(source, main);
        File.WriteAllText(copybook, Rec);
        var result = CompiledProgramCache.CompileCold(new CompilerDriver.Options(source, Path.Combine(_dir, "json85.dll"),
            DialectLevel: 85, CopyPaths: [_dir], Listing: request));
        if (result.Listing is { } listing) ListingJsonContract.AssertHolds(listing);
        return (result, source, copybook);
    }

    private static string Sha256(string path) => Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));

    [Fact]
    public void TextAndJson_AreBothWritten_AndTheJsonIsTheDocumentTheCompilerBuilt()
    {
        var (result, source, copybook) = Compile(new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json));
        Assert.True(result.Success, string.Join("\n", result.Errors));
        string lst = Path.GetFullPath(Path.Combine(_dir, "json85.lst")), json = Path.GetFullPath(Path.Combine(_dir, "json85.lst.json"));
        Assert.Contains(lst, result.OutputFiles);
        Assert.Contains(json, result.OutputFiles);
        string text = File.ReadAllText(json);
        Assert.Equal(ListingRendering.Json(result.Listing!), text);

        // Valid against the committed schema, and naming it.
        var node = JsonNode.Parse(text)!;
        Assert.Empty(JsonSchemaSubset.Validate(JsonNode.Parse(ListingJsonSchema.Committed())!, node));
        Assert.Equal(ListingDocument.SchemaUrn, (string?)node["$schema"]);
        Assert.Equal(ListingDocument.CurrentSchemaVersion, (string?)node["schemaVersion"]);

        // Each file by its role and the digest of the bytes the test wrote.
        Assert.Equal([("Source", Sha256(source)), ("Library", Sha256(copybook))],
            node["files"]!.AsArray().Select(f => ((string)f!["role"]!, (string)f["sha256"]!)));

        // Every physical line of the source, then the library text after the COPY statement's line (7), as the files hold them.
        string[] main = File.ReadAllText(source).Split('\n'), rec = File.ReadAllText(copybook).Split('\n');
        string[] expected = [.. main.Take(7), .. rec, .. main.Skip(7)];
        Assert.Equal(expected, node["lines"]!.AsArray().Select(l => (string)l!["text"]!));
        Assert.Equal([1], node["lines"]!.AsArray().Where(l => l!["inclusion"] is not null).Select(l => (int)l!["inclusion"]!).Distinct());

        // The DATE-COMPILED paragraph: written text kept, the replacement at the header's instant (§1985:VI.3.4.4 GR1).
        var dateCompiled = node["lines"]![2]!;
        Assert.Equal(main[2], (string?)dateCompiled["text"]);
        Assert.Equal("DateCompiled", (string?)dateCompiled["replacement"]);
        var instant = DateTimeOffset.Parse((string)node["header"]!["compiledAt"]!, CultureInfo.InvariantCulture);
        // The paragraph is replaced where it stands: the line's columns before the paragraph's word are kept as written.
        string margin = main[2][..main[2].IndexOf("DATE-COMPILED", StringComparison.Ordinal)];
        Assert.Equal(margin + "DATE-COMPILED. " + instant.ToString("yyyy'-'MM'-'dd HH':'mm':'ss zzz", CultureInfo.InvariantCulture) + ".",
            (string?)dateCompiled["replaced"]);
    }

    /// <summary>JSON alone writes no text listing; a failed compilation still writes its JSON, naming the outcome.</summary>
    [Fact]
    public void JsonAlone_IsWrittenOnAFailedCompilationToo()
    {
        var (result, _, _) = Compile(new ListingRequest(Formats: ListingFormats.Json),
            Main.Replace("DISPLAY FIELD-1.", "DISPLAY NO-SUCH-NAME.", StringComparison.Ordinal));
        Assert.Equal(CompilerDriver.Outcome.BindError, result.Status);
        Assert.False(File.Exists(Path.Combine(_dir, "json85.lst")));
        var read = JsonListingWriter.Read(File.ReadAllText(Path.Combine(_dir, "json85.lst.json")));
        Assert.Equal(ListingOutcome.BindError, read.Header.Outcome);
        // A record's list member compares by reference, so each line's diagnostic ids and spans are compared as sequences.
        static IEnumerable<(ListingLine, string, string)> ByValue(IEnumerable<ListingLine> lines) =>
            lines.Select(l => (l with { Diagnostics = [], Spans = [] }, string.Join(',', l.Diagnostics), string.Join(',', l.Spans)));
        Assert.Equal(ByValue(result.Listing!.Lines), ByValue(read.Lines));
        Assert.Contains(read.Diagnostics, d => d.Message.Contains("NO-SUCH-NAME", StringComparison.Ordinal) && d.Line is not null);
    }

    /// <summary>A listing file the host refuses is one error for that file (DESIGN-listing.md section 4.6), changes no outcome,
    /// and the other format is still written.</summary>
    [Fact]
    public void ARefusedJsonFile_IsOneError_AndTheTextIsStillWritten()
    {
        Directory.CreateDirectory(Path.Combine(_dir, "json85.lst.json"));   // a directory where the file would go
        var (result, _, _) = Compile(new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json));
        Assert.Equal(CompilerDriver.Outcome.Success, result.Status);
        string error = Assert.Single(result.Errors);
        Assert.StartsWith($"cannot write the listing '{Path.Combine(_dir, "json85")}.lst.json': ", error, StringComparison.Ordinal);
        Assert.Contains(Path.GetFullPath(Path.Combine(_dir, "json85.lst")), result.OutputFiles);
        Assert.DoesNotContain(result.OutputFiles, f => f.EndsWith(".lst.json", StringComparison.Ordinal));
    }
}
