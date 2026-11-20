// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Cli;
using WiseOwl.Cobol.Listing;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The listing's command line (DESIGN-listing.md section 8): on by default at <c>--std 85</c>, because §1985:IV.7.1 requires
/// each compiler to produce "an output listing of the source program in reference format"; off by default at the later
/// editions, which leave it to the implementor (§2023:7.3.18.3 GR1 "Whether the compiler produces a source listing is
/// implementor defined"); every value checked by the parser, an argument error and never a COBOL diagnostic.
/// </summary>
public sealed class ListingCliTests
{
    private static CliOptions Resolve(params string[] args)
    {
        var (command, resolve) = Program.BuildParser();
        var parse = command.Parse(args);
        Assert.True(parse.Errors.Count == 0, string.Join("; ", parse.Errors.Select(e => e.Message)));
        return resolve(parse);
    }

    private static IReadOnlyList<string> Errors(params string[] args)
        => Program.BuildParser().Command.Parse(args).Errors.Select(e => e.Message).ToList();

    [Fact]
    public void At85_TheListingIsOnByDefault_AndOffTurnsItOff()
    {
        Assert.Equal(new ListingRequest(), Resolve("a.cob", "--std", "85").Listing);
        Assert.Equal(new ListingRequest(), Resolve("a.cob", "--nist").Listing);   // --nist targets 85
        Assert.Null(Resolve("a.cob", "--std", "85", "--listing", "off").Listing);
    }

    [Theory]
    [InlineData("2002")]
    [InlineData("2014")]
    [InlineData("2023")]
    public void AtTheLaterEditions_TheListingIsOnRequest(string std)
    {
        Assert.Null(Resolve("a.cob", "--std", std).Listing);
        Assert.Equal(new ListingRequest(), Resolve("a.cob", "--std", std, "--listing").Listing);
        Assert.Equal(new ListingRequest(), Resolve("a.cob", "--std", std, "--listing", "on").Listing);
        Assert.Equal(new ListingRequest("-"), Resolve("a.cob", "--std", std, "--listing-output", "-").Listing);
    }

    [Fact]
    public void TheListingOptions_ReachTheRequest()
    {
        Assert.Equal(new ListingRequest("out/x", 0, 200),
            Resolve("a.cob", "--listing-output", "out/x", "--listing-page-length", "0", "--listing-page-width", "200").Listing);
    }

    /// <summary><c>--listing-format</c> (DESIGN-listing.md section 8): repeatable, each occurrence one format, the default text,
    /// and naming one asks for a listing at any edition.</summary>
    [Fact]
    public void TheListingFormats_ReachTheRequest()
    {
        Assert.Equal(new ListingRequest(Formats: ListingFormats.Json), Resolve("a.cob", "--std", "2023", "--listing-format", "json").Listing);
        Assert.Equal(new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json),
            Resolve("a.cob", "--listing-format", "text", "--listing-format=JSON").Listing);
        Assert.Equal(new ListingRequest(Formats: ListingFormats.Json), Resolve("--listing-format", "json", "a.cob").Listing);
        Assert.Equal(new ListingRequest("-", Formats: ListingFormats.Json),
            Resolve("a.cob", "--listing-output", "-", "--listing-format", "json").Listing);
        Assert.Equal(new ListingRequest(Formats: ListingFormats.Html), Resolve("a.cob", "--listing-format", "html").Listing);
        Assert.Equal(new ListingRequest(Formats: ListingFormats.Text | ListingFormats.Json | ListingFormats.Html),
            Resolve("a.cob", "--listing-format", "html", "--listing-format", "text", "--listing-format", "json").Listing);
        Assert.Null(Resolve("a.cob", "--std", "85", "--listing", "off", "--listing-format", "json").Listing);   // off wins
    }

    /// <summary><c>--listing-sections</c> (DESIGN-listing.md section 8): a comma list of section words, <c>standard</c> and
    /// <c>all</c>; the default is <c>standard</c>; naming it asks for a listing at any edition.</summary>
    [Fact]
    public void TheListingSections_ReachTheRequest()
    {
        Assert.Equal(ListingSectionSet.Standard, Resolve("a.cob", "--std", "85").Listing!.Sections);
        Assert.Equal(new ListingRequest { Sections = ListingSectionSet.Of(ListingSection.Source, ListingSection.Options) },
            Resolve("a.cob", "--std", "2023", "--listing-sections", "options,source").Listing);
        Assert.Equal(ListingSectionSet.All, Resolve("a.cob", "--listing-sections=all").Listing!.Sections);
        Assert.Null(Resolve("a.cob", "--std", "85", "--listing", "off", "--listing-sections", "all").Listing);   // off wins
    }

    /// <summary>End to end: <c>--listing-format text --listing-format json</c> writes <c>BASE.lst</c> and <c>BASE.lst.json</c>,
    /// both renderings of the one document; <c>--listing-format json</c> alone writes no text listing.</summary>
    [Fact]
    public void TheCommandLine_WritesEachFormatRequested()
    {
        string dir = WiseOwl.Cobol.Tests.Shared.ScratchDirectory.Create("listing-cli-json");
        try
        {
            string src = Path.Combine(dir, "clij.cob");
            File.WriteAllText(src, "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CLIJ.\n       PROCEDURE DIVISION.\n"
                + "       MAIN-PARA.\n           STOP RUN.\n");
            string dll = Path.Combine(dir, "clij.dll"), lst = Path.Combine(dir, "clij.lst"), json = Path.Combine(dir, "clij.lst.json");
            Assert.Equal(0, Program.Parse([src, "-o", dll, "--listing-format", "text", "--listing-format", "json"]).Invoke());
            var read = WiseOwl.Cobol.Listing.Json.JsonListingWriter.Read(File.ReadAllText(json));
            Assert.Equal(File.ReadAllText(src).Split('\n'), read.Lines.Select(l => l.Text));   // every physical line, as written
            Assert.Contains("PROGRAM-ID. CLIJ.", File.ReadAllText(lst), StringComparison.Ordinal);
            File.Delete(lst);
            File.Delete(json);
            Assert.Equal(0, Program.Parse([src, "-o", dll, "--listing-format", "json"]).Invoke());
            Assert.True(File.Exists(json));
            Assert.False(File.Exists(lst));
        }
        finally { WiseOwl.Cobol.Tests.Shared.ScratchDirectory.Remove(dir); }
    }

    /// <summary>End to end through the command line: at <c>--std 85</c> the compile writes <c>&lt;out&gt;.lst</c> with no
    /// listing option, and <c>--listing off</c> writes none.</summary>
    [Fact]
    public void At85_TheCommandLineWritesTheListingBesideTheAssembly_UnlessOff()
    {
        string dir = WiseOwl.Cobol.Tests.Shared.ScratchDirectory.Create("listing-cli");
        try
        {
            string src = Path.Combine(dir, "cli85.cob");
            File.WriteAllText(src, "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. CLI85.\n       PROCEDURE DIVISION.\n"
                + "       MAIN-PARA.\n           STOP RUN.\n");
            string dll = Path.Combine(dir, "cli85.dll"), lst = Path.Combine(dir, "cli85.lst");
            Assert.Equal(0, Program.Parse([src, "--std", "85", "-o", dll]).Invoke());
            Assert.True(File.Exists(lst));
            Assert.Contains("PROGRAM-ID. CLI85.", File.ReadAllText(lst), StringComparison.Ordinal);
            File.Delete(lst);
            Assert.Equal(0, Program.Parse([src, "--std", "85", "-o", dll, "--listing", "off"]).Invoke());
            Assert.False(File.Exists(lst));
        }
        finally { WiseOwl.Cobol.Tests.Shared.ScratchDirectory.Remove(dir); }
    }

    [Theory]
    [InlineData("--listing", "maybe")]
    [InlineData("--listing-page-width", "99")]
    [InlineData("--listing-page-width", "256")]
    [InlineData("--listing-page-length", "4")]
    [InlineData("--listing-page-length", "-1")]
    [InlineData("--listing-format", "pdf")]
    [InlineData("--listing-format", "text,json")]
    [InlineData("--listing-sections", "source,xrefs")]
    [InlineData("--listing-sections", "source,")]
    [InlineData("--listing-sections", "")]
    public void ABadValue_IsAnArgumentError(string option, string value) => Assert.NotEmpty(Errors("a.cob", option, value));

    /// <summary>The comment line of <see cref="ToStandardOutput_TheListingIsUtf8_AndTheDisplayLinesFollowIt"/>: two characters
    /// outside ASCII, one of them (U+00E9) a single byte in cp437 and the other (U+20AC) in no OEM code page at all.</summary>
    private const string NonAsciiComment = "      * café €";

    /// <summary>The line the program's DISPLAY directive transfers UPON CONSOLE.</summary>
    private const string DisplayedLine = "AFTER THE LISTING";

    /// <summary>Section 8 (PB5941, correction): with <c>--listing-output -</c> the one format goes to standard output as the
    /// bytes its file would hold, UTF-8 with no byte order mark whatever the console's code page, and the lines of the DISPLAY
    /// directives that go to standard output follow it there. Run as a process under a cp437 console with standard output
    /// redirected (<see cref="OemConsoleCli"/>): the listing goes through <c>Console.Out</c>, which
    /// <c>StandardStreams.EnsureUtf8</c> makes BOM-less UTF-8 when standard output is redirected, and an in-process run cannot
    /// observe those bytes because <c>EnsureUtf8</c> runs from <c>Main</c> (kb/Work PB5941 reviews R1-1 and R2-1). A writer that
    /// re-encodes through the console's code page sends U+00E9 as the byte 0x82 and U+20AC as <c>?</c>, which the strict decode
    /// or the comparison refuses; writing the listing after the DISPLAY lines puts the displayed line first.</summary>
    [Theory]
    [InlineData("text")]
    [InlineData("json")]
    [InlineData("html")]
    public void ToStandardOutput_TheListingIsUtf8_AndTheDisplayLinesFollowIt(string format)
    {
        string work = WiseOwl.Cobol.Tests.Shared.ScratchDirectory.Create("listing-cli-stdout");
        try
        {
            string source = Path.Combine(work, "lstout.cob");
            File.WriteAllText(source, "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. LSTOUT.\n" + NonAsciiComment + "\n"
                + $"       >>DISPLAY \"{DisplayedLine}\" UPON CONSOLE\n       PROCEDURE DIVISION.\n           STOP RUN.\n",
                ListingOutput.Utf8);

            var capture = OemConsoleCli.Run(work, source, "--std", "2023", "--listing-output", "-", "--listing-format", format);
            Assert.True(capture.Error.Length == 0, System.Text.Encoding.UTF8.GetString(capture.Error));
            Assert.False(capture.Output.AsSpan().StartsWith(System.Text.Encoding.UTF8.Preamble), "a byte order mark");
            string output = new System.Text.UTF8Encoding(false, throwOnInvalidBytes: true).GetString(capture.Output);

            // The displayed line is the last line, and the only line that is the displayed text alone: it follows the listing.
            string displayed = DisplayedLine + Environment.NewLine;
            Assert.EndsWith("\n" + displayed, output, StringComparison.Ordinal);
            string listing = output[..^displayed.Length];
            Assert.DoesNotContain(listing.Split('\n'), l => l.TrimEnd('\r') == DisplayedLine);

            if (format == "json")
                Assert.Contains(NonAsciiComment, WiseOwl.Cobol.Listing.Json.JsonListingWriter.Read(listing).Lines.Select(l => l.Text));
            else
                Assert.Contains(format == "html" ? NonAsciiComment[^6..] : NonAsciiComment, listing, StringComparison.Ordinal);
            if (format == "html") Assert.EndsWith("</html>\n", listing, StringComparison.Ordinal);
        }
        finally { WiseOwl.Cobol.Tests.Shared.ScratchDirectory.Remove(work); }
    }

    /// <summary>A listing on standard output is ONE format: two would interleave in one stream.</summary>
    [Fact]
    public void TwoFormatsOnStandardOutput_IsAnArgumentError() =>
        Assert.Contains(Errors("a.cob", "--listing-output", "-", "--listing-format", "text", "--listing-format", "json"),
            e => e.Contains("one format", StringComparison.Ordinal));
}
