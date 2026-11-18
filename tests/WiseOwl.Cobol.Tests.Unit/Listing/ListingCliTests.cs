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
    public void ABadValue_IsAnArgumentError(string option, string value) => Assert.NotEmpty(Errors("a.cob", option, value));
}
